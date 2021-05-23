using System;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Caching.Memory;
using Octokit;
using Octokit.Internal;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public abstract class RepositoryEventBase<TPayload> : IGitHubEventHandler
        where TPayload : ActivityPayload
    {
        private readonly IGitHubClientFactory gitHubClientFactory;
        private readonly IMemoryCache isEnabledForRepoCache;

        protected IGitHubClient GitHubClient { get; private set; }

        protected RepositoryEventBase(IGitHubClientFactory gitHubClientFactory, IMemoryCache isEnabledForRepoCache)
        {
            this.gitHubClientFactory = gitHubClientFactory;
            this.isEnabledForRepoCache = isEnabledForRepoCache;
        }

        public async Task<EventHandlerResult> Execute(string requestBody)
        {
            if (!tryParsePayload(requestBody, out var webhookPayload, out var errorResult))
                return errorResult;

            GitHubClient = await gitHubClientFactory.CreateGitHubClient(webhookPayload.Installation.Id);

            if (!await isEnabledForRepository(webhookPayload))
                return EventHandlerResult.Disabled("no ahk-monitor.yml or disabled");

            return await execute(webhookPayload);
        }

        protected abstract Task<EventHandlerResult> execute(TPayload webhookPayload);

        protected bool tryParsePayload(string requestBody, out TPayload payload, out EventHandlerResult errorResult)
        {
            payload = null;
            if (string.IsNullOrEmpty(requestBody))
            {
                errorResult = EventHandlerResult.PayloadError("request body was empty");
                return false;
            }

            try
            {
                payload = new SimpleJsonSerializer().Deserialize<TPayload>(requestBody);
            }
            catch (Exception ex)
            {
                errorResult = EventHandlerResult.PayloadError($"request body deserialization failed: {ex.Message}");
                return false;
            }

            if (payload == null)
            {
                errorResult = EventHandlerResult.PayloadError("parsed payload was null or empty");
                return false;
            }

            if (payload.Repository == null)
            {
                errorResult = EventHandlerResult.PayloadError("no repository information in webhook payload");
                return false;
            }

            errorResult = null;
            return true;
        }

        private Task<bool> isEnabledForRepository(TPayload webhookPayload)
            => isEnabledForRepoCache.GetOrCreateAsync(
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
                var contentAsString = contents.FirstOrDefault()?.Content;
                return ConfigYamlParser.IsEnabled(contentAsString);
            }
            catch (NotFoundException)
            {
                return false;
            }
        }
    }
}
