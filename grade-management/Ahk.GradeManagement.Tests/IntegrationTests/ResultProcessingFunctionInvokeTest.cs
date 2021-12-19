using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.ResultProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GradeManagement.Tests.IntegrationTests
{
    [TestClass]
    public class ResultProcessingFunctionInvokeTest
    {
        [TestMethod]
        public Task MissingTokenHeaderReturnsError()
            => callWebhookAssertIsRejected(req =>
            {
                req.Headers.Add("X-Ahk-TokenXXXXXX", "val");
                req.Headers.Add("X-Ahk-Sha256", "val");
                req.Headers.Add("Date", DateTime.UtcNow.ToString("R"));
            });

        [TestMethod]
        public Task MissingSignatureHeaderReturnsError()
            => callWebhookAssertIsRejected(req =>
            {
                req.Headers.Add("X-Ahk-Token", "val");
                req.Headers.Add("X-Ahk-Sha256XXXXXXX", "val");
                req.Headers.Add("Date", DateTime.UtcNow.ToString("R"));
            });

        [TestMethod]
        public Task MissingDateHeaderReturnsError()
            => callWebhookAssertIsRejected(req =>
            {
                req.Headers.Add("X-Ahk-Token", "val");
                req.Headers.Add("X-Ahk-Sha256", "val");
                req.Headers.Add("DateXXXXX", DateTime.UtcNow.ToString("R"));
            });

        [TestMethod]
        public Task UnparseableDateHeaderReturnsError()
            => callWebhookAssertIsRejected(req =>
            {
                req.Headers.Add("X-Ahk-Token", "val");
                req.Headers.Add("X-Ahk-Sha256", "val");
                req.Headers.Add("Date", "notadate");
            });

        [TestMethod]
        public Task DateHeaderValueOffReturnsError()
            => callWebhookAssertIsRejected(req =>
            {
                req.Headers.Add("X-Ahk-Token", "val");
                req.Headers.Add("X-Ahk-Sha256", "val");
                req.Headers.Add("Date", DateTime.UtcNow.AddMinutes(-25).ToString("R"));
            });

        [TestMethod]
        public Task TokenNotValidReturnsError()
            => callWebhookAssertIsRejected(
                req =>
                {
                    req.Headers.Add("X-Ahk-Token", "notavalidavalue");
                    req.Headers.Add("X-Ahk-Sha256", "val");
                    req.Headers.Add("Date", DateTime.UtcNow.ToString("R"));
                },
                svc => svc.Setup(s => s.GetSecretForToken("notavalidavalue")).ReturnsAsync((string)null).Verifiable());

        [TestMethod]
        public Task SignatureNotValidReturnsError()
        {
            var data = SampleCallbackData.Sample1;
            return callWebhookAssertIsRejected(
                req =>
                {
                    req.Headers.Add("X-Ahk-Token", data.Token);
                    req.Headers.Add("X-Ahk-Sha256", "notvalidsignature");
                    req.Headers.Add("Date", DateTime.UtcNow.ToString("R"));
                },
                svc => svc.Setup(s => s.GetSecretForToken(data.Token)).ReturnsAsync(data.Secret).Verifiable());
        }

        [TestMethod]
        public async Task PayloadNotJsonReturnsError()
        {
            var data = SampleCallbackData.InvalidPayload;

            var procService = new Mock<IResultProcessor>();
            procService.Setup(s => s.GetSecretForToken(data.Token)).ReturnsAsync(data.Secret);

            var dtMock = new Mock<IDateTimeProvider>();
            dtMock.Setup(s => s.GetUtcNow()).Returns(new DateTime(2021, 9, 13, 12, 34, 23, DateTimeKind.Utc));

            var func = new ResultProcessingFunction(procService.Object, dtMock.Object);

            var resp = await func.InvokeWithContentAndGetResponseAs<ObjectResult>(data, dtMock.Object);

            Assert.AreEqual(StatusCodes.Status400BadRequest, resp.StatusCode);
            procService.Verify(s => s.GetSecretForToken(data.Token), Times.Once());
            procService.Verify(s => s.ProcessResult(It.IsAny<ResultProcessing.Dto.AhkProcessResult>(), It.IsAny<DateTime>()), Times.Never());
        }

        [TestMethod]
        public async Task ValidRequestIsProcessed()
        {
            var data = SampleCallbackData.Sample1;

            var procService = new Mock<IResultProcessor>();
            procService.Setup(s => s.GetSecretForToken(data.Token)).ReturnsAsync(data.Secret);

            var dtMock = new Mock<IDateTimeProvider>();
            dtMock.Setup(s => s.GetUtcNow()).Returns(new DateTime(2021, 9, 13, 12, 34, 23, DateTimeKind.Utc));

            var func = new ResultProcessingFunction(procService.Object, dtMock.Object);

            var resp = await func.InvokeWithContentAndGetResponseAs<OkResult>(data, dtMock.Object);

            Assert.AreEqual(StatusCodes.Status200OK, resp.StatusCode);
            procService.Verify(s => s.GetSecretForToken(data.Token), Times.Once());
            procService.Verify(s => s.ProcessResult(It.IsAny<ResultProcessing.Dto.AhkProcessResult>(), It.IsAny<DateTime>()), Times.Once());
        }

        private static async Task callWebhookAssertIsRejected(Action<HttpRequest> configureRequest, Action<Mock<IResultProcessor>> configureProcessorMock = null)
        {
            var procService = new Mock<IResultProcessor>();
            configureProcessorMock?.Invoke(procService);

            var func = new ResultProcessingFunction(procService.Object, new DateTimeProvider());

            var resp = await func.InvokeAndGetResponseAs<ObjectResult>(configureRequest);

            Assert.AreEqual(StatusCodes.Status400BadRequest, resp.StatusCode);
            procService.Verify(s => s.ProcessResult(It.IsAny<ResultProcessing.Dto.AhkProcessResult>(), It.IsAny<DateTime>()), Times.Never());


            if (configureProcessorMock == null)
                procService.Verify(s => s.GetSecretForToken(It.IsAny<string>()), Times.Never());
            else
                procService.Verify();
        }
    }
}
