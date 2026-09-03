using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.Submissions;

public interface ISubmissionResolver
{
    Task<Submission> GetOrCreateAsync(int courseId, string gitHubRepoName, string? neptun = null, CancellationToken cancellationToken = default);

    Task<Student> GetOrCreateStudentAsync(int courseId, string neptun, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves the (student, submission) pair every write path needs. In the original system these were just
/// normalized strings on each record; here they are rows, created on first sighting.
///
/// Queries use <c>IgnoreQueryFilters</c> and filter on the explicit <paramref name="courseId"/> because callers
/// include machine-to-machine paths (webhooks, CI callbacks) that resolve their course from a payload or token
/// rather than the route, and must not depend on the ambient course context.
/// </summary>
public sealed class SubmissionResolver : ISubmissionResolver
{
    private readonly ApplicationDbContext db;
    private readonly ISubmissionArchiveService archive;

    public SubmissionResolver(ApplicationDbContext db, ISubmissionArchiveService archive)
    {
        this.db = db;
        this.archive = archive;
    }

    public async Task<Student> GetOrCreateStudentAsync(int courseId, string neptun, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize.Neptun(neptun);

        var student = await db.Students.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.CourseId == courseId && s.Neptun == normalized, cancellationToken);

        if (student is not null)
            return student;

        student = new Student { CourseId = courseId, Neptun = normalized };
        db.Students.Add(student);
        await db.SaveChangesAsync(cancellationToken);
        return student;
    }

    public async Task<Submission> GetOrCreateAsync(int courseId, string gitHubRepoName, string? neptun = null, CancellationToken cancellationToken = default)
    {
        var repo = Normalize.RepoName(gitHubRepoName);

        var submission = await db.Submissions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.CourseId == courseId && s.GitHubRepoName == repo, cancellationToken);

        if (submission is null)
        {
            // A repository belonging to an already-archived assignment is born archived, so archiving an
            // assignment holds for its future submissions too. Only on creation: this runs for every webhook
            // event, and recomputing an existing row's state would undo an admin's own decision.
            var archived = await archive.IsRepositoryArchivedAsync(courseId, repo, cancellationToken);

            submission = new Submission
            {
                CourseId = courseId,
                GitHubRepoName = repo,
                ArchivedAt = archived ? DateTimeOffset.UtcNow : null,
            };
            db.Submissions.Add(submission);
        }

        // The repository usually exists before neptun.txt is pushed, so link the student opportunistically.
        if (!string.IsNullOrWhiteSpace(neptun) && submission.StudentId is null)
        {
            var student = await GetOrCreateStudentAsync(courseId, neptun, cancellationToken);
            submission.StudentId = student.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
        return submission;
    }
}
