using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Grading.Dto;
using Ahk.Web.Services.Submissions;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.Grading;

public interface IGradeService
{
    Task<GradeRecord> SetGradeAsync(int courseId, SetGradeInput input, CancellationToken cancellationToken = default);

    Task<GradeRecord> ConfirmAutoGradeAsync(int courseId, ConfirmAutoGradeInput input, CancellationToken cancellationToken = default);

    Task<GradeRecord> RecordEvaluationResultAsync(int courseId, EvaluationResultInput input, DateTimeOffset timestamp, CancellationToken cancellationToken = default);
}

/// <summary>
/// Grade writes. Port of <c>SetGradeService</c> + <c>ResultProcessor</c>. Every operation **inserts** a new
/// <see cref="GradeRecord"/> — the history is append-only and the newest row is the current grade.
///
/// Reachable from three entry points with three different course-resolution mechanisms (teacher endpoint via
/// route, chatops via webhook payload, CI callback via token), which is why <paramref name="courseId"/> is
/// always explicit rather than read from the ambient course context.
/// </summary>
public sealed class GradeService : IGradeService
{
    /// <summary>Actor recorded for results arriving from the automated evaluation callback.</summary>
    public const string AutomatedActor = "grade-management-api";

    private readonly ApplicationDbContext db;
    private readonly ISubmissionResolver submissions;

    public GradeService(ApplicationDbContext db, ISubmissionResolver submissions)
    {
        this.db = db;
        this.submissions = submissions;
    }

    public async Task<GradeRecord> SetGradeAsync(int courseId, SetGradeInput input, CancellationToken cancellationToken = default)
    {
        var submission = await submissions.GetOrCreateAsync(courseId, input.Repository, input.Neptun, cancellationToken);
        var previous = await GetLastResultAsync(courseId, submission.Id, input.PrNumber, cancellationToken);

        var points = BuildPoints(input.Results, previous?.Points);
        return await AddResultAsync(courseId, submission, Normalize.Neptun(input.Neptun), input.PrNumber, input.PrUrl,
            DateTimeOffset.UtcNow, input.Actor, input.Origin, confirmed: true, points, cancellationToken);
    }

    public async Task<GradeRecord> ConfirmAutoGradeAsync(int courseId, ConfirmAutoGradeInput input, CancellationToken cancellationToken = default)
    {
        var submission = await submissions.GetOrCreateAsync(courseId, input.Repository, input.Neptun, cancellationToken);
        var previous = await GetLastResultAsync(courseId, submission.Id, input.PrNumber, cancellationToken);

        // Confirmation keeps whatever the automated evaluation produced.
        var points = previous?.Points
            .OrderBy(p => p.Order)
            .Select(p => (p.Name, p.Point))
            .ToList() ?? new List<(string, double)>();

        return await AddResultAsync(courseId, submission, Normalize.Neptun(input.Neptun), input.PrNumber, input.PrUrl,
            DateTimeOffset.UtcNow, input.Actor, input.Origin, confirmed: true, points, cancellationToken);
    }

    public async Task<GradeRecord> RecordEvaluationResultAsync(int courseId, EvaluationResultInput input, DateTimeOffset timestamp, CancellationToken cancellationToken = default)
    {
        var submission = await submissions.GetOrCreateAsync(courseId, input.GitHubRepoName, input.NeptunCode, cancellationToken);

        var prUrl = input.GitHubPullRequestNum.HasValue
            ? $"https://github.com/{Normalize.RepoName(input.GitHubRepoName)}/pull/{input.GitHubPullRequestNum}"
            : null;

        var origin = string.IsNullOrEmpty(input.Origin)
            ? $"https://github.com/{Normalize.RepoName(input.GitHubRepoName)}/commit/{input.GitHubCommitHash}"
            : input.Origin;

        return await AddResultAsync(courseId, submission, Normalize.Neptun(input.NeptunCode), input.GitHubPullRequestNum, prUrl,
            timestamp, AutomatedActor, origin, confirmed: false, AggregatePoints(input.Result), cancellationToken);
    }

    /// <summary>
    /// Sums task points per exercise name, ordered by name. Port of <c>ResultProcessor.GetTotalPoints</c> —
    /// per-task detail is intentionally not persisted.
    /// </summary>
    internal static List<(string Name, double Point)> AggregatePoints(IReadOnlyList<EvaluationTaskResult> tasks)
    {
        if (tasks is null)
            return new List<(string, double)>();

        return tasks
            .GroupBy(r => string.IsNullOrEmpty(r.ExerciseName) ? string.Empty : r.ExerciseName)
            .Select(g => (Name: g.Key, Point: g.Sum(r => r.Points)))
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Positional values keep the previous result's exercise names where available, otherwise "ex{i}".
    /// Port of <c>SetGradeService.getPoints</c>.
    /// </summary>
    internal static List<(string Name, double Point)> BuildPoints(IReadOnlyList<double> values, ICollection<GradeExercisePoint>? previousPoints)
    {
        var previous = previousPoints?.OrderBy(p => p.Order).ToList();
        var result = new List<(string, double)>(values.Count);

        for (var i = 0; i < values.Count; i++)
        {
            var name = previous is not null && previous.Count > i ? previous[i].Name : $"ex{i}";
            result.Add((name, values[i]));
        }

        return result;
    }

    private Task<GradeRecord?> GetLastResultAsync(int courseId, int submissionId, int? prNumber, CancellationToken cancellationToken)
        => db.GradeRecords
            .IgnoreQueryFilters()
            .Include(g => g.Points)
            .Where(g => g.CourseId == courseId && g.SubmissionId == submissionId && g.PrNumber == prNumber)
            .OrderByDescending(g => g.Date)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<GradeRecord> AddResultAsync(
        int courseId, Submission submission, string neptun, int? prNumber, string? prUrl,
        DateTimeOffset date, string? actor, string? origin, bool confirmed,
        List<(string Name, double Point)> points, CancellationToken cancellationToken)
    {
        var record = new GradeRecord
        {
            CourseId = courseId,
            SubmissionId = submission.Id,
            StudentId = submission.StudentId,
            Neptun = neptun,
            PrNumber = prNumber,
            PrUrl = prUrl,
            Date = date,
            Actor = actor,
            Origin = origin,
            Confirmed = confirmed,
        };

        for (var i = 0; i < points.Count; i++)
            record.Points.Add(new GradeExercisePoint { Name = points[i].Name, Point = points[i].Point, Order = i });

        db.GradeRecords.Add(record);

        submission.LastEventAt = date;
        await db.SaveChangesAsync(cancellationToken);

        return record;
    }
}
