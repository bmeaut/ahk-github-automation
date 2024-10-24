using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public abstract class RepositoryEventBase<TPayload>(
        IGitHubClientFactory gitHubClientFactory,
        IMemoryCache cache,
        IServiceProvider serviceProvider)
        : IGitHubEventHandler
        where TPayload : ActivityPayload
    {
        protected readonly ILogger Logger = serviceProvider.GetRequiredService<ILogger<GitHubMonitorFunction>>();

        protected IGitHubClient GitHubClient { get; private set; }

        public async Task<EventHandlerResult> Execute(string requestBody)
        {
            if (!tryParsePayload(requestBody, out var webhookPayload, out var errorResult))
                return errorResult;

            GitHubClient = await gitHubClientFactory.CreateGitHubClient(webhookPayload.Installation.Id, Logger);

            if (!await isEnabledForRepository(webhookPayload))
            {
                Logger.LogInformation("no ahk-monitor.yml or disabled");
                return EventHandlerResult.Disabled("no ahk-monitor.yml or disabled");
            }

            return await executeCore(webhookPayload);
        }

        protected abstract Task<EventHandlerResult> executeCore(TPayload webhookPayload);

        protected bool tryParsePayload(string requestBody, out TPayload payload, out EventHandlerResult errorResult)
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

        protected Task<bool> isUserOrganizationMember(TPayload webhookPayload, string username)
        {
            if (webhookPayload.Repository.Owner.Type != AccountType.Organization)
                return Task.FromResult(false);

            return cache.GetOrCreateAsync(
                key: $"githubisorgmember{webhookPayload.Repository.Owner.Login}{username}",
                factory: async cacheEntry =>
                {
                    var isMember = await getIsUserOrganizationMember(webhookPayload.Repository.Owner.Login, username);
                    cacheEntry.SetValue(isMember);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    return isMember;
                });
        }

        private async Task<bool> getIsUserOrganizationMember(string org, string user)
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

        private Task<bool> isEnabledForRepository(TPayload webhookPayload)
            => cache.GetOrCreateAsync(
                key: $"ahkmonitorisenabledinrepo{webhookPayload.Repository.Id}",
                factory: async cacheEntry =>
                {
                    var isEnabled = await getConfigIsEnabledInRepository(webhookPayload);
                    cacheEntry.SetValue(isEnabled);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(12));
                    return isEnabled;
                });

        private async Task<bool> getConfigIsEnabledInRepository(TPayload webhookPayload)
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
