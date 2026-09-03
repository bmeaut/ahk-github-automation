using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHubWebhooks;
using Ahk.Web.Services.GitHubWebhooks.Handlers.StatusTracking;
using Ahk.Web.Services.StatusTracking;
using Ahk.Web.Services.Submissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;
using Octokit.Internal;
using Xunit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// The status-tracking handlers, run against a real <see cref="SubmissionEventService"/> on EF InMemory.
///
/// These handlers are the whole of the dashboard's data: what a teacher sees is a projection over the rows
/// written here, so the mapping from GitHub payload to event subtype is worth pinning field by field.
/// </summary>
public class WebhookStatusEventTests
{
    private const int CourseId = 1;
    private const string Repository = "bmeaut/viaubc01-abc123";

    /// <summary>Creation of the *default* branch is how a new repository is recognised.</summary>
    [Fact]
    public async Task DefaultBranchCreate_RecordsRepositoryCreated()
    {
        using var db = NewContext(nameof(DefaultBranchCreate_RecordsRepositoryCreated));
        var handler = NewBranchHandler(db);

        var result = await handler.ExecuteAsync(NewContextFor(CreateBranchPayload("main"), "delivery-1"));

        Assert.Contains("repository create lifecycle handled", result.Result, StringComparison.Ordinal);
        var recorded = Assert.Single(await ReadEventsAsync(db));
        Assert.IsType<RepositoryCreatedEvent>(recorded);
        Assert.Equal("delivery-1", recorded.GitHubDeliveryId);
    }

    [Fact]
    public async Task NonDefaultBranchCreate_RecordsBranchCreated()
    {
        using var db = NewContext(nameof(NonDefaultBranchCreate_RecordsBranchCreated));
        var handler = NewBranchHandler(db);

        var result = await handler.ExecuteAsync(NewContextFor(CreateBranchPayload("feature/homework"), "delivery-1"));

        Assert.Contains("branch create lifecycle handled", result.Result, StringComparison.Ordinal);
        var recorded = Assert.Single(await ReadEventsAsync(db));
        Assert.Equal("feature/homework", Assert.IsType<BranchCreatedEvent>(recorded).Branch);
    }

    /// <summary>A tag is not a branch.</summary>
    [Fact]
    public async Task TagCreate_RecordsNothing()
    {
        using var db = NewContext(nameof(TagCreate_RecordsNothing));
        var handler = NewBranchHandler(db);

        var result = await handler.ExecuteAsync(NewContextFor(CreateBranchPayload("v1.0", refType: "tag"), "delivery-1"));

        Assert.Contains("not of interest", result.Result, StringComparison.Ordinal);
        Assert.Empty(await ReadEventsAsync(db));
    }

    [Fact]
    public async Task PullRequestOpened_RecordsPullRequestEventAndLinksTheStudent()
    {
        using var db = NewContext(nameof(PullRequestOpened_RecordsPullRequestEventAndLinksTheStudent));
        var handler = new PullRequestStatusTrackingHandler(NewEventService(db), NewCache(), NullLogger<PullRequestStatusTrackingHandler>.Instance);

        var result = await handler.ExecuteAsync(NewContextFor(PullRequestPayload("opened"), "delivery-1"));

        Assert.Contains("pull request lifecycle handled", result.Result, StringComparison.Ordinal);

        var recorded = Assert.IsType<Ahk.Web.Data.Entities.PullRequestEvent>(Assert.Single(await ReadEventsAsync(db)));
        Assert.Equal(12, recorded.Number);
        Assert.Equal("opened", recorded.Action);
        Assert.Equal("https://github.com/bmeaut/viaubc01-abc123/pull/12", recorded.HtmlUrl);
        Assert.Equal("ABC123", recorded.Neptun);
        Assert.Equal(new[] { "teacher1" }, recorded.Assignees);

        // neptun.txt is what ties a repository to a person; a pull request is usually where it first appears.
        var submission = await db.Submissions.IgnoreQueryFilters().Include(s => s.Student).SingleAsync();
        Assert.Equal("ABC123", submission.Student!.Neptun);
    }

    [Fact]
    public async Task PullRequestSynchronize_IsNotOfInterest()
    {
        using var db = NewContext(nameof(PullRequestSynchronize_IsNotOfInterest));
        var handler = new PullRequestStatusTrackingHandler(NewEventService(db), NewCache(), NullLogger<PullRequestStatusTrackingHandler>.Instance);

        var result = await handler.ExecuteAsync(NewContextFor(PullRequestPayload("synchronize"), "delivery-1"));

        Assert.Contains("not of interest", result.Result, StringComparison.Ordinal);
        Assert.Empty(await ReadEventsAsync(db));
    }

