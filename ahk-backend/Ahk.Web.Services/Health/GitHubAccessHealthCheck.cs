using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Ahk.Web.Data.Entities;

namespace Ahk.Web.Services.Health;

/// <summary>
/// Verifies that the course's GitHub access token still works and reaches the course's organization.
/// Two calls, both cheap: <c>GET /user</c> proves the token is valid, <c>GET /orgs/{org}</c> proves it can see
/// the organization the course's repositories live in.
///
/// This is the only check that leaves the process, so it carries its own short timeout — the admin dashboard
/// runs it for every course at once and must stay responsive when GitHub is slow.
/// </summary>
public sealed class GitHubAccessHealthCheck : ICourseHealthCheck
{
    /// <summary>Named <see cref="HttpClient"/> registered by <c>AddAhkServices</c>.</summary>
    public const string HttpClientName = "github-health";

    private readonly IHttpClientFactory httpClientFactory;

    public GitHubAccessHealthCheck(IHttpClientFactory httpClientFactory) => this.httpClientFactory = httpClientFactory;

    public string Id => "github-access-token";

    public string Title => "GitHub access token";

    public int Order => 20;

    public async Task<HealthCheckResult> RunAsync(Course course, CancellationToken cancellationToken = default)
    {
        var token = course.GitHubConfig?.GitHubAccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return HealthCheckResult.NotConfigured(
                this,
                "No access token is stored for this course.",
                "Add a token under GitHub integration to let the portal talk to GitHub.");
        }

        using var client = httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var (identity, failure) = await GetLoginAsync(client, "user", cancellationToken);
        if (failure is not null)
            return failure;

        if (string.IsNullOrWhiteSpace(course.GitHubOrganization))
        {
            return HealthCheckResult.Warning(
                this,
                $"The token is valid (authenticated as {identity}), but the course has no GitHub organization to check against.",
                "Set the course's GitHub organization so repository access can be verified.");
        }

        var (org, orgFailure) = await GetLoginAsync(client, $"orgs/{Uri.EscapeDataString(course.GitHubOrganization)}", cancellationToken);
        if (orgFailure is not null)
        {
            // A valid token that cannot see the org is the interesting failure: report both facts.
            return HealthCheckResult.Failed(
                this,
                $"The token is valid (authenticated as {identity}) but cannot read the organization '{course.GitHubOrganization}'. {orgFailure.Message}",
                $"Grant the token access to '{course.GitHubOrganization}', or correct the organization on the course.");
        }

        return HealthCheckResult.Healthy(this, $"Authenticated as {identity}; organization '{org}' is reachable.");
    }

    /// <summary>
    /// Calls a GitHub endpoint and returns its <c>login</c> field. The failure result is returned rather than
    /// thrown so a single unreachable course cannot fail the whole dashboard.
    /// </summary>
    private async Task<(string? Login, HealthCheckResult? Failure)> GetLoginAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(path, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return (null, HealthCheckResult.Failed(this, DescribeFailure(response.StatusCode), RemediationFor(response.StatusCode)));

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var login = document.RootElement.TryGetProperty("login", out var value) ? value.GetString() : null;
            return (login ?? "unknown", null);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, HealthCheckResult.Failed(this, "GitHub did not respond within 10 seconds.", "Check network access from the server to api.github.com."));
        }
        catch (HttpRequestException ex)
        {
            return (null, HealthCheckResult.Failed(this, $"GitHub could not be reached: {ex.Message}", "Check network access from the server to api.github.com."));
        }
        catch (JsonException)
        {
            return (null, HealthCheckResult.Failed(this, "GitHub returned a response the portal could not read.", "Retry; if it persists, check whether the API base address is correct."));
        }
    }

    private static string DescribeFailure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "GitHub rejected the token (401). It is invalid, revoked or expired.",
        HttpStatusCode.Forbidden => "GitHub refused the request (403). The token lacks the required scope, or the rate limit is exhausted.",
        HttpStatusCode.NotFound => "GitHub returned 404. The organization does not exist, or the token cannot see it.",
        _ => $"GitHub returned {(int)status} {status}.",
    };

    private static string RemediationFor(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Issue a new access token and save it under GitHub integration.",
        HttpStatusCode.Forbidden => "Give the token the read:org and repo scopes, then try again.",
        HttpStatusCode.NotFound => "Check the organization name on the course and the token's access to it.",
        _ => "Retry the check; if it persists, verify the token on GitHub.",
    };
}
