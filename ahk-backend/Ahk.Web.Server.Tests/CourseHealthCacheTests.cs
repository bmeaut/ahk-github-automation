using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Admin;
using Ahk.Web.Server.Admin.Dto;
using Ahk.Web.Services.Courses;
using Ahk.Web.Services.Health;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Covers the health verdict cached on the course row: that every run writes it, that the course register
/// serves it without running anything, and that the background refresh skips what is already current.
///
/// The point of the cache is that /admin/courses never waits on GitHub, so much of what is asserted here is
/// an <em>absence</em> — a check that did not run, a course that was not touched.
/// </summary>
public class CourseHealthCacheTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    private sealed class NoCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId => null;
    }

    /// <summary>A check with a fixed verdict, so a report's aggregate and summary are whatever a test wants.</summary>
    private sealed class StubCheck : ICourseHealthCheck
    {
        private readonly HealthStatus status;

        public StubCheck(string id, string title, HealthStatus status)
        {
            Id = id;
            Title = title;
            this.status = status;
        }

        public string Id { get; }

        public string Title { get; }

        public int Order => 10;

        public Task<HealthCheckResult> RunAsync(Course course, CancellationToken cancellationToken = default) =>
            Task.FromResult(new HealthCheckResult { CheckId = Id, Title = Title, Status = status, Message = "stub" });
    }

    private static ApplicationDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options, new NoCourseProvider());

    private static async Task<string> SeedCoursesAsync(params Course[] courses)
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateContext(dbName);
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync();
        return dbName;
    }

    private static CourseHealthService ServiceOver(ApplicationDbContext db, params ICourseHealthCheck[] checks) =>
        new(db, new FakeTimeProvider(Now), checks);

    // ---- Writing the cache ----

    [Fact]
    public async Task CheckingACourse_StampsTheVerdictOnTheCourseRow()
    {
        var dbName = await SeedCoursesAsync(new Course { Id = 1, Slug = "viaubc01", Name = "Sample" });

        await using (var db = CreateContext(dbName))
        {
            await ServiceOver(
                db,
                new StubCheck("a", "Webhook settings", HealthStatus.Healthy),
                new StubCheck("b", "GitHub access token", HealthStatus.Failed))
                .CheckCourseAsync(1);
        }

        await using var verify = CreateContext(dbName);
        var course = await verify.Courses.SingleAsync(c => c.Id == 1);

        Assert.Equal(HealthStatus.Failed, course.HealthStatus);
        Assert.Equal(Now, course.HealthCheckedAt);
        Assert.Equal("GitHub access token", course.HealthSummary);
    }

    /// <summary>The summary names what to go and fix — so a course with nothing wrong names nothing.</summary>
    [Fact]
    public async Task WhenEveryCheckPasses_TheSummaryIsEmpty()
    {
        var dbName = await SeedCoursesAsync(new Course { Id = 1, Slug = "viaubc01", Name = "Sample" });

        await using (var db = CreateContext(dbName))
        {
            await ServiceOver(db, new StubCheck("a", "Webhook settings", HealthStatus.Healthy)).CheckCourseAsync(1);
        }

        await using var verify = CreateContext(dbName);
        var course = await verify.Courses.SingleAsync(c => c.Id == 1);

        Assert.Equal(HealthStatus.Healthy, course.HealthStatus);
        Assert.Equal(string.Empty, course.HealthSummary);
    }

    /// <summary>Warnings and unconfigured checks are worth naming too: both are things an admin can act on.</summary>
    [Fact]
    public async Task TheSummaryNamesEveryCheckThatDidNotPass()
    {
        var dbName = await SeedCoursesAsync(new Course { Id = 1, Slug = "viaubc01", Name = "Sample" });

        await using (var db = CreateContext(dbName))
        {
            await ServiceOver(
                db,
                new StubCheck("a", "Webhook settings", HealthStatus.Warning),
                new StubCheck("b", "GitHub access token", HealthStatus.Healthy),
                new StubCheck("c", "CI callback token", HealthStatus.NotConfigured))
                .CheckCourseAsync(1);
        }

        await using var verify = CreateContext(dbName);
        var course = await verify.Courses.SingleAsync(c => c.Id == 1);

        Assert.Equal("Webhook settings, CI callback token", course.HealthSummary);
    }

    [Fact]
    public async Task CheckingEveryCourse_StampsEveryCourse()
    {
        var dbName = await SeedCoursesAsync(
            new Course { Id = 1, Slug = "viaubc01", Name = "One" },
            new Course { Id = 2, Slug = "viaubb01", Name = "Two" });

        await using (var db = CreateContext(dbName))
        {
            await ServiceOver(db, new StubCheck("a", "Webhook settings", HealthStatus.Warning)).CheckAllCoursesAsync();
        }

        await using var verify = CreateContext(dbName);
        Assert.All(await verify.Courses.ToListAsync(), c =>
        {
            Assert.Equal(HealthStatus.Warning, c.HealthStatus);
            Assert.Equal(Now, c.HealthCheckedAt);
        });
    }

    // ---- Serving the cache ----

    [Fact]
    public async Task TheCourseRegister_ServesTheCachedVerdictAndFlagsStaleness()
    {
        var dbName = await SeedCoursesAsync(
            new Course
            {
                Id = 1,
                Slug = "fresh",
                Name = "Fresh",
                HealthStatus = HealthStatus.Healthy,
                HealthCheckedAt = Now.AddHours(-1),
                HealthSummary = string.Empty,
            },
            new Course
            {
                Id = 2,
                Slug = "stale",
                Name = "Stale",
                HealthStatus = HealthStatus.Failed,
                HealthCheckedAt = Now.AddHours(-25),
                HealthSummary = "GitHub access token",
            },
            new Course { Id = 3, Slug = "never", Name = "Never" });

        await using var db = CreateContext(dbName);
        var controller = new CoursesAdminController(
            db,
            Mock.Of<IWebhookTokenService>(),
            new FakeTimeProvider(Now),
            Options.Create(new CourseHealthOptions()));

        var result = await controller.List(CancellationToken.None);
        var courses = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(Assert.IsType<OkObjectResult>(result.Result).Value)
            .ToDictionary(c => c.Slug, StringComparer.Ordinal);

        Assert.Equal(HealthStatus.Healthy, courses["fresh"].HealthStatus);
        Assert.False(courses["fresh"].HealthStale);

        Assert.Equal(HealthStatus.Failed, courses["stale"].HealthStatus);
        Assert.Equal("GitHub access token", courses["stale"].HealthSummary);
        Assert.True(courses["stale"].HealthStale);

        // Never checked reads as stale — that is what queues the first run — but shows no verdict.
        Assert.Null(courses["never"].HealthStatus);
        Assert.True(courses["never"].HealthStale);
    }

    // ---- Queueing a refresh ----

    [Fact]
    public async Task RefreshStale_QueuesOnlyTheCoursesPastTheTtl()
    {
        var dbName = await SeedCoursesAsync(
            new Course { Id = 1, Slug = "fresh", Name = "Fresh", HealthCheckedAt = Now.AddHours(-1) },
            new Course { Id = 2, Slug = "stale", Name = "Stale", HealthCheckedAt = Now.AddHours(-25) },
            new Course { Id = 3, Slug = "never", Name = "Never" });

        await using var db = CreateContext(dbName);
        var queue = new CourseHealthRefreshQueue();
        var controller = new CourseHealthAdminController(
            Mock.Of<ICourseHealthService>(MockBehavior.Strict),
            db,
            queue,
            new FakeTimeProvider(Now),
            Options.Create(new CourseHealthOptions()));

        // A strict, setup-free health service proves the point: queueing a refresh runs no check itself.
        var accepted = Assert.IsType<AcceptedResult>(await controller.RefreshStale(CancellationToken.None));
        Assert.NotNull(accepted.Value);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var queued = new List<int>();
        await foreach (var id in queue.DequeueAllAsync(cts.Token))
        {
            queued.Add(id);
            if (queued.Count == 2)
                break;
        }

        Assert.Equal(new[] { 2, 3 }, queued);
    }

    // ---- The worker's skip rule ----

    /// <summary>
    /// The queue deliberately keeps no de-duplication state: the worker re-reads the timestamp instead. A
    /// course already refreshed — by an earlier queued run, or by an admin opening the health dashboard — is
    /// dropped rather than checked again.
    /// </summary>
    [Fact]
    public async Task TheRefreshWorker_SkipsACourseThatIsAlreadyCurrent()
    {
        var dbName = await SeedCoursesAsync(
            new Course { Id = 1, Slug = "fresh", Name = "Fresh", HealthCheckedAt = Now.AddHours(-1) },
            new Course { Id = 2, Slug = "stale", Name = "Stale", HealthCheckedAt = Now.AddHours(-25) });

        var checkedCourses = new List<int>();
        var sawStale = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var health = new Mock<ICourseHealthService>();
        health.Setup(h => h.CheckCourseAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((int courseId, CancellationToken _) =>
            {
                checkedCourses.Add(courseId);
                sawStale.TrySetResult();
                return Task.FromResult<CourseHealthReport?>(new CourseHealthReport { CourseId = courseId });
            });

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentCourseProvider, NoCourseProvider>();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped(_ => health.Object);
        await using var provider = services.BuildServiceProvider();

        var queue = new CourseHealthRefreshQueue();

        // Fresh first: the worker is strictly sequential, so once the stale course has been checked we know
        // the fresh one was already dequeued — and skipped.
        queue.Enqueue(1);
        queue.Enqueue(2);

        var worker = new CourseHealthRefreshWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            queue,
            new FakeTimeProvider(Now),
            Options.Create(new CourseHealthOptions()),
            NullLogger<CourseHealthRefreshWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            await sawStale.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        Assert.Equal(new[] { 2 }, checkedCourses);
    }
}
