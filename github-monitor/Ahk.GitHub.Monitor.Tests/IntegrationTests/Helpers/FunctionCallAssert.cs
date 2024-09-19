using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests
{
    internal static class FunctionCallAssert
    {
        public static Task<IActionResult> Invoke(this GitHubMonitorFunction function, Action<MockHttpRequestData> configureRequest)
        {
            // Create a mock function context (you might need to implement this based on your test framework)
            var functionContext = new Mock<FunctionContext>(); // You may need to create or use a mocking framework
            var request = new MockHttpRequestData(functionContext.Object, new MemoryStream());

            // Configure the request (e.g., add headers, set body, etc.)
            configureRequest(request);

            // Invoke the function with the configured request and a null logger
            return function.Run(request);
        }

        public static Task<IActionResult> Invoke2(this GitHubMonitorFunction function, SampleCallbackData data)
        {
            // Create a mock function context (you might need to implement this based on your test framework)
            var functionContext = new Mock<FunctionContext>(); // You may need to create or use a mocking framework
            var request = new MockHttpRequestData(functionContext.Object, new MemoryStream());

            // Configure the request (e.g., add headers, set body, etc.)
            request = configureRequest(request, data);

            // Invoke the function with the configured request and a null logger
            return function.Run(request);
        }

        public static async Task<TResponse> InvokeAndGetResponseAs<TResponse>(this GitHubMonitorFunction function, Action<MockHttpRequestData> configureRequest)
            where TResponse : IActionResult
        {
            var result = await function.Invoke(configureRequest);
            Assert.IsInstanceOfType(result, typeof(TResponse));
            return (TResponse)result;
        }

        public static async Task<TResponse> InvokeWithContentAndGetResponseAs<TResponse>(this GitHubMonitorFunction function, SampleCallbackData data)
            where TResponse : IActionResult
        {
            var result = await function.Invoke2(data);
            Assert.IsInstanceOfType(result, typeof(TResponse));
            return (TResponse)result;
        }

        private static MockHttpRequestData configureRequest(MockHttpRequestData req, SampleCallbackData data)
        {
            // Write the body to the request stream
            var memStream = new MemoryStream();
            using var writer = new StreamWriter(memStream, leaveOpen: true);
            writer.Write(data.Body);
            writer.Flush();

            memStream.Position = 0;
            req = new MockHttpRequestData(req.FunctionContext, memStream);

            // Add headers
            req.Headers.Add("X-GitHub-Event", data.EventName);
            req.Headers.Add("X-Hub-Signature-256", data.Signature);
            return req;
        }

    }
}
