using System.Linq;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.StatusTracking;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Parity checks for the event-log → status projection ported from
/// <c>grade-management/.../StatusTracking/StatusTrackingService.cs</c>: latest PR action wins, assignees are
/// the distinct union, branches are distinct, and workflow runs report count + last conclusion.
/// </summary>
public class StatusProjectionTests
{
    private sealed class FixedCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId { get; set; }
    }

    private const int CourseId = 1;

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options, new FixedCourseProvider { CurrentCourseId = CourseId });
    }

    private static async Task<ApplicationDbContext> SeedAsync()
    {
        var db = CreateContext(Guid.NewGuid().ToString());
        var now = DateTimeOffset.UtcNow;

        var student = new Student { CourseId = CourseId, Neptun = "ABC123" };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var submission = new Submission { CourseId = CourseId, StudentId = student.Id, GitHubRepoName = "org/course-hw1-abc123" };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        db.SubmissionEvents.AddRange(
            new RepositoryCreatedEvent { CourseId = CourseId, SubmissionId = submission.Id, Timestamp = now.AddDays(-5) },
            new BranchCreatedEvent { CourseId = CourseId, SubmissionId = submission.Id, Timestamp = now.AddDays(-4), Branch = "solution" },
            new BranchCreatedEvent { CourseId = CourseId, SubmissionId = submission.Id, Timestamp = now.AddDays(-4), Branch = "solution" },
            new PullRequestEvent
            {
                CourseId = CourseId, SubmissionId = submission.Id, Timestamp = now.AddDays(-3),
                Number = 1, Action = "opened", HtmlUrl = "https://github.com/org/course-hw1-abc123/pull/1",
                Neptun = "ABC123", Assignees = new List<string> { "teacher1" },
            },
            new PullRequestEvent
            {
                CourseId = CourseId, SubmissionId = submission.Id, Timestamp = now.AddDays(-1),
                Number = 1, Action = "closed", HtmlUrl = "https://github.com/org/course-hw1-abc123/pull/1",
                Neptun = "ABC123", Assignees = new List<string> { "teacher2" },
            },
            new WorkflowRunEvent { CourseId = CourseId, SubmissionId = submission.Id, Timestamp = now.AddDays(-3), Conclusion = "failure" },
            new WorkflowRunEvent { CourseId = CourseId, SubmissionId = submission.Id, Timestamp = now.AddDays(-2), Conclusion = "success" });

        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Projection_MatchesLegacySemantics()
    {
        await using var db = await SeedAsync();
        var service = new StatusTrackingService(db);

        var statuses = await service.ListStatusesAsync(CourseId);

        var status = Assert.Single(statuses);
        Assert.Equal("org/course-hw1-abc123", status.Repository);
        Assert.Equal("ABC123", status.Neptun);

        // Branches are distinct.
        Assert.Equal(new[] { "solution" }, status.Branches);

        // One PR, latest action wins, assignees are the distinct union across its events.
        var pr = Assert.Single(status.PullRequests);
        Assert.Equal(1, pr.Number);
        Assert.Equal("closed", pr.Status);
        Assert.Equal("teacher1, teacher2", pr.Assignee);

        // Workflow runs: total count and the most recent conclusion.
        Assert.Equal(2, status.WorkflowRuns.Count);
        Assert.Equal("success", status.WorkflowRuns.LastStatus);
    }

    /// <summary>
    /// The assignment a repository belongs to comes from an acceptance naming that same repository — the only
    /// association there is. A repository nothing accepted (GitHub Classroom, or anything created outside the
    /// portal) has none, which is what the submissions screen lists as "No assignment".
    /// </summary>
    [Fact]
    public async Task Projection_CarriesTheAcceptedAssignment()
    {
        await using var db = await SeedAsync();
        var assignmentId = await AddAssignmentAsync(db, CourseId, "Homework 1", acceptedRepo: "org/course-hw1-abc123");

        // A second repository in the same course that nobody accepted.
        db.Submissions.Add(new Submission { CourseId = CourseId, GitHubRepoName = "org/course-legacy-xyz789" });
        await db.SaveChangesAsync();

        var statuses = await new StatusTrackingService(db).ListStatusesAsync(CourseId);

        var accepted = statuses.Single(s => s.Repository == "org/course-hw1-abc123");
        Assert.Equal(assignmentId, accepted.AssignmentId);
        Assert.Equal("Homework 1", accepted.AssignmentName);

        var unassigned = statuses.Single(s => s.Repository == "org/course-legacy-xyz789");
        Assert.Null(unassigned.AssignmentId);
        Assert.Null(unassigned.AssignmentName);
    }

    /// <summary>An acceptance in another course must not label this course's repository.</summary>
    [Fact]
    public async Task Projection_IgnoresAnAcceptanceFromAnotherCourse()
    {
        await using var db = await SeedAsync();
        await AddAssignmentAsync(db, courseId: 999, name: "Someone else's homework", acceptedRepo: "org/course-hw1-abc123");

        var status = Assert.Single(await new StatusTrackingService(db).ListStatusesAsync(CourseId));

        Assert.Null(status.AssignmentId);
        Assert.Null(status.AssignmentName);
    }

    /// <summary>Adds an assignment plus one acceptance for <paramref name="acceptedRepo"/>, returning its id.</summary>
    private static async Task<int> AddAssignmentAsync(ApplicationDbContext db, int courseId, string name, string acceptedRepo)
    {
        var user = new ApplicationUser { UserName = $"student-{courseId}-{name}", NeptunCode = "ABC123" };
        db.Users.Add(user);

        var assignment = new Assignment
        {
            CourseId = courseId,
            Name = name,
            TemplateRepoName = "org/course-hw1",
            InviteToken = $"token-{courseId}-{name}",
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        db.AssignmentAcceptances.Add(new AssignmentAcceptance
        {
            CourseId = courseId,
            AssignmentId = assignment.Id,
            UserId = user.Id,
            GitHubRepoName = acceptedRepo,
            RepoUrl = $"https://github.com/{acceptedRepo}",
            GitHubUsername = "sample-student",
        });
        await db.SaveChangesAsync();

        return assignment.Id;
    }

    [Fact]
    public async Task Projection_ReturnsNothingForAnotherCourse()
    {
        await using var db = await SeedAsync();
        var service = new StatusTrackingService(db);

        Assert.Empty(await service.ListStatusesAsync(courseId: 999));
    }
}
