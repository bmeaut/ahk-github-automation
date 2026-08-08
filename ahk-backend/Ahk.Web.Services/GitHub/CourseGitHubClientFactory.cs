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
    /// Carried over from <c>github-monitor</c>'s client factory. Deliberately shorter than it looks like it
    /// should be: a webhook delivery that takes longer than GitHub's own delivery timeout is already lost, so
    /// failing fast beats hanging on to the request.
    /// </summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

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
