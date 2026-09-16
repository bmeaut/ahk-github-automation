using System.Net;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Ahk.Web.Services.GitHub;

/// <summary>
/// Retries a GitHub call that is safe to make twice. GitHub's API fails transiently at a low but real rate —
/// roughly one delivery in two thousand — and a webhook handler that gives up on the first 502 turns that
/// blip into permanent damage: a branch left unprotected, a status never recorded.
///
/// <para>⚠️ <strong>Idempotent calls only.</strong> Every call this wraps must be a read, or a write whose
/// second application is indistinguishable from its first. It must never wrap a comment, a review, a merge or
/// a reaction: a timeout is not proof that GitHub did nothing, and a 504 on a merge commonly means "slow",
/// not "didn't happen" — so a retry there posts a duplicate or merges twice. The list of what is wrapped, and
/// what is deliberately not, lives with the handlers.</para>
///
/// <para>The original exception is re-thrown once the attempts are spent, rather than translated the way
/// <c>GitHubRepositoryService.ExecuteAsync</c> translates to <see cref="GitHubOperationException"/>: the
/// webhook dispatcher records the exception through <see cref="GitHubErrorFormatter"/>, which needs the real
/// <see cref="ApiException"/> to read the status off.</para>
/// </summary>
public static class GitHubCallRetry
{
    /// <summary>Attempts in total, the first included.</summary>
    private const int MaxAttempts = 3;

    /// <summary>
    /// Waits between attempts, by attempt number; running off the end reuses the last, as
    /// <c>WebhookOptions.RetryBackoff</c> does. Short on purpose — this backs off a blip, not an outage, and
    /// it spends a delivery worker that processes one delivery at a time.
    /// </summary>
    private static readonly TimeSpan[] Backoff = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];

    /// <summary>
    /// Runs <paramref name="call"/>, retrying it on a transient GitHub failure.
    /// </summary>
    /// <param name="operation">What is being attempted, for the log line. Not shown to students.</param>
    /// <param name="call">The call. Must be idempotent — see the type's remarks.</param>
    /// <param name="logger">Gets one warning per retry. A call that succeeds on a retry is otherwise silent:
    /// the delivery row reads exactly as a first-attempt success, which is what makes this safe to apply
    /// widely.</param>
    /// <param name="cancellationToken">The delivery's budget. Checked before every attempt and every wait, so
    /// retries stay inside <c>WebhookOptions.DeliveryTimeout</c> and stop at shutdown.</param>
    public static async Task<T> IdempotentAsync<T>(
        string operation,
        Func<Task<T>> call,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(logger);

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await call();
            }

            // ⚠️ The cancellation check belongs in the filter, not the body: Octokit's methods take no
            // cancellation token, so a cancellation raised inside the call is always our own request timeout —
            // but our token may have tripped meanwhile, and retrying then would outlive the delivery budget.
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex) && !cancellationToken.IsCancellationRequested)
            {
                var delay = Backoff[Math.Min(attempt - 1, Backoff.Length - 1)];

                logger.LogWarning(
                    ex,
                    "GitHub call {Operation} failed on attempt {Attempt} of {MaxAttempts}, retrying in {Delay}. {GitHubError}",
                    operation,
                    attempt,
                    MaxAttempts,
                    delay,
                    GitHubErrorFormatter.Describe(ex));

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Whether the failure is worth a second attempt. Everything GitHub decided on purpose is not: the answer
    /// would be identical, and the attempts are better spent elsewhere.
    /// </summary>
    private static bool IsTransient(Exception exception) => exception switch
    {
        // ⚠️ Must precede the ApiException arm — all four are subclasses.
        //
        // ForbiddenException covers RateLimitExceededException and SecondaryRateLimitExceededException, both of
        // which GitHub signals as 403. Retrying a rate limit a second later cannot help (the reset is minutes
        // or an hour away) and a *secondary* limit is made worse by exactly this behaviour.
        ForbiddenException or NotFoundException or ApiValidationException or AuthorizationException => false,

        ApiException api => IsTransientStatus(api.StatusCode),

        // Our own request timeout, surfaced by HttpClient. See the filter above for why this cannot be the
        // caller's cancellation.
        TaskCanceledException or TimeoutException => true,

        _ => false,
    };

    private static bool IsTransientStatus(HttpStatusCode status) =>
        (int)status >= 500
        || status == HttpStatusCode.RequestTimeout
        || status == HttpStatusCode.TooManyRequests;
}
