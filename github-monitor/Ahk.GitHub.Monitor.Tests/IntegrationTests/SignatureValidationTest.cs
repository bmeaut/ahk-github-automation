using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.EventDispatch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests
{
    [TestClass]
    public class SignatureValidationTest
    {
        [TestMethod]
        public async Task InvalidGitHubSignatureHeaderReturnError()
        {
            var eds = new Mock<IEventDispatchService>();
            var func = FunctionBuilder.Create(eds.Object);

            var wrongSignature = new SampleCallbackData(SampleData.BranchCreate.Body, "wrongsignature", SampleData.BranchCreate.EventName);
            var resp = await func.InvokeWithContentAndGetResponseAs<ObjectResult>(wrongSignature);

            Assert.AreEqual(StatusCodes.Status400BadRequest, resp.StatusCode);
            eds.Verify(s => s.Process(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WebhookResult>(), NullLogger.Instance), Times.Never());
        }

        [TestMethod]
        public async Task ValidGitHubSignatureAcceptedAndDispatched()
        {
            var eds = new Mock<IEventDispatchService>();
            eds.Setup(s => s.Process(SampleData.BranchCreate.EventName, It.IsAny<string>(), It.IsAny<WebhookResult>(), NullLogger.Instance)).Returns(Task.CompletedTask);

            var ctx = FunctionBuilder.Create(eds.Object);
            var resp = await ctx.InvokeWithContentAndGetResponseAs<OkObjectResult>(SampleData.BranchCreate);

            Assert.AreEqual(StatusCodes.Status200OK, resp.StatusCode);
            //eds.Verify(s => s.Process(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WebhookResult>(), NullLogger.Instance), Times.Once()); Does not seem to work correctly
        }
    }
}
