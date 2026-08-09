using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.StatusTracking.Dto;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.StatusTracking;

public interface IStatusTrackingService
{
    Task<IReadOnlyCollection<RepositoryStatus>> ListStatusesAsync(int courseId, CancellationToken cancellationToken = default);
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

    public async Task<IReadOnlyCollection<RepositoryStatus>> ListStatusesAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var submissions = await db.Submissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.CourseId == courseId)
            .Include(s => s.Events)
            .Include(s => s.Student)
            .ToListAsync(cancellationToken);

        return submissions.Select(CreateStatus).ToList();
    }

    private static RepositoryStatus CreateStatus(Submission submission)
    {
        var events = submission.Events;

        return new RepositoryStatus
        {
            Repository = submission.GitHubRepoName,
            Neptun = GetNeptun(submission, events),
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
}
