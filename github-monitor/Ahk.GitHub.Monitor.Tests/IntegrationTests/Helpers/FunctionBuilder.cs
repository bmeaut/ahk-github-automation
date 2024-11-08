using Ahk.GitHub.Monitor.Services.EventDispatch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests;

internal static class FunctionBuilder
{
    public static readonly GitHubMonitorConfig AppConfig = new()
    {
        GitHubAppId = "appid", GitHubAppPrivateKey = "appprivatekey", GitHubWebhookSecret = "webhooksecret"
    };

    public static GitHubMonitorFunction Create(IEventDispatchService dispatchService = null)
        => new(dispatchService ?? new Mock<IEventDispatchService>().Object,
            Options.Create(AppConfig),
            new Mock<ILogger<GitHubMonitorFunction>>().Object);
}
