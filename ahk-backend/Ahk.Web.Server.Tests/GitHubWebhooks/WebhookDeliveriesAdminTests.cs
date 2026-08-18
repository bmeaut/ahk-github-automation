using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Admin;
using Ahk.Web.Server.Admin.Dto;
using Ahk.Web.Services.GitHubWebhooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// The admin delivery log, driven straight against the controller over EF InMemory — the questions here are
/// about projection and re-run semantics, not about routing or authorization.
/// </summary>
public class WebhookDeliveriesAdminTests : IDisposable
{
    private readonly ApplicationDbContext db;
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly WebhookDeliveriesAdminController controller;

    public WebhookDeliveriesAdminTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"deliveries-admin-{Guid.NewGuid()}")
            .Options;

        db = new ApplicationDbContext(options, new NoCourseProvider());
        db.Courses.Add(new Course { Id = 1, Slug = "viaubc01", Name = "Automated evaluation" });
        db.SaveChanges();

        controller = new WebhookDeliveriesAdminController(db, clock);
    }

    public void Dispose()
    {
        db.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The list must never carry the payload: it is the bulk of every row, and it holds commit messages and
    /// author emails out of private student repositories.
    /// </summary>
    [Fact]
    public async Task List_ReportsPayloadPresenceButNeverTheBody()
    {
        Add(GitHubWebhookDeliveryStatus.Succeeded, payload: """{"secret":"in the payload"}""");

        var result = await Ok(controller.List(courseId: null, status: null, repository: null));

        var item = Assert.Single(result.Items);
        Assert.True(item.HasPayload);
        Assert.Equal("viaubc01", item.CourseSlug);

        // The DTO has no payload property at all; this is the assertion that it stays that way.
        Assert.Null(typeof(WebhookDeliveryDto).GetProperty("Payload"));
    }

    /// <summary>
    /// Pending is the queue depth, so it is counted whatever its age — a delivery waiting since yesterday is
    /// exactly the one worth seeing. Everything else is a 24-hour tally.
    /// </summary>
    [Fact]
    public async Task Counts_TallyRecentWorkButAllWaitingWork()
    {
        Add(GitHubWebhookDeliveryStatus.Succeeded);
        Add(GitHubWebhookDeliveryStatus.Failed);
        Add(GitHubWebhookDeliveryStatus.Pending, receivedAt: clock.GetUtcNow().AddDays(-3));
        Add(GitHubWebhookDeliveryStatus.Succeeded, receivedAt: clock.GetUtcNow().AddDays(-3));

        var result = await Ok(controller.List(courseId: null, status: null, repository: null));

        Assert.Equal(1, result.Counts.Succeeded);
        Assert.Equal(1, result.Counts.Failed);
        Assert.Equal(1, result.Counts.Pending);
    }

    [Fact]
    public async Task Get_ReturnsTheRecordedHandlerOutcomes()
    {
        var id = Add(
            GitHubWebhookDeliveryStatus.Failed,
            outcomes: [new WebhookHandlerOutcome("PullRequestOpenDuplicateHandler", 0, null, "System.Exception: boom", 40)]);

        var detail = await Ok(controller.Get(id, CancellationToken.None));

        var outcome = Assert.Single(detail.Outcomes);
        Assert.Equal("PullRequestOpenDuplicateHandler", outcome.HandlerName);
        Assert.False(outcome.Succeeded);
    }

    [Fact]
    public async Task Retry_PutsTheDeliveryBackOnTheQueue()
    {
        var id = Add(
            GitHubWebhookDeliveryStatus.Failed,
            outcomes: [new WebhookHandlerOutcome("FirstHandler", 0, "action performed", null, 5)]);

        var response = await controller.Retry(id, new WebhookDeliveryRetryRequest(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(response.Result);

        var delivery = await db.GitHubWebhookDeliveries.SingleAsync(d => d.Id == id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(clock.GetUtcNow(), delivery.NextAttemptAt);
        Assert.Equal(0, delivery.AttemptCount);
        Assert.Null(delivery.Error);

        // Kept, because that record is exactly what tells the processor which handlers not to run again.
        Assert.NotNull(delivery.OutcomesJson);
    }

    /// <summary>"Re-run everything" is expressed by clearing the record, not by a flag sent to the worker.</summary>
    [Fact]
    public async Task RetryEverything_ClearsTheOutcomesSoNothingIsSkipped()
    {
        var id = Add(
            GitHubWebhookDeliveryStatus.Failed,
            outcomes: [new WebhookHandlerOutcome("FirstHandler", 0, "action performed", null, 5)]);

        await controller.Retry(id, new WebhookDeliveryRetryRequest { OnlyFailedHandlers = false }, CancellationToken.None);

        var delivery = await db.GitHubWebhookDeliveries.SingleAsync(d => d.Id == id);
        Assert.Null(delivery.OutcomesJson);
        Assert.Equal(0, delivery.HandlerCount);
    }

    [Fact]
    public async Task RetryWhileProcessing_Is409()
    {
        var id = Add(GitHubWebhookDeliveryStatus.Processing);

        var response = await controller.Retry(id, new WebhookDeliveryRetryRequest(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(response.Result);
    }

    /// <summary>
    /// A re-run without a payload would dispatch an empty body and surface as "request body was empty", which
    /// reads like a GitHub fault rather than our own retention policy.
    /// </summary>
    [Fact]
    public async Task RetryAfterThePayloadWasPruned_Is409()
    {
        var id = Add(GitHubWebhookDeliveryStatus.Succeeded, payload: null);

        var response = await controller.Retry(id, new WebhookDeliveryRetryRequest(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(response.Result);
    }

    [Fact]
    public async Task GetPayloadOfAPrunedDelivery_Is404()
    {
        var id = Add(GitHubWebhookDeliveryStatus.Succeeded, payload: null);

        Assert.IsType<NotFoundResult>(await controller.GetPayload(id, CancellationToken.None));
    }

    private static async Task<T> Ok<T>(Task<ActionResult<T>> call)
    {
        var result = await call;
        return (T)Assert.IsType<OkObjectResult>(result.Result).Value!;
    }

    private int Add(
        GitHubWebhookDeliveryStatus status,
        string? payload = """{"repository":{"full_name":"bmeaut/viaubc01-abc123"}}""",
        DateTimeOffset? receivedAt = null,
        WebhookHandlerOutcome[]? outcomes = null)
    {
        var delivery = new GitHubWebhookDelivery
        {
            CourseId = 1,
            DeliveryId = Guid.NewGuid().ToString(),
            EventName = "pull_request",
            RepositoryFullName = "bmeaut/viaubc01-abc123",
            Payload = payload,
            ReceivedAt = receivedAt ?? clock.GetUtcNow(),
            Status = status,
            OutcomesJson = outcomes is null ? null : System.Text.Json.JsonSerializer.Serialize(outcomes),
            HandlerCount = outcomes?.Length ?? 0,
            FailedHandlerCount = outcomes?.Count(o => !o.Succeeded) ?? 0,
        };

        db.GitHubWebhookDeliveries.Add(delivery);
        db.SaveChanges();
        return delivery.Id;
    }

    private sealed class NoCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId => null;
    }
}
