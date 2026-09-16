using System.Net;
using Ahk.Web.Services.GitHub;
using Microsoft.Extensions.Logging.Abstractions;
using Octokit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// <see cref="GitHubCallRetry"/> — what is retried, what is not, and how many times.
///
/// <para>The exclusions matter more than the retries. Retrying something GitHub refused on purpose wastes the
/// attempts, and for a secondary rate limit it is what provokes the limit in the first place; these tests are
/// what stop a later "why isn't this retried?" from quietly widening the set.</para>
/// </summary>
public class GitHubCallRetryTests
{
    private static ApiException Status(HttpStatusCode status) => new("boom", status);

    [Fact]
    public async Task TransientFailureIsRetriedAndTheResultReturned()
    {
        var attempts = 0;

        var result = await GitHubCallRetry.IdempotentAsync(
            "test",
            () =>
            {
                attempts++;
                return attempts == 1 ? throw Status(HttpStatusCode.BadGateway) : Task.FromResult(42);
            },
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task AttemptsAreSpentAndTheOriginalExceptionRethrown()
    {
        var attempts = 0;

        var thrown = await Assert.ThrowsAsync<ApiException>(() => GitHubCallRetry.IdempotentAsync<int>(
            "test",
            () =>
            {
                attempts++;
                throw Status(HttpStatusCode.InternalServerError);
            },
            NullLogger.Instance,
            CancellationToken.None));

        Assert.Equal(3, attempts);

        // The original exception, not a translation of it: the dispatcher's formatter reads the status off it.
        Assert.Equal(HttpStatusCode.InternalServerError, thrown.StatusCode);
    }

    [Fact]
    public async Task OwnRequestTimeoutIsRetried()
    {
        var attempts = 0;

        var result = await GitHubCallRetry.IdempotentAsync(
            "test",
            () =>
            {
                attempts++;
                return attempts == 1
                    ? throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing.")
                    : Task.FromResult("ok");
            },
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(2, attempts);
    }

    /// <summary>
    /// ⚠️ Rate limiting arrives as 403, so <see cref="RateLimitExceededException"/> is a
    /// <see cref="ForbiddenException"/>. Retrying it a second later cannot help — the reset is minutes or an
    /// hour away — and a *secondary* limit is made worse by exactly that. None of these may become retryable.
    /// </summary>
    [Theory]
    [InlineData("not-found")]
    [InlineData("validation")]
    [InlineData("authorization")]
    [InlineData("forbidden")]
    [InlineData("rate-limit")]
    [InlineData("bad-request")]
    [InlineData("conflict")]
    public async Task WhatGitHubRefusedOnPurposeIsNotRetried(string refusal)
    {
        var attempts = 0;

        await Assert.ThrowsAnyAsync<ApiException>(() => GitHubCallRetry.IdempotentAsync<int>(
            "test",
            () =>
            {
                attempts++;
                throw Refusal(refusal);
            },
            NullLogger.Instance,
            CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task AnAlreadyCancelledTokenRunsNothing()
    {
        var attempts = 0;
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GitHubCallRetry.IdempotentAsync(
            "test",
            () =>
            {
                attempts++;
                return Task.FromResult(1);
            },
            NullLogger.Instance,
            cancelled.Token));

        Assert.Equal(0, attempts);
    }

    /// <summary>
    /// The delivery budget tripping mid-call must end the call rather than start another attempt, or the
    /// retries outlive <c>WebhookOptions.DeliveryTimeout</c> and the single worker stays wedged past it.
    /// </summary>
    [Fact]
    public async Task CancellationDuringTheCallStopsTheRetries()
    {
        var attempts = 0;
        using var budget = new CancellationTokenSource();

        await Assert.ThrowsAsync<ApiException>(() => GitHubCallRetry.IdempotentAsync<int>(
            "test",
            async () =>
            {
                attempts++;
                await budget.CancelAsync();
                throw Status(HttpStatusCode.ServiceUnavailable);
            },
            NullLogger.Instance,
            budget.Token));

        Assert.Equal(1, attempts);
    }

    private static ApiException Refusal(string kind) => kind switch
    {
        "not-found" => new NotFoundException("nope", HttpStatusCode.NotFound),
        "validation" => new ApiValidationException(),
        "authorization" => new AuthorizationException(),
        "forbidden" => new ForbiddenException(GitHubErrorFormatterTests.Response(HttpStatusCode.Forbidden)),
        "rate-limit" => new RateLimitExceededException(GitHubErrorFormatterTests.Response(HttpStatusCode.Forbidden)),
        "bad-request" => Status(HttpStatusCode.BadRequest),
        "conflict" => Status(HttpStatusCode.Conflict),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
