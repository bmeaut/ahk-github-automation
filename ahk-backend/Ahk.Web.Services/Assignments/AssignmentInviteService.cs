using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.Submissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ahk.Web.Services.Assignments;

/// <summary>Where a student stands on one assignment's invite link.</summary>
public enum InviteStatus
{
    /// <summary>No assignment answers to that invite token in this course — a stale or mistyped link.</summary>
    NotFound,

    /// <summary>Archived, and this student never accepted it. No new repositories are handed out.</summary>
    Closed,

    /// <summary>The account carries no Neptun code, so no repository can be named for it. Terminal.</summary>
    NeedsNeptun,

    /// <summary>The account has no verified GitHub login yet.</summary>
    NeedsGitHubUsername,

    /// <summary>Everything is in place; the student only has to confirm.</summary>
    ReadyToAccept,

    /// <summary>The repository exists and is theirs.</summary>
    Accepted,

    /// <summary>The course is not wired up to GitHub, so nothing can be created. Terminal, and the instructor's problem.</summary>
    NotConfigured,
}

/// <summary>Everything the invite screen renders, in one shape for both the read and the accept call.</summary>
public sealed class InviteState
{
    public InviteStatus Status { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public string CourseSlug { get; set; } = string.Empty;

    public string AssignmentName { get; set; } = string.Empty;

    public string? AssignmentDescription { get; set; }

    /// <summary>The GitHub organization the repository lives in, named in the confirmation text.</summary>
    public string? Organization { get; set; }

    /// <summary>The repository the student is about to get, or already has, without the owner prefix.</summary>
    public string? RepositoryName { get; set; }

    public string? RepoUrl { get; set; }

    public string? GitHubUsername { get; set; }

    /// <summary>
    /// Set when GitHub only *invited* the student rather than adding them. Until they accept it there, the
    /// repository 404s for them, so the UI must send them here instead of to the repository.
    /// </summary>
    public string? InvitationUrl { get; set; }

    /// <summary>Human-readable detail for the terminal states; null when there is nothing extra to say.</summary>
    public string? Message { get; set; }
}

public interface IAssignmentInviteService
{
    Task<InviteState> GetStateAsync(int courseId, string inviteToken, ApplicationUser user, CancellationToken cancellationToken = default);

    Task<InviteState> AcceptAsync(int courseId, string inviteToken, ApplicationUser user, CancellationToken cancellationToken = default);
}

/// <summary>
/// Drives a student from an invite link to a repository of their own: verify who they are, create the
/// repository from the assignment's template, give them push access, and record the pair. This is the part of
/// GitHub Classroom the portal takes over.
///
/// Everything it does is idempotent. A student who reloads, double-clicks, or opens the link in two tabs ends
/// up with exactly one repository — guarded by the unique index on (AssignmentId, UserId) and by checking
/// GitHub for the repository before creating it.
/// </summary>
public sealed class AssignmentInviteService : IAssignmentInviteService
{
    private readonly ApplicationDbContext db;
    private readonly IGitHubRepositoryService gitHub;
    private readonly ICourseGitHubAppTokenProvider tokens;
    private readonly ISubmissionResolver submissions;
    private readonly ILogger<AssignmentInviteService> logger;

    public AssignmentInviteService(
        ApplicationDbContext db,
        IGitHubRepositoryService gitHub,
        ICourseGitHubAppTokenProvider tokens,
        ISubmissionResolver submissions,
        ILogger<AssignmentInviteService> logger)
    {
        this.db = db;
        this.gitHub = gitHub;
        this.tokens = tokens;
        this.submissions = submissions;
        this.logger = logger;
    }

    public async Task<InviteState> GetStateAsync(int courseId, string inviteToken, ApplicationUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var context = await LoadAsync(courseId, inviteToken, cancellationToken);
        if (context is null)
            return new InviteState { Status = InviteStatus.NotFound, Message = "This invite link does not match an assignment. Ask your instructor for a current one." };

        var (course, assignment) = context.Value;
        var acceptance = await FindAcceptanceAsync(assignment.Id, user.Id, cancellationToken);

        var state = Describe(course, assignment, user, acceptance);

        // An accepted student keeps their link forever; only newcomers are turned away by archiving.
        if (acceptance is not null)
            return state;

        if (assignment.ArchivedAt is not null)
        {
            state.Status = InviteStatus.Closed;
            state.Message = "This assignment is no longer accepting new repositories.";
            return state;
        }

        return state;
    }

