using Microsoft.Extensions.Options;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests
{
    internal static class FunctionBuilder
    {
        public static readonly GitHubMonitorConfig AppConfig = new GitHubMonitorConfig()
        {
            GitHubAppId = "appid",
            GitHubAppPrivateKey = "appprivatekey",
            GitHubWebhookSecret = "webhooksecret",
        };

        public static GitHubMonitorFunction Create(Services.IEventDispatchService dispatchService = null)
            => new GitHubMonitorFunction(dispatchService ?? new Mock<Services.IEventDispatchService>().Object, Options.Create(AppConfig), new Mock<Microsoft.Extensions.Logging.ILogger<GitHubMonitorFunction>>().Object);
    }
}
