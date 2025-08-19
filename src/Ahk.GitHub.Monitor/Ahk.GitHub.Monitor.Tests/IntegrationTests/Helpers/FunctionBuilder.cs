using System.Collections.Generic;
using Ahk.GitHub.Monitor.Config;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Microsoft.Extensions.Configuration;
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
    {
        var configValues = new Dictionary<string, string>
        {
            { "GitHubMonitorConfig:test:GitHubAppId", AppConfig.GitHubAppId },
            { "GitHubMonitorConfig:test:GitHubAppPrivateKey", AppConfig.GitHubAppPrivateKey },
            { "GitHubMonitorConfig:test:GitHubWebhookSecret", AppConfig.GitHubWebhookSecret }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new GitHubMonitorFunction(
            dispatchService ?? new Mock<IEventDispatchService>().Object, new Mock<ILogger<GitHubMonitorFunction>>().Object, configuration);
    }
}
