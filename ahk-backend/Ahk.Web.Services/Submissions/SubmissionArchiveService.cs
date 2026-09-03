using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.Submissions;

public interface ISubmissionArchiveService
{
    /// <summary>
    /// Archives or reactivates one submission. Returns false when the course has no such submission — the id
    /// comes from a client, so it is checked against the course rather than trusted.
    /// </summary>
    Task<bool> SetArchivedAsync(int courseId, int submissionId, bool archived, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carries an assignment's archived state to the repositories it produced. Called when an assignment is
    /// archived or reopened; the submissions follow either way.
    /// </summary>
    Task SetForAssignmentAsync(int courseId, int assignmentId, bool archived, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this repository belongs to an assignment that is currently archived — what a submission being
    /// created now has to inherit.
    /// </summary>
    Task<bool> IsRepositoryArchivedAsync(int courseId, string gitHubRepoName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Owns when a <see cref="Submission"/> is archived, for the three places that decide it: a course admin
/// acting on one row, an assignment being archived or reopened, and a submission being created while its
/// assignment is already archived.
///
/// <para>The link between an assignment and a repository is the acceptance's <c>GitHubRepoName</c> — the only
/// association there is (see <c>AssignmentService.CountSubmissionsAsync</c>), so a repository created outside
/// the portal belongs to no assignment and is never touched by the cascade.</para>
///
/// <para>Queries use <c>IgnoreQueryFilters</c> and an explicit <paramref name="courseId"/>: the creation path
/// runs from webhooks and the CI callback, which resolve their course from a payload rather than the route.</para>
/// </summary>
public sealed class SubmissionArchiveService : ISubmissionArchiveService
{
    private readonly ApplicationDbContext db;

    public SubmissionArchiveService(ApplicationDbContext db) => this.db = db;

    public async Task<bool> SetArchivedAsync(int courseId, int submissionId, bool archived, CancellationToken cancellationToken = default)
    {
        var submission = await db.Submissions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.CourseId == courseId, cancellationToken);

        if (submission is null)
            return false;

        if (Apply(submission, archived))
            await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task SetForAssignmentAsync(int courseId, int assignmentId, bool archived, CancellationToken cancellationToken = default)
    {
        var repositories = await db.AssignmentAcceptances.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.CourseId == courseId && a.AssignmentId == assignmentId)
            .Select(a => a.GitHubRepoName)
            .ToListAsync(cancellationToken);

        if (repositories.Count == 0)
            return;

        // Tracked updates rather than ExecuteUpdateAsync: an assignment's roster is a class, not a table scan,
        // and ExecuteUpdateAsync is relational-only — it would make this untestable on EF InMemory.
        var submissions = await db.Submissions.IgnoreQueryFilters()
            .Where(s => s.CourseId == courseId && repositories.Contains(s.GitHubRepoName))
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var submission in submissions)
            changed |= Apply(submission, archived);

        if (changed)
            await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsRepositoryArchivedAsync(int courseId, string gitHubRepoName, CancellationToken cancellationToken = default)
    {
        var repo = Normalize.RepoName(gitHubRepoName);

        return await db.AssignmentAcceptances.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(
                a => a.CourseId == courseId && a.GitHubRepoName == repo && a.Assignment!.ArchivedAt != null,
                cancellationToken);
    }

    /// <summary>Applies the state, reporting whether anything changed. Idempotent, like assignment archiving.</summary>
    private static bool Apply(Submission submission, bool archived)
    {
        if (archived == (submission.ArchivedAt is not null))
            return false;

        submission.ArchivedAt = archived ? DateTimeOffset.UtcNow : null;
        return true;
    }
}