    [Fact]
    public async Task CompletedWorkflowRun_RecordsItsConclusion()
    {
        using var db = NewContext(nameof(CompletedWorkflowRun_RecordsItsConclusion));
        var handler = new WorkflowRunStatusTrackingHandler(NewEventService(db), NewCache(), NullLogger<WorkflowRunStatusTrackingHandler>.Instance);

        var result = await handler.ExecuteAsync(NewContextFor(WorkflowRunPayload("completed", "failure"), "delivery-1"));

        Assert.Contains("workflow_run lifecycle handled", result.Result, StringComparison.Ordinal);
        Assert.Equal("failure", Assert.IsType<Ahk.Web.Data.Entities.WorkflowRunEvent>(Assert.Single(await ReadEventsAsync(db))).Conclusion);
    }

    /// <summary>
    /// GitHub redelivers, and an administrator can redeliver by hand from the Advanced tab. The delivery id
    /// makes that idempotent.
    ///
    /// <para>Note this exercises <see cref="SubmissionEventService"/>'s own explicit guard rather than the
    /// database: EF InMemory does not enforce the filtered unique index that backs it in production.</para>
    /// </summary>
    [Fact]
    public async Task RedeliveringTheSameDelivery_RecordsOneRow()
    {
        using var db = NewContext(nameof(RedeliveringTheSameDelivery_RecordsOneRow));
        var handler = NewBranchHandler(db);

        var first = await handler.ExecuteAsync(NewContextFor(CreateBranchPayload("feature/homework"), "delivery-1"));
        var second = await handler.ExecuteAsync(NewContextFor(CreateBranchPayload("feature/homework"), "delivery-1"));

        Assert.Contains("action performed", first.Result, StringComparison.Ordinal);
        Assert.Contains("redelivery, event already recorded", second.Result, StringComparison.Ordinal);
        Assert.Single(await ReadEventsAsync(db));
    }

    /// <summary>Two genuine deliveries are two events, redelivery guard notwithstanding.</summary>
    [Fact]
    public async Task DistinctDeliveries_RecordBothRows()
    {
        using var db = NewContext(nameof(DistinctDeliveries_RecordBothRows));
        var handler = NewBranchHandler(db);

        await handler.ExecuteAsync(NewContextFor(CreateBranchPayload("feature/a"), "delivery-1"));
        await handler.ExecuteAsync(NewContextFor(CreateBranchPayload("feature/b"), "delivery-2"));

        Assert.Equal(2, (await ReadEventsAsync(db)).Count);
    }

    /// <summary>
    /// The opt-in gate. A repository without <c>.github/ahk-monitor.yml</c> is ignored entirely — no event, no
    /// rule enforced — which is the single most common reason a correctly wired webhook appears to do nothing.
    /// </summary>
    [Fact]
    public async Task RepositoryWithoutAhkMonitorYml_IsIgnored()
    {
        using var db = NewContext(nameof(RepositoryWithoutAhkMonitorYml_IsIgnored));
        var handler = NewBranchHandler(db);

        var result = await handler.ExecuteAsync(NewContextFor(CreateBranchPayload("main"), "delivery-1", monitorYml: null));

        Assert.Contains("no ahk-monitor.yml or disabled", result.Result, StringComparison.Ordinal);
        Assert.Empty(await ReadEventsAsync(db));
    }

    [Fact]
    public async Task RepositoryWithDisabledAhkMonitorYml_IsIgnored()
    {
        using var db = NewContext(nameof(RepositoryWithDisabledAhkMonitorYml_IsIgnored));
        var handler = NewBranchHandler(db);

        var result = await handler.ExecuteAsync(NewContextFor(CreateBranchPayload("main"), "delivery-1", monitorYml: "enabled: false"));

        Assert.Contains("no ahk-monitor.yml or disabled", result.Result, StringComparison.Ordinal);
        Assert.Empty(await ReadEventsAsync(db));
    }

    [Fact]
    public async Task GarbagePayload_IsReportedNotThrown()
    {
        using var db = NewContext(nameof(GarbagePayload_IsReportedNotThrown));
        var handler = NewBranchHandler(db);

        var result = await handler.ExecuteAsync(NewContextFor("{\"nonsense\":1}", "delivery-1"));

        Assert.Contains("payload error", result.Result, StringComparison.Ordinal);
        Assert.Empty(await ReadEventsAsync(db));
    }

