using Octokit;
using Octokit.Internal;
using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public abstract class RepositoryEventBase<TPayload>
            where TPayload : ActivityPayload
    {
        protected readonly GitHubClient gitHubClient;
        private readonly string featureFlagName;

        protected RepositoryEventBase(GitHubClient gitHubClient, string featureFlagName)
        {
            this.gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
            this.featureFlagName = featureFlagName;
        }

        public async Task Execute(string requestBody, WebhookResult webhookResult)
        {
            if (!IsFunction.Enabled(featureFlagName))
            {
                webhookResult.LogInfo("monitoring feature disabled");
                return;
            }

            if (!tryParsePayload(requestBody, webhookResult, out var webhookPayload))
                return;

            if (!RepositoryFilterHelper.IsRepositoryOfInterrest(webhookPayload.Repository))
            {
                webhookResult.LogInfo($"repository {webhookPayload.Repository.FullName} is not of interrest");
                return;
            }

            await execute(webhookPayload, webhookResult);
        }

        protected abstract Task execute(TPayload webhookPayload1, WebhookResult webhookResult);

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
    }
}
