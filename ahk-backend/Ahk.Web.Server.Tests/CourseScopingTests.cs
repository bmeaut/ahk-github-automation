using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Verifies the EF Core course query filter: a request scoped to one course only sees that course's rows.
/// This is the core isolation mechanism, so it is exercised directly against the DbContext.
/// </summary>
public class CourseScopingTests
{
    private sealed class MutableCourseProvider : ICurrentCourseProvider
    {
        public Guid? CurrentCourseId { get; set; }
    }

    private static ApplicationDbContext CreateContext(ICurrentCourseProvider provider, string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options, provider);
    }

    [Fact]
    public async Task CourseNotes_AreFilteredToTheCurrentCourse()
    {
        var dbName = Guid.NewGuid().ToString();
        var courseA = Guid.NewGuid();
        var courseB = Guid.NewGuid();

        // Seed notes for two courses using an unscoped provider (null → filter matches nothing, so use raw add).
        var seedProvider = new MutableCourseProvider { CurrentCourseId = null };
        await using (var seed = CreateContext(seedProvider, dbName))
        {
            seed.CourseNotes.Add(new CourseNote { CourseId = courseA, Text = "A1" });
            seed.CourseNotes.Add(new CourseNote { CourseId = courseA, Text = "A2" });
            seed.CourseNotes.Add(new CourseNote { CourseId = courseB, Text = "B1" });
            await seed.SaveChangesAsync();
        }

        var provider = new MutableCourseProvider { CurrentCourseId = courseA };
        await using (var scoped = CreateContext(provider, dbName))
        {
            var notes = await scoped.CourseNotes.ToListAsync();
            Assert.Equal(2, notes.Count);
            Assert.All(notes, n => Assert.Equal(courseA, n.CourseId));
        }

        provider.CurrentCourseId = courseB;
        await using (var scoped = CreateContext(provider, dbName))
        {
            var notes = await scoped.CourseNotes.ToListAsync();
            Assert.Single(notes);
            Assert.Equal("B1", notes[0].Text);
        }
    }

    [Fact]
    public async Task CourseNotes_AreHiddenWhenNoCourseIsResolved()
    {
        var dbName = Guid.NewGuid().ToString();
        var provider = new MutableCourseProvider { CurrentCourseId = null };
        await using var db = CreateContext(provider, dbName);

        db.CourseNotes.Add(new CourseNote { CourseId = Guid.NewGuid(), Text = "orphan" });
        await db.SaveChangesAsync();

        Assert.Empty(await db.CourseNotes.ToListAsync());
    }
}
