using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Verifies the EF Core course query filter across the domain entities: a request scoped to one course only
/// sees that course's rows. This is the isolation mechanism the whole portal relies on, so it is exercised
/// directly against the DbContext.
/// </summary>
public class CourseScopingTests
{
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

    private const int CourseA = 1;
    private const int CourseB = 2;

    /// <summary>Seeds one submission with an event and a grade for each of two courses.</summary>
    private static async Task SeedTwoCoursesAsync(string dbName)
    {
        await using var db = CreateContext(new MutableCourseProvider { CurrentCourseId = null }, dbName);

        foreach (var (courseId, repo, neptun) in new[] { (CourseA, "org/a-hw1-abc123", "ABC123"), (CourseB, "org/b-hw1-xyz789", "XYZ789") })
        {
            var student = new Student { CourseId = courseId, Neptun = neptun };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            var submission = new Submission { CourseId = courseId, StudentId = student.Id, GitHubRepoName = repo };
            db.Submissions.Add(submission);
            await db.SaveChangesAsync();

            db.SubmissionEvents.Add(new BranchCreatedEvent
            {
                CourseId = courseId,
                SubmissionId = submission.Id,
                Timestamp = DateTimeOffset.UtcNow,
                Branch = "solution",
            });

            var grade = new GradeRecord
            {
                CourseId = courseId,
                SubmissionId = submission.Id,
                StudentId = student.Id,
                Neptun = neptun,
                Date = DateTimeOffset.UtcNow,
                Confirmed = true,
            };
            grade.Points.Add(new GradeExercisePoint { Name = "ex0", Point = 1, Order = 0 });
            db.GradeRecords.Add(grade);

            await db.SaveChangesAsync();
        }
    }

    [Theory]
    [InlineData(CourseA, "org/a-hw1-abc123")]
    [InlineData(CourseB, "org/b-hw1-xyz789")]
    public async Task Submissions_AreFilteredToTheCurrentCourse(int courseId, string expectedRepo)
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoCoursesAsync(dbName);

        await using var db = CreateContext(new MutableCourseProvider { CurrentCourseId = courseId }, dbName);

        var submissions = await db.Submissions.ToListAsync();
        Assert.Single(submissions);
        Assert.Equal(expectedRepo, submissions[0].GitHubRepoName);
    }

    [Fact]
    public async Task Students_Events_AndGrades_AreFilteredToTheCurrentCourse()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoCoursesAsync(dbName);

        await using var db = CreateContext(new MutableCourseProvider { CurrentCourseId = CourseA }, dbName);

        Assert.All(await db.Students.ToListAsync(), s => Assert.Equal(CourseA, s.CourseId));
        Assert.All(await db.SubmissionEvents.ToListAsync(), e => Assert.Equal(CourseA, e.CourseId));
        Assert.All(await db.GradeRecords.ToListAsync(), g => Assert.Equal(CourseA, g.CourseId));

        Assert.Single(await db.GradeRecords.ToListAsync());
    }

    /// <summary>
    /// Guards the trap called out in the plan: with no course resolved (machine-to-machine paths, the importer)
    /// the filter matches nothing, so such callers must set a provider or use IgnoreQueryFilters.
    /// </summary>
    [Fact]
    public async Task NoCurrentCourse_HidesAllCourseScopedRows()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedTwoCoursesAsync(dbName);

        await using var db = CreateContext(new MutableCourseProvider { CurrentCourseId = null }, dbName);

        Assert.Empty(await db.Submissions.ToListAsync());
        Assert.Empty(await db.GradeRecords.ToListAsync());

        // ...but the rows are there when the filter is bypassed.
        Assert.Equal(2, await db.Submissions.IgnoreQueryFilters().CountAsync());
    }
}
