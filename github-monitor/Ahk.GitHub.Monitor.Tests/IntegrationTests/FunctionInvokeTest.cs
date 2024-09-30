using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests
{
    [TestClass]
    public class FunctionInvokeTest
    {
        [TestMethod]
        public async Task NoAppConfigsReturnsError()
        {
            var log = new Mock<ILogger<GitHubMonitorFunction>>();
            var eds = new Mock<Services.IEventDispatchService>();
            var func = new GitHubMonitorFunction(eds.Object, Options.Create(new GitHubMonitorConfig()), log.Object);

            var resp = await func.InvokeAndGetResponseAs<ObjectResult>(req => { });

            Assert.AreEqual(StatusCodes.Status500InternalServerError, resp.StatusCode);
            eds.Verify(s => s.Process(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WebhookResult>(), NullLogger.Instance), Times.Never());
        }

        [TestMethod]
        public async Task MissingGitHubEventHeaderReturnsError()
        {
            var eds = new Mock<Services.IEventDispatchService>();
            var func = FunctionBuilder.Create(eds.Object);

            var resp = await func.InvokeAndGetResponseAs<ObjectResult>(req => req.Headers.Add("X-Hub-Signature-256", "dummy"));

            Assert.AreEqual(StatusCodes.Status400BadRequest, resp.StatusCode);
            eds.Verify(s => s.Process(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WebhookResult>(), NullLogger.Instance), Times.Never());
        }

        [TestMethod]
        public async Task MissingGitHubSignatureHeaderReturnsError()
        {
            var eds = new Mock<Services.IEventDispatchService>();
            var func = FunctionBuilder.Create(eds.Object);

            var resp = await func.InvokeAndGetResponseAs<ObjectResult>(req => req.Headers.Add("X-GitHub-Event", "dummy"));

            Assert.AreEqual(StatusCodes.Status400BadRequest, resp.StatusCode);
            eds.Verify(s => s.Process(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WebhookResult>(), NullLogger.Instance), Times.Never());
        }
    }
}
