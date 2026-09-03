using System.Linq;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.Grading;
using Ahk.Web.Services.StatusTracking;
using Ahk.Web.Services.Submissions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Archiving a submission: the three ways it happens, and what disappears once it has.
///
/// <para>The load-bearing case is the last one — a submission created <em>after</em> its assignment was
/// archived is born archived. Without it, "archive this assignment" would quietly stop applying the moment a
/// student pushed to a repository whose row did not exist yet.</para>
/// </summary>
public class SubmissionArchiveTests
{
    private const int CourseA = 1;
    private const int CourseB = 2;

    private sealed class FixedCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId { get; set; }
    }

    // ---- The cascade from an assignment ----

    [Fact]
    public async Task ArchivingAnAssignment_ArchivesTheRepositoriesItProduced()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1");
        await AcceptAsync(db, CourseA, assignment, "org/a-hw1-abc123");
        AddSubmission(db, CourseA, "org/a-hw1-abc123");

        // Same course, no acceptance: a repository created outside the portal belongs to no assignment.
        AddSubmission(db, CourseA, "org/a-legacy-xyz789");
        await db.SaveChangesAsync();

        await CreateAssignments(db).SetArchivedAsync(CourseA, assignment.Id, archived: true);

        Assert.NotNull(await ArchivedAtAsync(db, "org/a-hw1-abc123"));
        Assert.Null(await ArchivedAtAsync(db, "org/a-legacy-xyz789"));
    }

    [Fact]
    public async Task ReopeningAnAssignment_ReactivatesThem()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1");
        await AcceptAsync(db, CourseA, assignment, "org/a-hw1-abc123");
        AddSubmission(db, CourseA, "org/a-hw1-abc123");
        await db.SaveChangesAsync();

        var service = CreateAssignments(db);
        await service.SetArchivedAsync(CourseA, assignment.Id, archived: true);
        await service.SetArchivedAsync(CourseA, assignment.Id, archived: false);

        Assert.Null(await ArchivedAtAsync(db, "org/a-hw1-abc123"));
        Assert.Null((await db.Assignments.IgnoreQueryFilters().SingleAsync(a => a.Id == assignment.Id)).ArchivedAt);
    }

    [Fact]
    public async Task TheCascade_StaysInsideTheCourse()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1");
        await AcceptAsync(db, CourseA, assignment, "org/shared-hw1-abc123");
        AddSubmission(db, CourseA, "org/shared-hw1-abc123");
        AddSubmission(db, CourseB, "org/shared-hw1-abc123");
        await db.SaveChangesAsync();

        await CreateAssignments(db).SetArchivedAsync(CourseA, assignment.Id, archived: true);

        Assert.NotNull(await ArchivedAtAsync(db, "org/shared-hw1-abc123", CourseA));
        Assert.Null(await ArchivedAtAsync(db, "org/shared-hw1-abc123", CourseB));
    }

    /// <summary>
    /// The "even future ones" half of the requirement: the repository is already accepted, the assignment is
    /// archived, and the submission row appears later — from a webhook, or the CI callback.
    /// </summary>
    [Fact]
    public async Task ASubmissionCreatedForAnArchivedAssignment_IsBornArchived()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1", archived: true);
        await AcceptAsync(db, CourseA, assignment, "org/a-hw1-abc123");
        await db.SaveChangesAsync();

        var resolver = new SubmissionResolver(db, new SubmissionArchiveService(db));
        var created = await resolver.GetOrCreateAsync(CourseA, "org/a-hw1-abc123");

        Assert.NotNull(created.ArchivedAt);
    }

    [Fact]
    public async Task ASubmissionCreatedForALiveAssignment_IsActive()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1");
        await AcceptAsync(db, CourseA, assignment, "org/a-hw1-abc123");
        await db.SaveChangesAsync();

        var resolver = new SubmissionResolver(db, new SubmissionArchiveService(db));

        Assert.Null((await resolver.GetOrCreateAsync(CourseA, "org/a-hw1-abc123")).ArchivedAt);
        Assert.Null((await resolver.GetOrCreateAsync(CourseA, "org/never-accepted-xyz789")).ArchivedAt);
    }

    /// <summary>
    /// Resolving an existing submission must not recompute its state: an admin who reactivated one row would
    /// otherwise see the next webhook event undo the decision.
    /// </summary>
    [Fact]
    public async Task ResolvingAnExistingSubmission_LeavesItsStateAlone()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1", archived: true);
        await AcceptAsync(db, CourseA, assignment, "org/a-hw1-abc123");
        AddSubmission(db, CourseA, "org/a-hw1-abc123");
        await db.SaveChangesAsync();

        var resolver = new SubmissionResolver(db, new SubmissionArchiveService(db));

        Assert.Null((await resolver.GetOrCreateAsync(CourseA, "org/a-hw1-abc123")).ArchivedAt);
    }

    // ---- One row at a time ----

    [Fact]
    public async Task ACourseAdmin_ArchivesAndReactivatesOneSubmission()
    {
        await using var db = CreateContext();
        var submission = AddSubmission(db, CourseA, "org/a-legacy-abc123");
        await db.SaveChangesAsync();

        var service = new SubmissionArchiveService(db);

        Assert.True(await service.SetArchivedAsync(CourseA, submission.Id, archived: true));
        Assert.NotNull(await ArchivedAtAsync(db, "org/a-legacy-abc123"));

        // Idempotent, like assignment archiving.
        Assert.True(await service.SetArchivedAsync(CourseA, submission.Id, archived: true));

        Assert.True(await service.SetArchivedAsync(CourseA, submission.Id, archived: false));
        Assert.Null(await ArchivedAtAsync(db, "org/a-legacy-abc123"));
    }

    /// <summary>The id comes from a client, so it is checked against the course rather than trusted.</summary>
    [Fact]
    public async Task ASubmissionOfAnotherCourse_IsNotFound()
    {
        await using var db = CreateContext();
        var submission = AddSubmission(db, CourseB, "org/b-hw1-abc123");
        await db.SaveChangesAsync();

        Assert.False(await new SubmissionArchiveService(db).SetArchivedAsync(CourseA, submission.Id, archived: true));
        Assert.Null(await ArchivedAtAsync(db, "org/b-hw1-abc123", CourseB));
    }

    // ---- What an archived submission drops out of ----

    [Fact]
    public async Task ArchivedSubmissions_LeaveTheStatusListUnlessAskedFor()
    {
        await using var db = CreateContext();
        var archived = AddSubmission(db, CourseA, "org/a-hw1-abc123");
        AddSubmission(db, CourseA, "org/a-hw1-xyz789");
        await db.SaveChangesAsync();
        await new SubmissionArchiveService(db).SetArchivedAsync(CourseA, archived.Id, archived: true);

        var service = new StatusTrackingService(db);

        var active = await service.ListStatusesAsync(CourseA);
        var row = Assert.Single(active);
        Assert.Equal("org/a-hw1-xyz789", row.Repository);
        Assert.Null(row.ArchivedAt);
        Assert.NotEqual(0, row.SubmissionId);

        var all = await service.ListStatusesAsync(CourseA, includeArchived: true);
        Assert.Equal(2, all.Count);
        Assert.NotNull(all.Single(s => s.Repository == "org/a-hw1-abc123").ArchivedAt);
    }

    /// <summary>
    /// The grades list backs the CSV export as well, so this covers both — archiving takes the work out of the
    /// course's live picture without touching the grade rows.
    /// </summary>
    [Fact]
    public async Task ArchivedSubmissions_LeaveTheGradesListAndTheCsv()
    {
        await using var db = CreateContext();
        var archived = AddSubmission(db, CourseA, "org/a-hw1-abc123");
        var active = AddSubmission(db, CourseA, "org/a-hw1-xyz789");
        await db.SaveChangesAsync();

        AddConfirmedGrade(db, archived, "ABC123");
        AddConfirmedGrade(db, active, "XYZ789");
        await db.SaveChangesAsync();

        await new SubmissionArchiveService(db).SetArchivedAsync(CourseA, archived.Id, archived: true);

        var listing = new GradeListingService(db);

        var grade = Assert.Single(await listing.ListAsync(CourseA));
        Assert.Equal("XYZ789", grade.Neptun);

        var csv = await listing.ExportCsvAsync(CourseA);
        Assert.DoesNotContain("ABC123", csv, StringComparison.Ordinal);
        Assert.Contains("XYZ789", csv, StringComparison.Ordinal);

        // Asked for explicitly, an archived submission still has its points — that is what lets the screen
        // show them beside a row it is deliberately displaying.
        Assert.Equal(2, (await listing.ListAsync(CourseA, includeArchived: true)).Count);

        // Nothing was deleted: both grade records are still there.
        Assert.Equal(2, await db.GradeRecords.IgnoreQueryFilters().CountAsync(g => g.CourseId == CourseA));
    }

    /// <summary>
    /// The count on the assignments listing links to the submissions screen, so it counts what that screen
    /// will show — otherwise the link would promise rows it then filters away.
    /// </summary>
    [Fact]
    public async Task TheAssignmentSubmissionCount_ExcludesArchived()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1");
        await AcceptAsync(db, CourseA, assignment, "org/a-hw1-abc123");
        await AcceptAsync(db, CourseA, assignment, "org/a-hw1-xyz789");
        AddSubmission(db, CourseA, "org/a-hw1-abc123");
        var second = AddSubmission(db, CourseA, "org/a-hw1-xyz789");
        await db.SaveChangesAsync();

        var assignments = CreateAssignments(db);
        Assert.Equal(2, (await assignments.CountSubmissionsAsync(CourseA))[assignment.Id]);

        await new SubmissionArchiveService(db).SetArchivedAsync(CourseA, second.Id, archived: true);

        Assert.Equal(1, (await assignments.CountSubmissionsAsync(CourseA))[assignment.Id]);
    }

    // ---- Helpers ----

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // No current course: these services filter on an explicit course id and ignore the query filter.
        return new ApplicationDbContext(options, new FixedCourseProvider { CurrentCourseId = null });
    }

    /// <summary>The GitHub collaborators are strict with no setups: archiving must reach no network.</summary>
    private static AssignmentService CreateAssignments(ApplicationDbContext db) => new(
        db,
        new Mock<IGitHubRepositoryService>(MockBehavior.Strict).Object,
        new Mock<ICourseGitHubAppTokenProvider>(MockBehavior.Strict).Object,
        new SubmissionArchiveService(db));

    private static async Task<Assignment> AddAssignmentAsync(ApplicationDbContext db, int courseId, string name, bool archived = false)
    {
        var assignment = new Assignment
        {
            CourseId = courseId,
            Name = name,
            TemplateRepoName = $"org/course{courseId}-hw1",
            InviteToken = $"token-{courseId}-{name}",
            ArchivedAt = archived ? DateTimeOffset.UtcNow.AddDays(-1) : null,
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();
        return assignment;
    }

    private static async Task AcceptAsync(ApplicationDbContext db, int courseId, Assignment assignment, string repo)
    {
        var user = new ApplicationUser { UserName = $"student-{repo}", NeptunCode = "ABC123" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.AssignmentAcceptances.Add(new AssignmentAcceptance
        {
            CourseId = courseId,
            AssignmentId = assignment.Id,
            UserId = user.Id,
            GitHubRepoName = repo,
            RepoUrl = $"https://github.com/{repo}",
            GitHubUsername = "sample-student",
        });
    }

    private static Submission AddSubmission(ApplicationDbContext db, int courseId, string repo)
    {
        var submission = new Submission { CourseId = courseId, GitHubRepoName = repo };
        db.Submissions.Add(submission);
        return submission;
    }

    private static void AddConfirmedGrade(ApplicationDbContext db, Submission submission, string neptun)
    {
        var grade = new GradeRecord
        {
            CourseId = submission.CourseId,
            SubmissionId = submission.Id,
            Neptun = neptun,
            Date = DateTimeOffset.UtcNow,
            Confirmed = true,
        };

        grade.Points.Add(new GradeExercisePoint { Name = "ex0", Point = 2, Order = 0 });
        db.GradeRecords.Add(grade);
    }

    private static async Task<DateTimeOffset?> ArchivedAtAsync(ApplicationDbContext db, string repo, int courseId = CourseA) =>
        await db.Submissions.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.CourseId == courseId && s.GitHubRepoName == repo)
            .Select(s => s.ArchivedAt)
            .SingleAsync();
}
