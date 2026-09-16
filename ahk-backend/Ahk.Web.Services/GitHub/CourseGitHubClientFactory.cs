using Octokit;
using Octokit.Internal;

namespace Ahk.Web.Services.GitHub;

public interface ICourseGitHubClientFactory
{
    /// <summary>
    /// An Octokit client authenticated as the course's GitHub App installation, or null when the course has no
    /// working App credentials. Null rather than an exception, matching
    /// <see cref="ICourseGitHubAppTokenProvider.GetForCourseAsync(int, bool, CancellationToken)"/>.
    /// </summary>
    Task<IGitHubClient?> CreateForCourseAsync(int courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// An Octokit client for a token the caller already holds. A null or empty token yields an anonymous
    /// client, which still works for public reads at GitHub's 60-requests-an-hour rate.
    /// </summary>
    IGitHubClient CreateForToken(string? token);
}

/// <summary>
/// Builds the portal's Octokit clients. Every GitHub API call the portal makes goes through one of these — the
/// assignment flow, the webhook handlers and the chatops commands alike.
///
/// Deliberately not routed through the named <c>"github"</c> <see cref="HttpClient"/>: Octokit wants a
/// <c>Func&lt;HttpMessageHandler&gt;</c> rather than a configured client, and the named client's
/// <c>BaseAddress</c> and default headers would fight Octokit's own. That named client stays in use by
/// <see cref="CourseGitHubAppTokenProvider"/> (the App-JWT bootstrap, which is not an API call) and by the
/// health checks.
/// </summary>
public sealed class CourseGitHubClientFactory : ICourseGitHubClientFactory
{
    /// <summary>
    /// How long one GitHub call may take. It was 15 seconds, carried over from <c>github-monitor</c>, where a
    /// handler ran inside GitHub's ten-second webhook delivery request and anything slower was lost anyway.
    /// That constraint is gone: the receiver answers 202 before any handler runs, and the worker has
    /// <c>WebhookOptions.DeliveryTimeout</c> — five minutes — to finish. The old value was simply failing
    /// deliveries on GitHub's slower endpoints.
    ///
    /// <para>Bound on a wrapped call, with <see cref="GitHubCallRetry"/>'s three attempts and its 1s/3s
    /// backoff: about 94 seconds worst case, and the retry checks the delivery's budget between attempts, so
    /// it stays inside the five minutes.</para>
    /// </summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private static readonly ProductHeaderValue Product = new("ahk-portal", "1.0");

    private readonly ICourseGitHubAppTokenProvider tokenProvider;

    public CourseGitHubClientFactory(ICourseGitHubAppTokenProvider tokenProvider) => this.tokenProvider = tokenProvider;

    public async Task<IGitHubClient?> CreateForCourseAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var token = await tokenProvider.GetForCourseAsync(courseId, bypassCache: false, cancellationToken);
        return token is null ? null : CreateForToken(token.Token);
    }

    public IGitHubClient CreateForToken(string? token)
    {
        var credentials = string.IsNullOrWhiteSpace(token)
            ? Credentials.Anonymous
            : new Credentials(token);

        var connection = new Connection(Product, new InMemoryCredentialStore(credentials));
        var client = new GitHubClient(connection);
        client.SetRequestTimeout(RequestTimeout);

        return client;
    }
}
