using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// The course query filter must cover the assignment entities too. Assignments carry an invite link that
/// provisions repositories, so one course seeing another's would be worse than a data leak — it would let a
/// student of course A accept course B's homework.
/// </summary>
public class AssignmentScopingTests
{
    private const int CourseA = 1;
    private const int CourseB = 2;

    private sealed class MutableCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId { get; set; }
    }

    private static ApplicationDbContext CreateContext(ICurrentCourseProvider provider, string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options, provider);
    }

    private static async Task SeedTwoCoursesAsync(string dbName)
    {
        await using var db = CreateContext(new MutableCourseProvider { CurrentCourseId = null }, dbName);

        foreach (var (courseId, slug) in new[] { (CourseA, "a"), (CourseB, "b") })
        {
            var user = new ApplicationUser { Id = courseId + 100, UserName = $"student-{slug}", NeptunCode = "ABC123" };
            db.Users.Add(user);

            var assignment = new Assignment
            {
                CourseId = courseId,
                Name = $"Homework of {slug}",
                TemplateRepoName = $"org/{slug}-hw1",
                InviteToken = $"token-{slug}",
            };
            db.Assignments.Add(assignment);
            await db.SaveChangesAsync();

            db.AssignmentAcceptances.Add(new AssignmentAcceptance
            {
                CourseId = courseId,
                AssignmentId = assignment.Id,
                UserId = user.Id,
                GitHubRepoName = $"org/{slug}-hw1-abc123",
                RepoUrl = $"https://github.com/org/{slug}-hw1-abc123",
                GitHubUsername = "octocat",
            });
            await db.SaveChangesAsync();
        }
    }

    [Theory]
    [InlineData(CourseA, "org/a-hw1")]
    [InlineData(CourseB, "org/b-hw1")]
    public async Task Assignments_AreFilteredToTheCurrentCourse(int courseId, string expectedTemplate)
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoCoursesAsync(dbName);

        await using var db = CreateContext(new MutableCourseProvider { CurrentCourseId = courseId }, dbName);

        var assignments = await db.Assignments.ToListAsync();
        Assert.Single(assignments);
        Assert.Equal(expectedTemplate, assignments[0].TemplateRepoName);
    }

    [Fact]
    public async Task Acceptances_AreFilteredToTheCurrentCourse()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoCoursesAsync(dbName);

        await using var db = CreateContext(new MutableCourseProvider { CurrentCourseId = CourseA }, dbName);

        var acceptances = await db.AssignmentAcceptances.ToListAsync();
        Assert.Single(acceptances);
        Assert.Equal(CourseA, acceptances[0].CourseId);
    }

    /// <summary>
    /// The student home page and the invite service run without a {course} route segment, so they see nothing
    /// unless they bypass the filter. This pins that trap in place rather than leaving it to be rediscovered.
    /// </summary>
    [Fact]
    public async Task NoCurrentCourse_HidesAssignmentsUnlessFiltersAreIgnored()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoCoursesAsync(dbName);

        await using var db = CreateContext(new MutableCourseProvider { CurrentCourseId = null }, dbName);

        Assert.Empty(await db.Assignments.ToListAsync());
        Assert.Empty(await db.AssignmentAcceptances.ToListAsync());

        Assert.Equal(2, await db.Assignments.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.AssignmentAcceptances.IgnoreQueryFilters().CountAsync());
    }
}