    private static BranchCreateStatusTrackingHandler NewBranchHandler(ApplicationDbContext db)
        => new(NewEventService(db), NewCache(), NullLogger<BranchCreateStatusTrackingHandler>.Instance);

    private static ISubmissionEventService NewEventService(ApplicationDbContext db)
        => new SubmissionEventService(db, new SubmissionResolver(db, new SubmissionArchiveService(db)));

    private static IMemoryCache NewCache() => new MemoryCache(new MemoryCacheOptions());

    private static async Task<IReadOnlyList<SubmissionEvent>> ReadEventsAsync(ApplicationDbContext db)
        => await db.SubmissionEvents.IgnoreQueryFilters().ToListAsync();

    private static ApplicationDbContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
        var db = new ApplicationDbContext(options, new FixedCourseProvider());

        db.Courses.Add(new Course { Id = CourseId, Slug = "viaubc01", Name = "Sample Course", GitHubOrganization = "bmeaut" });
        db.SaveChanges();

        return db;
    }

    private static GitHubWebhookContext NewContextFor(string body, string deliveryId, string? monitorYml = "enabled: true")
        => new()
        {
            CourseId = CourseId,
            GitHubEventName = "create",
            DeliveryId = deliveryId,
            RequestBody = body,
            GitHubClient = NewGitHubClient(monitorYml),
            WorkflowRunThreshold = 5,
        };

    /// <summary>
    /// Only the two file reads the handlers make are stubbed. Octokit models are built by deserializing the
    /// JSON GitHub would send rather than through their constructors, which are long and mostly irrelevant.
    /// </summary>
    private static IGitHubClient NewGitHubClient(string? monitorYml)
    {
        var contents = new Mock<IRepositoryContentsClient>();

        contents
            .Setup(c => c.GetAllContentsByRef(It.IsAny<long>(), ".github/ahk-monitor.yml", It.IsAny<string>()))
            .Returns(() => monitorYml is null
                ? throw new NotFoundException("not found", System.Net.HttpStatusCode.NotFound)
                : Task.FromResult<IReadOnlyList<RepositoryContent>>(new[] { FileContent(monitorYml) }));

        contents
            .Setup(c => c.GetAllContentsByRef(It.IsAny<long>(), "neptun.txt", It.IsAny<string>()))
            .ReturnsAsync(new[] { FileContent("ABC123\n") });

        var repositories = new Mock<IRepositoriesClient>();
        repositories.SetupGet(r => r.Content).Returns(contents.Object);

        var client = new Mock<IGitHubClient>();
        client.SetupGet(c => c.Repository).Returns(repositories.Object);

        return client.Object;
    }

    private static RepositoryContent FileContent(string text)
    {
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
        return new SimpleJsonSerializer().Deserialize<RepositoryContent>(
            $$"""{"name":"f","path":"f","sha":"s","size":{{text.Length}},"type":"file","encoding":"base64","content":"{{encoded}}"}""");
    }

    private static string CreateBranchPayload(string branch, string refType = "branch")
        => $$"""
        {
          "ref": "{{branch}}",
          "ref_type": "{{refType}}",
          "repository": { "id": 55, "name": "viaubc01-abc123", "full_name": "{{Repository}}", "default_branch": "main",
                          "owner": { "login": "bmeaut", "id": 9, "type": "Organization" } },
          "installation": { "id": 123 }
        }
        """;

    private static string PullRequestPayload(string action)
        => $$"""
        {
          "action": "{{action}}",
          "number": 12,
          "pull_request": {
            "number": 12,
            "html_url": "https://github.com/bmeaut/viaubc01-abc123/pull/12",
            "head": { "ref": "feature/homework" },
            "assignees": [ { "login": "teacher1", "id": 3 } ]
          },
          "repository": { "id": 55, "name": "viaubc01-abc123", "full_name": "{{Repository}}", "default_branch": "main",
                          "owner": { "login": "bmeaut", "id": 9, "type": "Organization" } },
          "installation": { "id": 123 }
        }
        """;

    private static string WorkflowRunPayload(string action, string conclusion)
        => $$"""
        {
          "action": "{{action}}",
          "workflow_run": { "conclusion": "{{conclusion}}" },
          "repository": { "id": 55, "name": "viaubc01-abc123", "full_name": "{{Repository}}", "default_branch": "main",
                          "owner": { "login": "bmeaut", "id": 9, "type": "Organization" } },
          "installation": { "id": 123 }
        }
        """;

    private sealed class FixedCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId => CourseId;
    }
}
