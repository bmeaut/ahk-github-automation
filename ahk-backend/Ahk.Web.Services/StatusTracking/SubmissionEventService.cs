using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Submissions;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.StatusTracking;

public interface ISubmissionEventService
{
    /// <summary>
    /// Appends a status event. Returns false when the event was a webhook redelivery and was skipped.
    /// </summary>
    Task<bool> RecordAsync(int courseId, string gitHubRepoName, SubmissionEvent submissionEvent, string? neptun = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Appends to the status event log (was <c>StatusTrackingService.InsertNewEvent</c> plus the queue plumbing).
/// Resolves the submission first, so events and grades share the same anchor row.
/// </summary>
public sealed class SubmissionEventService : ISubmissionEventService
{
    private readonly ApplicationDbContext db;
    private readonly ISubmissionResolver submissions;

    public SubmissionEventService(ApplicationDbContext db, ISubmissionResolver submissions)
    {
        this.db = db;
        this.submissions = submissions;
    }

    public async Task<bool> RecordAsync(int courseId, string gitHubRepoName, SubmissionEvent submissionEvent, string? neptun = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submissionEvent);

        // GitHub redelivers webhooks; the delivery id makes appending idempotent.
        if (!string.IsNullOrEmpty(submissionEvent.GitHubDeliveryId))
        {
            var alreadySeen = await db.SubmissionEvents.IgnoreQueryFilters()
                .AnyAsync(e => e.GitHubDeliveryId == submissionEvent.GitHubDeliveryId, cancellationToken);
            if (alreadySeen)
                return false;
        }

        var submission = await submissions.GetOrCreateAsync(courseId, gitHubRepoName, neptun, cancellationToken);

        submissionEvent.CourseId = courseId;
        submissionEvent.SubmissionId = submission.Id;
        if (submissionEvent.Timestamp == default)
            submissionEvent.Timestamp = DateTimeOffset.UtcNow;

        db.SubmissionEvents.Add(submissionEvent);
        submission.LastEventAt = submissionEvent.Timestamp;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
