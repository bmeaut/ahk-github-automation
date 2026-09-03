using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.StatusTracking.Dto;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.StatusTracking;

public interface IStatusTrackingService
{
    /// <summary>
    /// The course's submission statuses. Archived submissions are left out unless
    /// <paramref name="includeArchived"/> asks for them.
    /// </summary>
    Task<IReadOnlyCollection<RepositoryStatus>> ListStatusesAsync(int courseId, bool includeArchived = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Projects the append-only <see cref="SubmissionEvent"/> log into the per-submission status view.
/// Port of <c>grade-management/.../StatusTracking/StatusTrackingService.cs</c>: the grouping and
/// "latest event wins" rules are preserved exactly, with the repo-prefix filter replaced by the course id.
/// </summary>
public sealed class StatusTrackingService : IStatusTrackingService
{
    private readonly ApplicationDbContext db;

    public StatusTrackingService(ApplicationDbContext db) => this.db = db;

    public async Task<IReadOnlyCollection<RepositoryStatus>> ListStatusesAsync(int courseId, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var submissions = await db.Submissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.CourseId == courseId)
            .Where(s => includeArchived || s.ArchivedAt == null)
            .Include(s => s.Events)
            .Include(s => s.Student)
            .ToListAsync(cancellationToken);

        var assignments = await LoadAssignmentsByRepoAsync(courseId, cancellationToken);

        return submissions.Select(s => CreateStatus(s, assignments)).ToList();
    }

    /// <summary>
    /// Which assignment each repository belongs to, if any. One query for the whole course rather than a join
    /// on the submission query, because the projection above already materializes every row.
    ///
    /// <para>The acceptance is the only association there is: both sides store the full "owner/name" through
    /// <c>Normalize.RepoName</c>, and a repository created outside the portal has no acceptance and so
    /// no assignment. The grouping is defensive — nothing stops two assignments from having produced the same
    /// repository name.</para>
    /// </summary>
    private async Task<IReadOnlyDictionary<string, AssignmentRef>> LoadAssignmentsByRepoAsync(int courseId, CancellationToken cancellationToken)
    {
        var acceptances = await db.AssignmentAcceptances
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .Select(a => new { a.GitHubRepoName, a.AssignmentId, AssignmentName = a.Assignment!.Name })
            .ToListAsync(cancellationToken);

        return acceptances
            .GroupBy(a => a.GitHubRepoName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => new AssignmentRef(g.First().AssignmentId, g.First().AssignmentName),
                StringComparer.Ordinal);
    }

    private static RepositoryStatus CreateStatus(Submission submission, IReadOnlyDictionary<string, AssignmentRef> assignments)
    {
        var events = submission.Events;
        assignments.TryGetValue(submission.GitHubRepoName, out var assignment);

        return new RepositoryStatus
        {
            SubmissionId = submission.Id,
            Repository = submission.GitHubRepoName,
            ArchivedAt = submission.ArchivedAt,
            Neptun = GetNeptun(submission, events),
            AssignmentId = assignment?.Id,
            AssignmentName = assignment?.Name,
            Branches = events.OfType<BranchCreatedEvent>().Select(e => e.Branch).Distinct().ToArray(),
            PullRequests = events.OfType<PullRequestEvent>()
                .GroupBy(e => e.Number)
                .Select(GetPrStatus)
                .ToArray(),
            WorkflowRuns = GetWorkflowRunsStatus(events),
        };
    }

    /// <summary>Latest non-empty neptun from the PR events, falling back to the linked student.</summary>
    private static string GetNeptun(Submission submission, IEnumerable<SubmissionEvent> events)
        => events.OfType<PullRequestEvent>()
            .Where(e => !string.IsNullOrEmpty(e.Neptun))
            .OrderByDescending(e => e.Timestamp)
            .Select(e => e.Neptun!)
            .FirstOrDefault()
           ?? submission.Student?.Neptun
           ?? string.Empty;

    private static PullRequestStatus GetPrStatus(IGrouping<int, PullRequestEvent> events)
    {
        var latest = events.OrderByDescending(e => e.Timestamp).First();
        return new PullRequestStatus
        {
            Number = events.Key,
            HtmlUrl = latest.HtmlUrl,
            Status = latest.Action,
            Assignee = string.Join(", ", events.SelectMany(e => e.Assignees).Distinct()),
        };
    }

    private static WorkflowRunsStatus GetWorkflowRunsStatus(IEnumerable<SubmissionEvent> events)
    {
        var items = events.OfType<WorkflowRunEvent>().ToList();
        return new WorkflowRunsStatus
        {
            Count = items.Count,
            LastStatus = items.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Conclusion,
        };
    }

    /// <summary>The assignment a repository belongs to, as the projection needs it.</summary>
    private sealed record AssignmentRef(int Id, string Name);
}
