using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ahk.GitHub.Monitor.Services.GitHubClientFactory
{
    internal class ResponseLoggerHandler(HttpMessageHandler innerHandler, ILogger logger)
        : DelegatingHandler(innerHandler)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                logger.LogWarning(
                    $"Request {request.Method} request to {request.RequestUri} responded with {response.StatusCode}");

            return response;
        }
    }
}
