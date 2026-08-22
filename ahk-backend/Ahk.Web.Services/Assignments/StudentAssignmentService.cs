using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ahk.Web.Services.Assignments;

/// <summary>Whether the student can actually open the repository yet.</summary>
public enum RepositoryAccess
{
    /// <summary>They have push access.</summary>
    Active,

    /// <summary>GitHub has invited them and is waiting; the repository is invisible to them until they accept.</summary>
    InvitationPending,

    /// <summary>The invitation ran out. It has to be re-sent before they can get in.</summary>
    InvitationExpired,

    /// <summary>GitHub could not be asked just now, so the last known state is reported.</summary>
    Unknown,
}

/// <summary>One repository a student holds, with the course and assignment it belongs to.</summary>
public sealed class StudentRepository
{
    public int AcceptanceId { get; set; }

    public string CourseSlug { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string AssignmentName { get; set; } = string.Empty;

    public string GitHubRepoName { get; set; } = string.Empty;

    public string RepoUrl { get; set; } = string.Empty;

    public DateTimeOffset AcceptedAt { get; set; }

    public RepositoryAccess Access { get; set; }

    /// <summary>Where the student accepts a pending invitation; null once they have access.</summary>
    public string? InvitationUrl { get; set; }

    public DateTimeOffset? InvitationSentAt { get; set; }
}

public interface IStudentAssignmentService
{
    Task<IReadOnlyList<StudentRepository>> ListForUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a fresh invitation for a repository the student still cannot open. Returns null when the
    /// acceptance is not theirs or does not exist.
    /// </summary>
    Task<StudentRepository?> ResendInvitationAsync(int userId, int acceptanceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// The student's own view of the system: every repository they hold, across every course, and a way to get back
/// in when a GitHub invitation lapsed before they clicked it.
///
/// ⚠️ These calls arrive on routes with no {course} segment, so no current course is resolved and the ambient
/// query filter would match nothing. Every read here uses <c>IgnoreQueryFilters()</c> and filters on the user.
/// </summary>
public sealed class StudentAssignmentService : IStudentAssignmentService
{
    private readonly ApplicationDbContext db;
    private readonly IGitHubRepositoryService gitHub;
    private readonly ICourseGitHubAppTokenProvider tokens;
    private readonly ILogger<StudentAssignmentService> logger;

    public StudentAssignmentService(
        ApplicationDbContext db,
        IGitHubRepositoryService gitHub,
        ICourseGitHubAppTokenProvider tokens,
        ILogger<StudentAssignmentService> logger)
    {
        this.db = db;
        this.gitHub = gitHub;
        this.tokens = tokens;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<StudentRepository>> ListForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var acceptances = await db.AssignmentAcceptances.IgnoreQueryFilters()
            .Include(a => a.Assignment)
            .Include(a => a.Course)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AcceptedAt)
            .ToListAsync(cancellationToken);

        var changed = false;
        var results = new List<StudentRepository>(acceptances.Count);

        foreach (var acceptance in acceptances)
        {
            // Only rows we believe are waiting cost a GitHub call — a student holds a handful of repositories,
            // and most of them are settled.
            if (acceptance.InvitationPending)
                changed |= await RefreshInvitationStateAsync(acceptance, cancellationToken);

            results.Add(Project(acceptance));
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken);

        return results;
    }

    public async Task<StudentRepository?> ResendInvitationAsync(int userId, int acceptanceId, CancellationToken cancellationToken = default)
    {
        var acceptance = await db.AssignmentAcceptances.IgnoreQueryFilters()
            .Include(a => a.Assignment)
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == acceptanceId && a.UserId == userId, cancellationToken);

        if (acceptance is null)
            return null;

        var token = await tokens.GetForCourseAsync(acceptance.CourseId, bypassCache: false, cancellationToken);
        if (token is null)
        {
            acceptance.InvitationPending = true;
            return Project(acceptance);
        }

        var (owner, name) = IAssignmentService.SplitRepoName(acceptance.GitHubRepoName);

        // They may have accepted since the page was drawn; re-sending would then be a pointless invitation.
        if (await gitHub.IsCollaboratorAsync(owner, name, acceptance.GitHubUsername, token.Token, cancellationToken))
        {
            ClearInvitation(acceptance);
            await MarkGitHubVerifiedAsync(acceptance, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Project(acceptance);
        }

        // GitHub has no "extend"; the stale invitation has to go before a fresh one can be issued.
        var existing = await gitHub.FindInvitationAsync(owner, name, acceptance.GitHubUsername, token.Token, cancellationToken);
        if (existing is not null)
            await gitHub.DeleteInvitationAsync(owner, name, existing.Id, token.Token, cancellationToken);

        var result = await gitHub.AddCollaboratorAsync(owner, name, acceptance.GitHubUsername, token.Token, cancellationToken);

        acceptance.InvitationPending = result.InvitationCreated;
        acceptance.InvitationId = result.InvitationId;
        acceptance.InvitationSentAt = result.InvitationCreated ? DateTimeOffset.UtcNow : null;

        await db.SaveChangesAsync(cancellationToken);
        return Project(acceptance);
    }

    /// <summary>
    /// Asks GitHub what really happened to a pending invitation. Returns true when the stored state changed.
    /// Never throws: this runs while rendering the student's home page, and an unreachable GitHub must degrade
    /// to "unknown", not to an error page.
    /// </summary>
    private async Task<bool> RefreshInvitationStateAsync(AssignmentAcceptance acceptance, CancellationToken cancellationToken)
    {
        try
        {
            var token = await tokens.GetForCourseAsync(acceptance.CourseId, bypassCache: false, cancellationToken);
            if (token is null)
                return false;

            var (owner, name) = IAssignmentService.SplitRepoName(acceptance.GitHubRepoName);

            if (await gitHub.IsCollaboratorAsync(owner, name, acceptance.GitHubUsername, token.Token, cancellationToken))
            {
                ClearInvitation(acceptance);
                await MarkGitHubVerifiedAsync(acceptance, cancellationToken);
                return true;
            }

            var invitation = await gitHub.FindInvitationAsync(owner, name, acceptance.GitHubUsername, token.Token, cancellationToken);

            // No access and no invitation at all: it lapsed and GitHub cleaned it up. Treat it as expired so
            // the student is offered the resend button rather than left waiting for nothing.
            if (invitation is null)
            {
                acceptance.InvitationId = null;
                return true;
            }

            if (invitation.Id != acceptance.InvitationId)
            {
                acceptance.InvitationId = invitation.Id;
                return true;
            }

            return false;
        }
        catch (Exception ex) when (ex is GitHubOperationException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Could not refresh the invitation state of {Repository}.", acceptance.GitHubRepoName);
            return false;
        }
    }

    /// <summary>
    /// Records that the GitHub login on this acceptance has been acted on by whoever holds it: an invitation
    /// only becomes a collaborator when someone signed in as that account accepts it.
    ///
    /// <para>Only stamped while the user still claims that same login — re-binding to a different account
    /// clears the stamp, and an old acceptance settling afterwards must not resurrect it. Deliberately does
    /// not save: the caller batches one SaveChanges for the whole page.</para>
    /// </summary>
    private async Task MarkGitHubVerifiedAsync(AssignmentAcceptance acceptance, CancellationToken cancellationToken)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == acceptance.UserId, cancellationToken);

