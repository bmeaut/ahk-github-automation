using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public abstract class RepositoryEventBase<TPayload> : IGitHubEventHandler
        where TPayload : ActivityPayload
    {
        private readonly IGitHubClientFactory gitHubClientFactory;
        private readonly IMemoryCache cache;

        protected RepositoryEventBase(IGitHubClientFactory gitHubClientFactory, IMemoryCache cache, ILogger logger)
        {
            this.gitHubClientFactory = gitHubClientFactory;
            this.cache = cache;

            Logger = logger;
        }

        protected ILogger Logger { get; }

        protected IGitHubClient GitHubClient { get; private set; }

        public async Task<EventHandlerResult> Execute(string requestBody)
        {
            if (!TryParsePayload(requestBody, out var webhookPayload, out var errorResult))
                return errorResult;

            GitHubClient = await gitHubClientFactory.CreateGitHubClient(webhookPayload.Installation.Id, Logger);

            if (!await IsEnabledForRepository(webhookPayload))
            {
                Logger.LogInformation("no ahk-monitor.yml or disabled");
                return EventHandlerResult.Disabled("no ahk-monitor.yml or disabled");
            }

            return await ExecuteCore(webhookPayload);
        }

        protected abstract Task<EventHandlerResult> ExecuteCore(TPayload webhookPayload);

        protected bool TryParsePayload(string requestBody, out TPayload payload, out EventHandlerResult errorResult)
        {
            payload = null;
            if (string.IsNullOrEmpty(requestBody))
            {
                errorResult = EventHandlerResult.PayloadError("request body was empty");
                Logger.LogError("request body was empty");
                return false;
            }

            try
            {
                payload = new SimpleJsonSerializer().Deserialize<TPayload>(requestBody);
            }
            catch (Exception ex)
            {
                errorResult = EventHandlerResult.PayloadError($"request body deserialization failed: {ex.Message}");
                Logger.LogError(ex, "request body deserialization failed");
                return false;
            }

            if (payload == null)
            {
                errorResult = EventHandlerResult.PayloadError("parsed payload was null or empty");
                Logger.LogError("parsed payload was null or empty");
                return false;
            }

            if (payload.Repository == null)
            {
                errorResult = EventHandlerResult.PayloadError("no repository information in webhook payload");
                Logger.LogError("no repository information in webhook payload");
                return false;
            }

            errorResult = null;
            return true;
        }

        protected Task<string> GetNeptun(long repositoryId, string branchName)
            => cache.GetOrCreateAsync(
                key: $"neptuntxtfile{repositoryId}{branchName}",
                factory: async cacheEntry =>
                {
                    // Don't cache null value
                    // https://schwabencode.com/blog/2022/12/08/dotnet-avoid-caching-with-memorycache-and-getorcreate
                    var value = await GetNeptunTxtFileContent(repositoryId, branchName);
                    if (value is null)
                    {
                        cacheEntry.Dispose();
                        return null;
                    }

                    cacheEntry.Value = value;
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                    return value;
                });

        protected async Task<string> GetNeptunTxtFileContent(long repositoryId, string branchName)
        {
            try
            {
                var contents = await GitHubClient.Repository.Content.GetAllContentsByRef(repositoryId, "neptun.txt", branchName);
                if (contents.Count == 0)
                    return null;
                return !string.IsNullOrWhiteSpace(contents[0].Content)
                    ? contents[0].Content.Trim()
                    : null;
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        protected Task<bool> isUserOrganizationMember(TPayload webhookPayload, string username)
        {
            if (webhookPayload.Repository.Owner.Type != AccountType.Organization)
                return Task.FromResult(false);

            return cache.GetOrCreateAsync(
                key: $"githubisorgmember{webhookPayload.Repository.Owner.Login}{username}",
                factory: async cacheEntry =>
                {
                    var isMember = await IsUserOrganizationMember(webhookPayload.Repository.Owner.Login, username);
                    cacheEntry.SetValue(isMember);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    return isMember;
                });
        }

        private async Task<bool> IsUserOrganizationMember(string org, string user)
        {
            try
            {
                return await GitHubClient.Organization.Member.CheckMember(org, user);
            }
            catch (NotFoundException)
            {
                return false;
            }
        }

        private Task<bool> IsEnabledForRepository(TPayload webhookPayload)
            => cache.GetOrCreateAsync(
                key: $"ahkmonitorisenabledinrepo{webhookPayload.Repository.Id}",
                factory: async cacheEntry =>
                {
                    var isEnabled = await IsAhkEnabledInRepositoryConfig(webhookPayload);
                    cacheEntry.SetValue(isEnabled);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(12));
                    return isEnabled;
                });

        private async Task<bool> IsAhkEnabledInRepositoryConfig(TPayload webhookPayload)
        {
            try
            {
                var contents = await GitHubClient.Repository.Content.GetAllContentsByRef(webhookPayload.Repository.Id, ".github/ahk-monitor.yml", webhookPayload.Repository.DefaultBranch);
                if (contents.Count == 0)
                    return false;
                var contentAsString = contents[0].Content;
                return ConfigYamlParser.IsEnabled(contentAsString);
            }
            catch (NotFoundException)
            {
                return false;
            }
        }
    }
}
