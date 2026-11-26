using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Submissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// The resolver losing a check-then-insert race to a concurrent writer: it must use the row that won, not throw.
///
/// <para>Seen in production: generating a repository from a template makes GitHub fire a <c>repository</c>
/// webhook at once, the delivery worker created the submission while the accept request was still busy with
/// GitHub, and the request's own insert hit the unique index. The 500 came after the repository and the
/// invitation existed but before the acceptance was saved, so the submission showed up under "No assignment".</para>
///
/// <para>EF InMemory enforces no unique index, so <see cref="ConcurrentInsert"/> stands in for the other writer:
/// on the first save it commits the same row through a second context, then throws what SQL Server would.</para>
/// </summary>
public class SubmissionResolverRaceTests
{
    private const int CourseId = 1;

    private sealed class NoCourse : ICurrentCourseProvider
    {
        public int? CurrentCourseId => null;
    }

    private sealed class ConcurrentInsert : SaveChangesInterceptor
    {
        private readonly Func<Task> insertWinner;
        private bool fired;

        public ConcurrentInsert(Func<Task> insertWinner) => this.insertWinner = insertWinner;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (fired)
                return result;

            fired = true;
            await insertWinner();
            throw new DbUpdateException("Cannot insert duplicate key row (simulated unique index violation).");
        }
    }

    [Fact]
    public async Task Submission_LosingTheInsertRace_ReturnsTheRowThatWon()
    {
        const string repo = "viauad02/viauad02-lab-android-n52eux";
        var database = Guid.NewGuid().ToString();
        int? winnerId = null;

        await using var db = CreateContext(database, new ConcurrentInsert(async () =>
        {
            await using var worker = CreateContext(database);
            var winner = new Submission { CourseId = CourseId, GitHubRepoName = repo };
            worker.Submissions.Add(winner);
            await worker.SaveChangesAsync();
            winnerId = winner.Id;
        }));

        var submission = await CreateResolver(db).GetOrCreateAsync(CourseId, repo);

        Assert.NotNull(winnerId);
        Assert.Equal(winnerId, submission.Id);
        await using var check = CreateContext(database);
        Assert.Single(await check.Submissions.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Submission_LosingTheRace_StillLinksTheStudent()
    {
        const string repo = "viauad02/viauad02-lab-android-n52eux";
        var database = Guid.NewGuid().ToString();

        await using var db = CreateContext(database, new ConcurrentInsert(async () =>
        {
            await using var worker = CreateContext(database);
            worker.Submissions.Add(new Submission { CourseId = CourseId, GitHubRepoName = repo });
            await worker.SaveChangesAsync();
        }));

        var submission = await CreateResolver(db).GetOrCreateAsync(CourseId, repo, "n52eux");

        await using var check = CreateContext(database);
        var stored = await check.Submissions.IgnoreQueryFilters().Include(s => s.Student).SingleAsync();
        Assert.Equal(submission.Id, stored.Id);
        Assert.Equal("N52EUX", stored.Student?.Neptun);
    }

    [Fact]
    public async Task Student_LosingTheInsertRace_ReturnsTheRowThatWon()
    {
        var database = Guid.NewGuid().ToString();
        int? winnerId = null;

        await using var db = CreateContext(database, new ConcurrentInsert(async () =>
        {
            await using var worker = CreateContext(database);
            var winner = new Student { CourseId = CourseId, Neptun = "N52EUX" };
            worker.Students.Add(winner);
            await worker.SaveChangesAsync();
            winnerId = winner.Id;
        }));

        var student = await CreateResolver(db).GetOrCreateStudentAsync(CourseId, "n52eux");

        Assert.Equal(winnerId, student.Id);
        await using var check = CreateContext(database);
        Assert.Single(await check.Students.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task AFailureThatIsNotARace_StillThrows()
    {
        // The interceptor throws but inserts nothing, so there is no winner to fall back on.
        await using var db = CreateContext(Guid.NewGuid().ToString(), new ConcurrentInsert(() => Task.CompletedTask));

        await Assert.ThrowsAsync<DbUpdateException>(() => CreateResolver(db).GetOrCreateAsync(CourseId, "org/repo"));
    }

    private static SubmissionResolver CreateResolver(ApplicationDbContext db) => new(db, new SubmissionArchiveService(db));

    private static ApplicationDbContext CreateContext(string database, IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(database);
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);

        return new ApplicationDbContext(builder.Options, new NoCourse());
    }
}
