using Polly;
using Polly.Retry;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services.GitHubClientFactory;

internal class RetryOnServerErrorHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    private readonly AsyncRetryPolicy<HttpResponseMessage> policy =
        Policy.HandleResult<HttpResponseMessage>(m => m.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(3, GetExponentialBackoffSleep);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => policy.ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));

    private static TimeSpan GetExponentialBackoffSleep(int retryAttempt) =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
}