    public async Task<InviteState> AcceptAsync(int courseId, string inviteToken, ApplicationUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        // The client is not trusted: everything GetStateAsync decided is decided again here.
        var state = await GetStateAsync(courseId, inviteToken, user, cancellationToken);
        if (state.Status != InviteStatus.ReadyToAccept)
            return state;

        var context = await LoadAsync(courseId, inviteToken, cancellationToken);
        if (context is null)
            return new InviteState { Status = InviteStatus.NotFound };

        var (course, assignment) = context.Value;

        var token = await tokens.GetForCourseAsync(course, bypassCache: false, cancellationToken);
        if (token is null)
            return NotConfigured(state);

        // Enrolls the student in the course on first contact — the model has no roster import, here or anywhere.
        var student = await submissions.GetOrCreateStudentAsync(courseId, user.NeptunCode!, cancellationToken);
        if (!string.Equals(student.GitHubUsername, user.GitHubUsername, StringComparison.OrdinalIgnoreCase))
        {
            student.GitHubUsername = user.GitHubUsername;
            await db.SaveChangesAsync(cancellationToken);
        }

        var organization = course.GitHubOrganization!;
        var repositoryName = BuildRepositoryName(assignment.TemplateRepoName, student.Neptun);
        var fullName = Normalize.RepoName($"{organization}/{repositoryName}");

        var existing = await gitHub.GetRepositoryAsync(organization, repositoryName, token.Token, cancellationToken);
        var repository = existing;

        if (repository is null)
        {
            var (templateOwner, templateName) = IAssignmentService.SplitRepoName(assignment.TemplateRepoName);

            repository = await gitHub.GenerateFromTemplateAsync(
                templateOwner, templateName, organization, repositoryName, token.Token, cancellationToken);

            // Belt and braces. A generated repository normally has Actions on, but the evaluator is the whole
            // point of the repository, so it is worth one call not to find out otherwise weeks later.
            try
            {
                await gitHub.EnsureActionsEnabledAsync(organization, repositoryName, token.Token, cancellationToken);
            }
            catch (GitHubOperationException ex)
            {
                logger.LogWarning(ex, "Could not enable Actions on {Repository}; the repository was still created.", fullName);
            }
        }

        var collaborator = await gitHub.AddCollaboratorAsync(organization, repositoryName, user.GitHubUsername!, token.Token, cancellationToken);

        // Created eagerly so grades and status events land on the right row even before the first webhook.
        // No SubmissionEvent is written here: the webhook receiver owns the event log, and a second
        // "repository created" would double-count in the status projection.
        await submissions.GetOrCreateAsync(courseId, fullName, student.Neptun, cancellationToken);

        var acceptance = new AssignmentAcceptance
        {
            CourseId = courseId,
            AssignmentId = assignment.Id,
            UserId = user.Id,
            GitHubRepoName = fullName,
            RepoUrl = repository.HtmlUrl,
            GitHubUsername = user.GitHubUsername!,
            InvitationPending = collaborator.InvitationCreated,
            InvitationId = collaborator.InvitationId,
            InvitationSentAt = collaborator.InvitationCreated ? DateTimeOffset.UtcNow : null,
        };

        db.AssignmentAcceptances.Add(acceptance);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost the race against another tab. The unique index did its job; report the row that won.
            db.Entry(acceptance).State = EntityState.Detached;

            var winner = await FindAcceptanceAsync(assignment.Id, user.Id, cancellationToken);
            if (winner is null)
                throw;

            acceptance = winner;
        }

        var result = Describe(course, assignment, user, acceptance);
        if (existing is not null)
            result.Message = "This repository already existed, so it was linked to you rather than created again.";

        return result;
    }

    /// <summary>
    /// The student's repository name: the template repository's own name with their Neptun code appended, the
    /// convention the courses already use. Lowercased like every repository name in the model.
    /// </summary>
    internal static string BuildRepositoryName(string templateRepoName, string neptun)
    {
        var (_, name) = IAssignmentService.SplitRepoName(templateRepoName);
        return Normalize.RepoName($"{name}-{neptun}");
    }

    private InviteState Describe(Course course, Assignment assignment, ApplicationUser user, AssignmentAcceptance? acceptance)
    {
        var state = new InviteState
        {
            CourseName = course.Name,
            CourseSlug = course.Slug,
            AssignmentName = assignment.Name,
            AssignmentDescription = assignment.Description,
            Organization = course.GitHubOrganization,
            GitHubUsername = user.GitHubUsername,
        };

        if (acceptance is not null)
        {
            var (owner, name) = IAssignmentService.SplitRepoName(acceptance.GitHubRepoName);

            state.Status = InviteStatus.Accepted;
            state.RepositoryName = name;
            state.RepoUrl = acceptance.RepoUrl;
            state.GitHubUsername = acceptance.GitHubUsername;
            state.InvitationUrl = acceptance.InvitationPending ? $"https://github.com/{owner}/{name}/invitations" : null;
            return state;
        }

        if (string.IsNullOrWhiteSpace(course.GitHubOrganization))
        {
            state.Status = InviteStatus.NotConfigured;
            state.Message = "This course is not connected to a GitHub organization yet. Ask your instructor to finish setting it up.";
            return state;
        }

        if (string.IsNullOrWhiteSpace(user.NeptunCode))
        {
            state.Status = InviteStatus.NeedsNeptun;
            state.Message = "This account has no Neptun code, so no repository can be created for it. Sign in with your BME eduID account instead.";
            return state;
        }

        state.RepositoryName = BuildRepositoryName(assignment.TemplateRepoName, Normalize.Neptun(user.NeptunCode));

        if (string.IsNullOrWhiteSpace(user.GitHubUsername))
        {
            state.Status = InviteStatus.NeedsGitHubUsername;
            return state;
        }

        state.Status = InviteStatus.ReadyToAccept;
        return state;
    }

    private static InviteState NotConfigured(InviteState state)
    {
        state.Status = InviteStatus.NotConfigured;
        state.Message = "This course has no working GitHub App configured, so repositories cannot be created. Ask your instructor to check it.";
        return state;
    }

    private async Task<(Course Course, Assignment Assignment)?> LoadAsync(int courseId, string inviteToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(inviteToken))
            return null;

        var assignment = await db.Assignments.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(a => a.CourseId == courseId && a.InviteToken == inviteToken, cancellationToken);

        if (assignment is null)
            return null;

        var course = await db.Courses.AsNoTracking()
            .Include(c => c.GitHubConfig)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        return course is null ? null : (course, assignment);
    }

    private async Task<AssignmentAcceptance?> FindAcceptanceAsync(int assignmentId, int userId, CancellationToken cancellationToken) =>
        await db.AssignmentAcceptances.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId && a.UserId == userId, cancellationToken);
}
