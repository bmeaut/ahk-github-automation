using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests
{
    internal static class FunctionCallAssert
    {
        public static Task<IActionResult> Invoke(this GitHubMonitorFunction function, Action<HttpRequest> configureRequest)
        {
            var req = new DefaultHttpRequest(new DefaultHttpContext());
            configureRequest(req);

            return function.Run(req, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        }

        public static async Task<TResponse> InvokeAndGetResponseAs<TResponse>(this GitHubMonitorFunction function, Action<HttpRequest> configureRequest)
            where TResponse : IActionResult
        {
            var result = await function.Invoke(configureRequest);
            Assert.IsInstanceOfType(result, typeof(TResponse));
            return (TResponse)result;
        }

        public static async Task<TResponse> InvokeWithContentAndGetResponseAs<TResponse>(this GitHubMonitorFunction function, SampleCallbackData data)
            where TResponse : IActionResult
        {
            var result = await function.Invoke(req => configureRequest(req, data));
            Assert.IsInstanceOfType(result, typeof(TResponse));
            return (TResponse)result;
        }

        private static void configureRequest(HttpRequest req, SampleCallbackData data)
        {
            req.Headers.Add("X-GitHub-Event", data.EventName);
            req.Headers.Add("X-Hub-Signature", data.Signature);

            var memStream = new System.IO.MemoryStream();
            var writer = new System.IO.StreamWriter(memStream);
            writer.Write(data.Body);
            writer.Flush();

            memStream.Position = 0;
            req.Body = memStream;
        }
    }
}
