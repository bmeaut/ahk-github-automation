using System;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Octokit;
using Octokit.Internal;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public abstract class RepositoryEventBase<TPayload> : IGitHubEventHandler
        where TPayload : ActivityPayload
    {
        private static readonly YamlDotNet.Serialization.IDeserializer YamlDeserializer = CreateYamlDeserializer();

        private readonly IGitHubClientFactory gitHubClientFactory;

        protected IGitHubClient GitHubClient { get; private set; }

        protected RepositoryEventBase(IGitHubClientFactory gitHubClientFactory)
        {
            this.gitHubClientFactory = gitHubClientFactory;
        }

        public async Task<EventHandlerResult> Execute(string requestBody)
        {
            if (!tryParsePayload(requestBody, out var webhookPayload, out var errorResult))
                return errorResult;

            GitHubClient = await gitHubClientFactory.CreateGitHubClient(webhookPayload.Installation.Id);
            var (repoSettings, repoSettingsErrorResult) = await tryGetRepositorySettings(webhookPayload);

            if (repoSettings == null)
                return repoSettingsErrorResult;

            if (!repoSettings.Enabled)
                return EventHandlerResult.Disabled($"ahk-monitor.yml disabled app for repository {webhookPayload.Repository.FullName}");

            return await execute(webhookPayload, repoSettings);
        }

        protected abstract Task<EventHandlerResult> execute(TPayload webhookPayload, RepositorySettings repoSettings);

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

        private static YamlDotNet.Serialization.IDeserializer CreateYamlDeserializer()
            => new YamlDotNet.Serialization.DeserializerBuilder()
                    .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

        private async Task<(RepositorySettings, EventHandlerResult)> tryGetRepositorySettings(TPayload webhookPayload)
        {
            try
            {
                var contents = await GitHubClient.Repository.Content.GetAllContentsByRef(webhookPayload.Repository.Id, ".github/ahk-monitor.yml", webhookPayload.Repository.DefaultBranch);
                var settingsString = contents.FirstOrDefault()?.Content;

                if (settingsString == null)
                    return (null, EventHandlerResult.Disabled("repository has no ahk-monitor.yml"));

                try
                {
                    return (YamlDeserializer.Deserialize<RepositorySettings>(settingsString), null);
                }
                catch (Exception ex)
                {
                    return (null, EventHandlerResult.PayloadError("Config yaml parse error: " + ex.Message));
                }
            }
            catch (NotFoundException)
            {
                return (null, EventHandlerResult.Disabled("repository has no ahk-monitor.yml"));
            }
        }
    }
}
