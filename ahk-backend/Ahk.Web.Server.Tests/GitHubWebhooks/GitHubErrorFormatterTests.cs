using System.Net;
using Ahk.Web.Services.GitHub;
using Moq;
using Octokit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// <see cref="GitHubErrorFormatter"/> — the delivery log has to name what GitHub returned.
///
/// <para>The case that motivated all of this is <see cref="AGatewayFailureWithNoBodyStillNamesItsStatus"/>: a
/// production failure arrived as the bare text "An error occurred with this API request", because Octokit's
/// <c>ApiException.Message</c> falls back to that whenever GitHub answers without a parseable <c>message</c>
/// — which is exactly what a gateway 5xx does. The status code was on the exception the whole time.</para>
/// </summary>
public class GitHubErrorFormatterTests
{
    /// <summary>
    /// A stand-in for GitHub's response. <c>Octokit.Internal.Response</c> is not public, so this is the only
    /// way to build an <see cref="ApiException"/> that carries headers and a body. Shared with
    /// <see cref="GitHubCallRetryTests"/>, which needs one to construct a <see cref="ForbiddenException"/>.
    /// </summary>
    internal static IResponse Response(
        HttpStatusCode status,
        object? body = null,
        IReadOnlyDictionary<string, string>? headers = null,
        string contentType = "application/json")
    {
        var mock = new Mock<IResponse>();
        mock.SetupGet(r => r.StatusCode).Returns(status);
        mock.SetupGet(r => r.Body).Returns(body!);
        mock.SetupGet(r => r.ContentType).Returns(contentType);
        mock.SetupGet(r => r.Headers).Returns(headers ?? new Dictionary<string, string>());
        mock.SetupGet(r => r.ApiInfo).Returns(new ApiInfo(
            new Dictionary<string, Uri>(), [], [], "etag", new RateLimit(5000, 4999, 0), TimeSpan.Zero));

        return mock.Object;
    }

    [Fact]
    public void AGatewayFailureWithNoBodyStillNamesItsStatus()
    {
        var exception = new ApiException(Response(HttpStatusCode.BadGateway, body: string.Empty));

        var described = GitHubErrorFormatter.Describe(exception);

        Assert.Contains("502", described, StringComparison.Ordinal);
        Assert.Contains("BadGateway", described, StringComparison.Ordinal);
        Assert.Contains("(no message in response body)", described, StringComparison.Ordinal);
    }

    [Fact]
    public void GitHubsOwnMessageAndRequestIdAreKept()
    {
        var exception = new ApiException(Response(
            HttpStatusCode.UnprocessableEntity,
            body: """{"message":"Validation Failed","errors":[{"resource":"PullRequest","field":"base","code":"invalid"}]}""",
            headers: new Dictionary<string, string> { ["X-GitHub-Request-Id"] = "ABCD:1234" }));

        var described = GitHubErrorFormatter.Describe(exception);

        Assert.Contains("Validation Failed", described, StringComparison.Ordinal);
        Assert.Contains("PullRequest/base/invalid", described, StringComparison.Ordinal);

        // Case-insensitively, because Octokit makes no promise about the header dictionary's comparer.
        Assert.Contains("request-id: ABCD:1234", described, StringComparison.Ordinal);
    }

    [Fact]
    public void AnHtmlErrorPageIsFlattenedToOneLine()
    {
        var exception = new ApiException(Response(
            HttpStatusCode.ServiceUnavailable,
            body: "<html>\n  <body>\n    Sorry, something went wrong.\n  </body>\n</html>",
            contentType: "text/html"));

        var described = GitHubErrorFormatter.Describe(exception);

        Assert.DoesNotContain('\n', described);
        Assert.Contains("Sorry, something went wrong.", described, StringComparison.Ordinal);
    }

    /// <summary>
    /// Octokit hands back a byte array for content types it does not treat as text, and that alone explains an
    /// empty <c>ApiError.Message</c> — so it is worth saying rather than dropping.
    /// </summary>
    [Fact]
    public void ANonTextBodyIsReportedRatherThanDropped()
    {
        var exception = new ApiException(Response(
            HttpStatusCode.InternalServerError, body: new byte[42], contentType: "application/octet-stream"));

        var described = GitHubErrorFormatter.Describe(exception);

        Assert.Contains("42 bytes", described, StringComparison.Ordinal);
    }

    /// <summary>⚠️ <c>ApiException.HttpResponse</c> is null for the exceptions built from status alone.</summary>
    [Fact]
    public void AnApiExceptionWithoutAResponseDoesNotThrow()
    {
        var described = GitHubErrorFormatter.Describe(new ApiException("boom", HttpStatusCode.InternalServerError));

        Assert.Contains("500", described, StringComparison.Ordinal);
    }

    [Fact]
    public void OurOwnTimeoutIsNotPresentedAsAGitHubError()
    {
        var described = GitHubErrorFormatter.Describe(
            new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing."));

        Assert.Contains("the portal's own GitHub request timeout elapsed", described, StringComparison.Ordinal);
    }

    [Fact]
    public void SummarizeKeepsTheStackBehindTheHeadline()
    {
        var exception = new ApiException(Response(HttpStatusCode.BadGateway));

        var summary = GitHubErrorFormatter.Summarize(exception);

        Assert.StartsWith(GitHubErrorFormatter.Describe(exception), summary, StringComparison.Ordinal);
        Assert.Contains(nameof(ApiException), summary, StringComparison.Ordinal);
    }
}
