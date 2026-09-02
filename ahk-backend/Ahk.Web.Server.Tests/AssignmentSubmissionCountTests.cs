using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.GitHub;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// How many repositories belong to each assignment — the count the assignments listing turns into a link to
/// the filtered submissions screen.
///
/// <para>The association is an <see cref="AssignmentAcceptance"/> naming the same repository as the
/// <see cref="Submission"/>, and nothing else: no naming convention, no prefix guess. These pin down what that
/// means at the edges — an acceptance whose repository was never created, a repository nobody accepted, and
/// another course's rows.</para>
///
/// <para>The GitHub collaborators are strict mocks with no setups, so counting proves it reaches no network.</para>
/// </summary>
public class AssignmentSubmissionCountTests
{
    private const int CourseA = 1;
    private const int CourseB = 2;

    private sealed class FixedCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId { get; set; }
    }

    [Fact]
    public async Task Counts_OnlyAcceptancesWhoseRepositoryExists()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1");

        // Two students accepted; only one of the repositories has a submission row.
        await AcceptAsync(db, CourseA, assignment, "org/a-hw1-abc123");
        await AcceptAsync(db, CourseA, assignment, "org/a-hw1-xyz789");
        AddSubmission(db, CourseA, "org/a-hw1-abc123");
        await db.SaveChangesAsync();

        var counts = await CreateService(db).CountSubmissionsAsync(CourseA);

        Assert.Equal(1, counts[assignment.Id]);
    }

    /// <summary>A repository nobody accepted belongs to no assignment — this is every pre-migration repo.</summary>
    [Fact]
    public async Task Counts_IgnoreSubmissionsWithNoAcceptance()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1");
        AddSubmission(db, CourseA, "org/a-legacy-abc123");
        await db.SaveChangesAsync();

        var counts = await CreateService(db).CountSubmissionsAsync(CourseA);

        Assert.False(counts.ContainsKey(assignment.Id));
    }

    [Fact]
    public async Task Counts_AreScopedToTheCourse()
    {
        await using var db = CreateContext();
        var inA = await AddAssignmentAsync(db, CourseA, "Homework 1");
        var inB = await AddAssignmentAsync(db, CourseB, "Homework 1");

        await AcceptAsync(db, CourseA, inA, "org/a-hw1-abc123");
        await AcceptAsync(db, CourseB, inB, "org/b-hw1-abc123");
        AddSubmission(db, CourseA, "org/a-hw1-abc123");
        AddSubmission(db, CourseB, "org/b-hw1-abc123");
        await db.SaveChangesAsync();

        var counts = await CreateService(db).CountSubmissionsAsync(CourseA);

        Assert.Equal(1, counts[inA.Id]);
        Assert.False(counts.ContainsKey(inB.Id));
    }

    /// <summary>
    /// The same repository name in another course must not count here. Submissions are unique per course, so
    /// this is only reachable across courses — which is exactly the case a name-only join gets wrong.
    /// </summary>
    [Fact]
    public async Task Counts_IgnoreAMatchingRepositoryInAnotherCourse()
    {
        await using var db = CreateContext();
        var assignment = await AddAssignmentAsync(db, CourseA, "Homework 1");
        await AcceptAsync(db, CourseA, assignment, "org/shared-hw1-abc123");
        AddSubmission(db, CourseB, "org/shared-hw1-abc123");
        await db.SaveChangesAsync();

        var counts = await CreateService(db).CountSubmissionsAsync(CourseA);

        Assert.False(counts.ContainsKey(assignment.Id));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // No current course: the service filters on the explicit course id and ignores the query filter.
        return new ApplicationDbContext(options, new FixedCourseProvider { CurrentCourseId = null });
    }

    private static AssignmentService CreateService(ApplicationDbContext db) => new(
        db,
        new Mock<IGitHubRepositoryService>(MockBehavior.Strict).Object,
        new Mock<ICourseGitHubAppTokenProvider>(MockBehavior.Strict).Object);

    private static async Task<Assignment> AddAssignmentAsync(ApplicationDbContext db, int courseId, string name)
    {
        var assignment = new Assignment
        {
            CourseId = courseId,
            Name = name,
            TemplateRepoName = $"org/course{courseId}-hw1",
            InviteToken = $"token-{courseId}-{name}",
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

    private static void AddSubmission(ApplicationDbContext db, int courseId, string repo) =>
        db.Submissions.Add(new Submission { CourseId = courseId, GitHubRepoName = repo });
}
