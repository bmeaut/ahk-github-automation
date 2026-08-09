using System.Net;
using Octokit;

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
/// The GitHub REST calls the assignment flow needs, over Octokit.
///
/// The interface and its four record types are deliberately Octokit-free. They are narrow projections that keep
/// <c>AssignmentInviteService</c> and every test double away from Octokit's large, awkward-to-construct models,
/// and they are what lets <c>AssignmentInviteTests</c> mock this service strictly.
///
/// Every method takes the caller's installation token explicitly rather than resolving a course itself, so the
/// service stays a thin, testable transport with no ambient state.
/// </summary>
public sealed class GitHubRepositoryService : IGitHubRepositoryService
{
    private readonly ICourseGitHubClientFactory clientFactory;

    public GitHubRepositoryService(ICourseGitHubClientFactory clientFactory) => this.clientFactory = clientFactory;

    public Task<GitHubUser?> GetUserAsync(string login, string? token, CancellationToken cancellationToken = default)
    {
        // GET /users/{login} works unauthenticated; the token only lifts the rate limit from 60 to 5000/hour.
        var client = clientFactory.CreateForToken(token);

        return ExecuteAsync<GitHubUser?>(
            $"looking up the GitHub user '{login}'",
            async () =>
            {
                var user = await client.User.Get(login);
                return new GitHubUser(user.Login ?? login, user.Id);
            },
            notFound: () => null);
    }

    public Task<GitHubRepository?> GetRepositoryAsync(string owner, string name, string token, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateForToken(token);

        return ExecuteAsync<GitHubRepository?>(
            $"reading the repository '{owner}/{name}'",
            async () => ToRepository(await client.Repository.Get(owner, name), $"{owner}/{name}"),
            notFound: () => null);
    }

    public Task<GitHubRepository> GenerateFromTemplateAsync(string templateOwner, string templateName, string owner, string name, string token, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateForToken(token);

        // include_all_branches is not sent, and GitHub defaults it to false: the student starts from the
        // template's default branch, which is what the evaluator and the branch-protection rules assume.
        var request = new NewRepositoryFromTemplate(name)
        {
            Owner = owner,
            Private = true,
        };

        return ExecuteAsync(
            $"creating '{owner}/{name}' from the template '{templateOwner}/{templateName}'",
            async () => ToRepository(await client.Repository.Generate(templateOwner, templateName, request), $"{owner}/{name}"));
    }

    public Task EnsureActionsEnabledAsync(string owner, string name, string token, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateForToken(token);

        // Octokit has no first-class client for the Actions permissions endpoint; Connection keeps it on the
        // same authenticated client rather than opening a second transport for one call.
        return ExecuteAsync(
            $"enabling Actions on '{owner}/{name}'",
            async () =>
            {
                await client.Connection.Put(
                    new Uri($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/actions/permissions", UriKind.Relative),
                    new { enabled = true });
                return true;
            });
    }

    public Task<CollaboratorResult> AddCollaboratorAsync(string owner, string name, string login, string token, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateForToken(token);

        return ExecuteAsync(
            $"granting '{login}' access to '{owner}/{name}'",
            async () =>
            {
                // GitHub answers 204 when the login was added outright (they are already an organization
                // member) and 201 with the invitation when it was not. Octokit surfaces that as null vs a
                // RepositoryInvitation, so the null check *is* the 204/201 distinction.
                var invitation = await client.Repository.Collaborator.Add(owner, name, login, new CollaboratorRequest("push"));

                return invitation is null
                    ? new CollaboratorResult(InvitationCreated: false, InvitationId: null)
                    : new CollaboratorResult(InvitationCreated: true, InvitationId: invitation.Id);
            });
    }

    public Task<bool> IsCollaboratorAsync(string owner, string name, string login, string token, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateForToken(token);

        return ExecuteAsync(
            $"checking whether '{login}' can access '{owner}/{name}'",
            () => client.Repository.Collaborator.IsCollaborator(owner, name, login),
            notFound: () => false);
    }

    public Task<GitHubInvitation?> FindInvitationAsync(string owner, string name, string login, string token, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateForToken(token);

        // Octokit's invitation client is keyed by repository *id*, which would cost an extra repository read
        // per call. Connection keeps it to the one request the REST API actually needs.
        return ExecuteAsync<GitHubInvitation?>(
            $"listing invitations of '{owner}/{name}'",
            async () =>
            {
                var response = await client.Connection.Get<IReadOnlyList<RepositoryInvitation>>(
                    new Uri($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/invitations", UriKind.Relative),
                    parameters: null);

                var invitation = response.Body?.FirstOrDefault(
                    i => string.Equals(i.Invitee?.Login, login, StringComparison.OrdinalIgnoreCase));

                if (invitation is null)
                    return null;

                // GitHub reports expiry itself. Never compute it here: the window is GitHub's policy to change,
                // and a stale local constant would have the portal telling students something untrue.
                return new GitHubInvitation(invitation.Id, invitation.Invitee?.Login, invitation.Expired, invitation.CreatedAt);
            },
            notFound: () => null);
    }

    public Task DeleteInvitationAsync(string owner, string name, long invitationId, string token, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateForToken(token);

        return ExecuteAsync(
            $"withdrawing invitation {invitationId} on '{owner}/{name}'",
            async () =>
            {
                await client.Connection.Delete(
                    new Uri($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/invitations/{invitationId}", UriKind.Relative));
                return true;
            },
            // Already gone is the desired state, not a failure — the student may have accepted it meanwhile.
            notFound: () => true);
    }

    private static GitHubRepository ToRepository(Repository repository, string fallbackFullName)
    {
        var fullName = string.IsNullOrEmpty(repository.FullName) ? fallbackFullName : repository.FullName;

        return new GitHubRepository(
            fullName,
            string.IsNullOrEmpty(repository.HtmlUrl) ? $"https://github.com/{fullName}" : repository.HtmlUrl,
            repository.IsTemplate,
            repository.DefaultBranch);
    }

    /// <summary>
    /// Runs a GitHub call and translates Octokit's exceptions into <see cref="GitHubOperationException"/>, which
    /// controllers surface as a 502 carrying GitHub's own explanation. <paramref name="notFound"/> turns a 404
    /// into a value instead, for the calls where "absent" is an answer rather than a failure.
    /// </summary>
    private static async Task<T> ExecuteAsync<T>(string operation, Func<Task<T>> call, Func<T>? notFound = null)
    {
        try
        {
            return await call();
        }
        catch (NotFoundException) when (notFound is not null)
        {
            return notFound();
        }
        catch (ApiException ex)
        {
            throw new GitHubOperationException(operation, ex.StatusCode, ex.ApiError?.Message);
        }
        catch (OperationCanceledException ex)
        {
            // Octokit surfaces its own request timeout as a cancellation, which would otherwise look like the
            // caller giving up rather than GitHub being slow.
            throw new GitHubOperationException(operation, HttpStatusCode.GatewayTimeout, ex.Message);
        }
    }
}
