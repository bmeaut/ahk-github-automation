using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ahk.Web.Services.GitHub;

/// <summary>A GitHub account, as much of it as this application cares about.</summary>
public sealed record GitHubUser(string Login, long Id);

/// <summary>A GitHub repository, as much of it as this application cares about.</summary>
public sealed record GitHubRepository(string FullName, string HtmlUrl, bool IsTemplate, string? DefaultBranch);

/// <summary>
/// Outcome of sharing a repository with a student. GitHub adds an organization member outright (204) but only
/// *invites* anyone else (201) — and that invitation expires, so the two cases are not interchangeable.
/// </summary>
public sealed record CollaboratorResult(bool InvitationCreated, long? InvitationId);

/// <summary>A pending repository invitation.</summary>
public sealed record GitHubInvitation(long Id, string? InviteeLogin, bool Expired, DateTimeOffset? CreatedAt);

public interface IGitHubRepositoryService
{
    /// <summary>The account behind a login, or null when there is none. This is the typo check.</summary>
    Task<GitHubUser?> GetUserAsync(string login, string? token, CancellationToken cancellationToken = default);

    /// <summary>The repository, or null when it does not exist (or the token cannot see it).</summary>
    Task<GitHubRepository?> GetRepositoryAsync(string owner, string name, string token, CancellationToken cancellationToken = default);

    /// <summary>Creates a private repository from a template repository. The template must be marked as one.</summary>
    Task<GitHubRepository> GenerateFromTemplateAsync(string templateOwner, string templateName, string owner, string name, string token, CancellationToken cancellationToken = default);

    /// <summary>Turns Actions on for a repository. Cheap insurance; a freshly generated repository normally has it already.</summary>
    Task EnsureActionsEnabledAsync(string owner, string name, string token, CancellationToken cancellationToken = default);

    /// <summary>Grants a login push access, directly or by invitation depending on organization membership.</summary>
    Task<CollaboratorResult> AddCollaboratorAsync(string owner, string name, string login, string token, CancellationToken cancellationToken = default);

    /// <summary>Whether the login already has access — the definitive answer to "did they accept the invitation".</summary>
    Task<bool> IsCollaboratorAsync(string owner, string name, string login, string token, CancellationToken cancellationToken = default);

    /// <summary>The outstanding invitation for a login, or null when there is none.</summary>
    Task<GitHubInvitation?> FindInvitationAsync(string owner, string name, string login, string token, CancellationToken cancellationToken = default);

    Task DeleteInvitationAsync(string owner, string name, long invitationId, string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// The GitHub REST calls the assignment flow needs, over the shared named <see cref="HttpClient"/>. Deliberately
/// raw <c>HttpClient</c> + <c>System.Text.Json</c> rather than Octokit: this is the idiom the portal already
/// uses (see <c>Health/GitHubAccessHealthCheck</c>) and a handful of endpoints does not justify a dependency.
///
/// Every method takes the caller's installation token explicitly rather than resolving a course itself, so the
/// service stays a thin, testable transport with no ambient state.
/// </summary>
public sealed class GitHubRepositoryService : IGitHubRepositoryService
{
    private readonly IHttpClientFactory httpClientFactory;

    public GitHubRepositoryService(IHttpClientFactory httpClientFactory) => this.httpClientFactory = httpClientFactory;

    public async Task<GitHubUser?> GetUserAsync(string login, string? token, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);
        using var response = await client.GetAsync(Relative($"users/{Uri.EscapeDataString(login)}"), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, $"looking up the GitHub user '{login}'", cancellationToken);

        using var document = await ReadJsonAsync(response, cancellationToken);
        return new GitHubUser(
            document.RootElement.GetProperty("login").GetString() ?? login,
            document.RootElement.GetProperty("id").GetInt64());
    }

    public async Task<GitHubRepository?> GetRepositoryAsync(string owner, string name, string token, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);
        using var response = await client.GetAsync(Relative($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}"), cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, $"reading the repository '{owner}/{name}'", cancellationToken);

        using var document = await ReadJsonAsync(response, cancellationToken);
        return ReadRepository(document.RootElement, $"{owner}/{name}");
    }

    public async Task<GitHubRepository> GenerateFromTemplateAsync(string templateOwner, string templateName, string owner, string name, string token, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);

        // include_all_branches stays false: the student starts from the template's default branch, which is
        // what the evaluator and the branch-protection rules assume.
        var body = new
        {
            owner,
            name,
            @private = true,
            include_all_branches = false,
        };

        using var response = await client.PostAsJsonAsync(
            Relative($"repos/{Uri.EscapeDataString(templateOwner)}/{Uri.EscapeDataString(templateName)}/generate"),
            body,
            cancellationToken);

        await EnsureSuccessAsync(response, $"creating '{owner}/{name}' from the template '{templateOwner}/{templateName}'", cancellationToken);

        using var document = await ReadJsonAsync(response, cancellationToken);
        return ReadRepository(document.RootElement, $"{owner}/{name}");
    }