        if (user?.GitHubUsername is null || user.GitHubVerifiedAt is not null)
            return;

        if (string.Equals(user.GitHubUsername, acceptance.GitHubUsername, StringComparison.OrdinalIgnoreCase))
            user.GitHubVerifiedAt = DateTimeOffset.UtcNow;
    }

    private static void ClearInvitation(AssignmentAcceptance acceptance)
    {
        acceptance.InvitationPending = false;
        acceptance.InvitationId = null;
        acceptance.InvitationSentAt = null;
    }

    private static StudentRepository Project(AssignmentAcceptance acceptance)
    {
        var (owner, name) = IAssignmentService.SplitRepoName(acceptance.GitHubRepoName);

        // A pending row with no invitation id left is one GitHub has already dropped: expired, not waiting.
        var access = acceptance.InvitationPending
            ? acceptance.InvitationId is null ? RepositoryAccess.InvitationExpired : RepositoryAccess.InvitationPending
            : RepositoryAccess.Active;

        return new StudentRepository
        {
            AcceptanceId = acceptance.Id,
            CourseSlug = acceptance.Course?.Slug ?? string.Empty,
            CourseName = acceptance.Course?.Name ?? string.Empty,
            AssignmentName = acceptance.Assignment?.Name ?? string.Empty,
            GitHubRepoName = acceptance.GitHubRepoName,
            RepoUrl = acceptance.RepoUrl,
            AcceptedAt = acceptance.AcceptedAt,
            Access = access,
            InvitationUrl = acceptance.InvitationPending ? $"https://github.com/{owner}/{name}/invitations" : null,
            InvitationSentAt = acceptance.InvitationSentAt,
        };
    }
}
