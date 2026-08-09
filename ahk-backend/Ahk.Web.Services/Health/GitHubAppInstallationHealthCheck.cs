using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHub;

namespace Ahk.Web.Services.Health;

/// <summary>
/// Verifies the course's GitHub App can actually act on its organization: that the credentials mint an
/// installation token at all, that the installation covers every repository, and that it was granted
/// <c>administration: write</c>.
///
/// That last one is the whole point. Creating a repository from a template and adding a student as a
/// collaborator both sit behind it, so without this check the first sign of a missing permission would be a
/// student staring at a failed invite. Here an administrator sees it on the dashboard instead.
/// </summary>
public sealed class GitHubAppInstallationHealthCheck : ICourseHealthCheck
{
    /// <summary>The dashboard runs every course at once; no single check may hold it open.</summary>
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(10);

    private readonly ICourseGitHubAppTokenProvider tokens;

    public GitHubAppInstallationHealthCheck(ICourseGitHubAppTokenProvider tokens) => this.tokens = tokens;

    public string Id => "github-app-installation";

    public string Title => "GitHub App installation";

    public int Order => 25;

    public async Task<HealthCheckResult> RunAsync(Course course, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(course);

        if (string.IsNullOrWhiteSpace(course.GitHubConfig?.GitHubAppId) || string.IsNullOrWhiteSpace(course.GitHubConfig?.GitHubAppPrivateKey))
        {
            return HealthCheckResult.NotConfigured(
                this,
                "No GitHub App id and private key are stored for this course.",
                "Register a GitHub App for the organization and add its id and private key under GitHub integration. See docs/github-app.md.");
        }

        if (string.IsNullOrWhiteSpace(course.GitHubOrganization))
        {
            return HealthCheckResult.Failed(
                this,
                "The course has no GitHub organization, so the App installation cannot be located.",
                "Set the course's GitHub organization.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Budget);

        try
        {
            // bypassCache: a health check that reports a cached success from 40 minutes ago is not a health check.
            var token = await tokens.GetForCourseAsync(course, bypassCache: true, timeout.Token);
            if (token is null)
            {
                return HealthCheckResult.NotConfigured(
                    this,
                    "The GitHub App is not fully configured for this course.",
                    "Check the App id, private key and organization under GitHub integration.");
            }

            var granted = token.Permissions.Count == 0
                ? "none"
                : string.Join(", ", token.Permissions.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}: {p.Value}"));

            if (!token.HasAdministrationWrite)
            {
                return HealthCheckResult.Failed(
                    this,
                    $"Installation {token.InstallationId} works, but it was not granted 'administration: write'. Assignments cannot create repositories or add students. Granted: {granted}.",
                    "Edit the App's repository permissions, set Administration to Read & write, and accept the new permissions on the organization's installation.");
            }

            // "selected" means new repositories fall outside the installation, so the collaborator call that
            // follows repository creation would 404 on a repository the App itself just made.
            if (!string.Equals(token.RepositorySelection, "all", StringComparison.Ordinal))
            {
                return HealthCheckResult.Warning(
                    this,
                    $"Installation {token.InstallationId} is limited to selected repositories, so repositories created for students fall outside it.",
                    "Change the installation's repository access to 'All repositories'.");
            }

            return HealthCheckResult.Healthy(this, $"Installation {token.InstallationId} covers all repositories and can administer them.");
        }
        catch (GitHubOperationException ex)
        {
            return HealthCheckResult.Failed(this, ex.Message, RemediationFor(ex));
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            return HealthCheckResult.Failed(
                this,
                $"The stored private key could not be read: {ex.Message}",
                "Re-download the App's private key (.pem) from GitHub and paste its full contents under GitHub integration.");
        }
        catch (FormatException)
        {
            return HealthCheckResult.Failed(
                this,
                "The stored private key is not valid PEM or base64.",
                "Re-download the App's private key (.pem) from GitHub and paste its full contents under GitHub integration.");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Failed(this, $"GitHub could not be reached: {ex.Message}", "Check network access from the server to api.github.com.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Failed(this, "GitHub did not respond within 10 seconds.", "Check network access from the server to api.github.com.");
        }
    }

    private static string RemediationFor(GitHubOperationException ex) => ex.Status switch
    {
        System.Net.HttpStatusCode.NotFound => "The App is registered but not installed on this organization. Install it, with access to all repositories.",
        System.Net.HttpStatusCode.Unauthorized => "The App id or private key is wrong, or the key was revoked. Generate a new key and store it again.",
        _ => "Check the App registration and its installation on the organization.",
    };
}
