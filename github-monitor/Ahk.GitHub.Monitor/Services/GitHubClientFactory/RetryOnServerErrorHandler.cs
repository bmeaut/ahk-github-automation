using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace Ahk.GitHub.Monitor.Services
{
    internal class RetryOnServerErrorHandler : DelegatingHandler
    {
        private readonly AsyncRetryPolicy<HttpResponseMessage> policy =
            Policy.HandleResult<HttpResponseMessage>(m => m.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: getExponentialBackoffSleep);

        public RetryOnServerErrorHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => policy.ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));

        private static TimeSpan getExponentialBackoffSleep(int retryAttempt) => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
    }
}