    public async Task EnsureActionsEnabledAsync(string owner, string name, string token, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);
        using var response = await client.PutAsJsonAsync(
            Relative($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/actions/permissions"),
            new { enabled = true },
            cancellationToken);

        await EnsureSuccessAsync(response, $"enabling Actions on '{owner}/{name}'", cancellationToken);
    }

    public async Task<CollaboratorResult> AddCollaboratorAsync(string owner, string name, string login, string token, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);
        using var response = await client.PutAsJsonAsync(
            Relative($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/collaborators/{Uri.EscapeDataString(login)}"),
            new { permission = "push" },
            cancellationToken);

        await EnsureSuccessAsync(response, $"granting '{login}' access to '{owner}/{name}'", cancellationToken);

        // 204 means the login was added outright (they are already an organization member). 201 means an
        // invitation was created and the repository stays invisible to them until they accept it.
        if (response.StatusCode == HttpStatusCode.NoContent)
            return new CollaboratorResult(InvitationCreated: false, InvitationId: null);

        using var document = await ReadJsonAsync(response, cancellationToken);
        var id = document.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetInt64() : (long?)null;
        return new CollaboratorResult(InvitationCreated: true, InvitationId: id);
    }

    public async Task<bool> IsCollaboratorAsync(string owner, string name, string login, string token, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);
        using var response = await client.GetAsync(
            Relative($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/collaborators/{Uri.EscapeDataString(login)}"),
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK)
            return true;

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        await EnsureSuccessAsync(response, $"checking whether '{login}' can access '{owner}/{name}'", cancellationToken);
        return false;
    }

    public async Task<GitHubInvitation?> FindInvitationAsync(string owner, string name, string login, string token, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);
        using var response = await client.GetAsync(
            Relative($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/invitations"),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, $"listing invitations of '{owner}/{name}'", cancellationToken);

        using var document = await ReadJsonAsync(response, cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var invitee = element.TryGetProperty("invitee", out var inviteeElement) && inviteeElement.ValueKind == JsonValueKind.Object
                ? inviteeElement.GetProperty("login").GetString()
                : null;

            if (!string.Equals(invitee, login, StringComparison.OrdinalIgnoreCase))
                continue;

            // GitHub reports expiry itself. Never compute it here: the window is GitHub's policy to change,
            // and a stale local constant would have the portal telling students something untrue.
            var expired = element.TryGetProperty("expired", out var expiredElement)
                && expiredElement.ValueKind == JsonValueKind.True;

            var createdAt = element.TryGetProperty("created_at", out var createdElement)
                && createdElement.TryGetDateTimeOffset(out var parsed)
                    ? parsed
                    : (DateTimeOffset?)null;

            return new GitHubInvitation(element.GetProperty("id").GetInt64(), invitee, expired, createdAt);
        }

        return null;
    }

    public async Task DeleteInvitationAsync(string owner, string name, long invitationId, string token, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(token);
        using var response = await client.DeleteAsync(
            Relative($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/invitations/{invitationId}"),
            cancellationToken);

        // Already gone is the desired state, not a failure — the student may have accepted it meanwhile.
        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        await EnsureSuccessAsync(response, $"withdrawing invitation {invitationId} on '{owner}/{name}'", cancellationToken);
    }

    private static Uri Relative(string path) => new(path, UriKind.Relative);

    private static GitHubRepository ReadRepository(JsonElement element, string fallbackFullName)
    {
        var fullName = element.TryGetProperty("full_name", out var full) ? full.GetString() : null;
        var htmlUrl = element.TryGetProperty("html_url", out var url) ? url.GetString() : null;
        var isTemplate = element.TryGetProperty("is_template", out var template) && template.ValueKind == JsonValueKind.True;
        var defaultBranch = element.TryGetProperty("default_branch", out var branch) ? branch.GetString() : null;

        return new GitHubRepository(
            fullName ?? fallbackFullName,
            htmlUrl ?? $"https://github.com/{fullName ?? fallbackFullName}",
            isTemplate,
            defaultBranch);
    }

    private HttpClient CreateClient(string? token)
    {
        var client = httpClientFactory.CreateClient(GitHubApiDefaults.HttpClientName);

        // GET /users/{login} works unauthenticated; it just drops from 5000 to 60 requests an hour.
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await GitHubApiDefaults.ReadErrorMessageAsync(response, cancellationToken);
        throw new GitHubOperationException(operation, response.StatusCode, message);
    }
}
