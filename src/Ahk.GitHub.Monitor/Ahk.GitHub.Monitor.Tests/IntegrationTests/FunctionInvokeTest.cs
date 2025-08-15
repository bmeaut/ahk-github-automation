using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Config;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests;

[TestClass]
public class FunctionInvokeTest
{
    [TestMethod]
    public async Task NoAppConfigsReturnsError()
    {
        var log = new Mock<ILogger<GitHubMonitorFunction>>();
        var eds = new Mock<IEventDispatchService>();
        var mockConfiguration = new Mock<IConfiguration>();
        var func = new GitHubMonitorFunction(eds.Object, log.Object, mockConfiguration.Object);

        ObjectResult resp = await func.InvokeAndGetResponseAs<ObjectResult>(req => { });

        Assert.AreEqual(StatusCodes.Status500InternalServerError, resp.StatusCode);
        eds.Verify(
            s => s.Process(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WebhookResult>(), NullLogger.Instance),
            Times.Never());
    }

    [TestMethod]
    public async Task MissingGitHubEventHeaderReturnsError()
    {
        var eds = new Mock<IEventDispatchService>();
        GitHubMonitorFunction func = FunctionBuilder.Create(eds.Object);

        ObjectResult resp =
            await func.InvokeAndGetResponseAs<ObjectResult>(req => req.Headers.Add("X-Hub-Signature-256", "dummy"));

        Assert.AreEqual(StatusCodes.Status400BadRequest, resp.StatusCode);
        eds.Verify(
            s => s.Process(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WebhookResult>(), NullLogger.Instance),
            Times.Never());
    }

    [TestMethod]
    public async Task MissingGitHubSignatureHeaderReturnsError()
    {
        var eds = new Mock<IEventDispatchService>();
        GitHubMonitorFunction func = FunctionBuilder.Create(eds.Object);

        ObjectResult resp =
            await func.InvokeAndGetResponseAs<ObjectResult>(req => req.Headers.Add("X-GitHub-Event", "dummy"));

        Assert.AreEqual(StatusCodes.Status400BadRequest, resp.StatusCode);
        eds.Verify(
            s => s.Process(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WebhookResult>(), NullLogger.Instance),
            Times.Never());
    }
}
