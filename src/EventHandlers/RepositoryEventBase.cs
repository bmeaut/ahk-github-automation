using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Options;
using Octokit;
using Octokit.Internal;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public abstract class RepositoryEventBase<TPayload>
            where TPayload : ActivityPayload
    {
        private readonly IOptions<GitHubMonitorConfig> config;
        private readonly IGitHubClientFactory gitHubClientFactory;

        protected RepositoryEventBase(IOptions<GitHubMonitorConfig> config, IGitHubClientFactory gitHubClientFactory)
        {
            this.config = config;
            this.gitHubClientFactory = gitHubClientFactory;
        }

        public async Task Execute(string requestBody, WebhookResult webhookResult)
        {
            if (!tryParsePayload(requestBody, webhookResult, out var webhookPayload))
                return;

            if (!isRepositoryOfInterrest(webhookPayload.Repository))
            {
                webhookResult.LogInfo($"repository {webhookPayload.Repository.FullName} is not of interrest");
                return;
            }

            var gitHubClient = await gitHubClientFactory.CreateGitHubClient(webhookPayload.Installation.Id);
            await execute(gitHubClient, webhookPayload, webhookResult);
        }

        protected abstract Task execute(GitHubClient gitHubClient, TPayload webhookPayload, WebhookResult webhookResult);

        protected bool tryParsePayload(string requestBody, WebhookResult webhookResult, out TPayload payload)
        {
            payload = null;
            if (string.IsNullOrEmpty(requestBody))
            {
                webhookResult.LogError("request body was empty");
                return false;
            }

            try
            {
                payload = new SimpleJsonSerializer().Deserialize<TPayload>(requestBody);
            }
            catch (Exception ex)
            {
                webhookResult.LogError(ex, "request body deserialization failed");
                return false;
            }

            if (payload == null)
            {
                webhookResult.LogError("parsed payload was null or empty");
                return false;
            }

            if (payload.Repository == null)
            {
                webhookResult.LogError("no repository information in webhook payload");
                return false;
            }

            return true;
        }

        private bool isRepositoryOfInterrest(Repository repository)
        {
            if (string.IsNullOrEmpty(config.Value.Repositories))
                return true;

            return config.Value.Repositories.Split(';').Any(prefix => doesRepositoryMatchPrefix(repository, prefix));
        }

        private static bool doesRepositoryMatchPrefix(Repository repository, string prefix)
        {
            if (prefix.Contains('/'))
                return repository.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            else
                return repository.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
