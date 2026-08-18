using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.GitHubWebhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Octokit;
using Xunit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// What the worker does with one queued delivery, driven directly over EF InMemory with no host.
///
/// The claim is plain EF rather than raw SQL — a single worker in a single process is the only claimer — which
/// is what lets these run on the same provider CI already has, with no LocalDB and no second implementation.
/// </summary>
public class GitHubWebhookDeliveryProcessorTests
{
    private const int CourseId = 1;

    [Fact]
    public async Task SuccessfulDelivery_IsSucceededWithOrderedOutcomes()
    {
        var harness = new Harness();
        harness.Dispatcher.Returns(
            new WebhookHandlerOutcome("FirstHandler", 0, "action performed: commented", null, 12),
            new WebhookHandlerOutcome("SecondHandler", 1, "no action needed: nothing to do", null, 3));

        var id = await harness.QueueAsync("pull_request");
        Assert.True(await harness.Processor.ProcessNextAsync());

        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Succeeded, delivery.Status);
        Assert.Equal(1, delivery.AttemptCount);
        Assert.NotNull(delivery.CompletedAt);
        Assert.Equal(2, delivery.HandlerCount);
        Assert.Equal(0, delivery.FailedHandlerCount);

        var outcomes = Harness.OutcomesOf(delivery);
        Assert.Equal(new[] { "FirstHandler", "SecondHandler" }, outcomes.Select(o => o.HandlerName));
        Assert.Equal("action performed: commented", outcomes[0].Result);
    }

    /// <summary>
    /// ⚠️ The dispatcher swallows handler exceptions, so it returns cleanly even when every handler blew up.
    /// The status has to come from the outcomes, or a delivery where nothing worked reports Succeeded.
    /// </summary>
    [Fact]
    public async Task HandlerFailure_IsFailedAndNotRetried()
    {
        var harness = new Harness();
        harness.Dispatcher.Returns(
            new WebhookHandlerOutcome("FirstHandler", 0, "action performed: commented", null, 12),
            new WebhookHandlerOutcome("SecondHandler", 1, null, "System.Exception: boom", 3));

        var id = await harness.QueueAsync("pull_request");
        await harness.Processor.ProcessNextAsync();

        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Failed, delivery.Status);

        // A handler that threw may already have posted a comment or merged a pull request. Re-running it is a
        // decision for a human, so nothing is scheduled.
        Assert.Null(delivery.NextAttemptAt);
        Assert.Equal(1, delivery.FailedHandlerCount);
        Assert.Equal(2, delivery.HandlerCount);
    }

    /// <summary>
    /// A failure before the dispatcher ran cannot have touched GitHub, so it is the one case worth retrying
    /// on its own.
    /// </summary>
    [Fact]
    public async Task SetupFailure_IsRetriedWithBackoff()
    {
        var harness = new Harness();
        harness.TokenProvider
            .Setup(p => p.GetForCourseAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("api.github.com is unreachable"));

        var id = await harness.QueueAsync("pull_request");
        await harness.Processor.ProcessNextAsync();

        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(harness.Now.AddMinutes(1), delivery.NextAttemptAt);
        Assert.Contains("retrying", delivery.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetupFailure_GivesUpAfterMaxAttempts()
    {
        var harness = new Harness();
        harness.TokenProvider
            .Setup(p => p.GetForCourseAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("api.github.com is unreachable"));

        var id = await harness.QueueAsync("pull_request");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            Assert.True(await harness.Processor.ProcessNextAsync());
            harness.Clock.Advance(TimeSpan.FromMinutes(30));
        }

        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Failed, delivery.Status);
        Assert.Equal(3, delivery.AttemptCount);
        Assert.Contains("Giving up", delivery.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// A null token means "not configured" — the provider throws on a transport failure — so retrying would
    /// only fail identically. The legacy message is kept because administrators recognise it.
    /// </summary>
    [Fact]
    public async Task MissingGitHubCredentials_IsFailedWithoutRetry()
    {
        var harness = new Harness();
        harness.TokenProvider
            .Setup(p => p.GetForCourseAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitHubInstallationToken?)null);

        var id = await harness.QueueAsync("pull_request");
        await harness.Processor.ProcessNextAsync();

        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Failed, delivery.Status);
        Assert.Null(delivery.NextAttemptAt);
        Assert.Equal("GitHub App ID/Token not configured", delivery.Error);
    }

    /// <summary>
    /// The configuration is re-read at processing time, not snapshotted at accept time: an administrator who
    /// turned the integration off in between meant it.
    /// </summary>
    [Fact]
    public async Task IntegrationTurnedOffAfterAccepting_IsSkipped()
    {
        var harness = new Harness();
        var id = await harness.QueueAsync("pull_request");

        await harness.SetIntegrationEnabledAsync(false);
        await harness.Processor.ProcessNextAsync();

        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Skipped, delivery.Status);
        Assert.Contains("turned off", delivery.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// After an outage the queue holds a backlog. Warning students about pull requests they opened last week
    /// is worse than staying quiet, so anything past the age limit is dropped rather than run.
    /// </summary>
    [Fact]
    public async Task DeliveryOlderThanTheAgeLimit_IsSkipped()
    {
        var harness = new Harness();
        var id = await harness.QueueAsync("pull_request");

        harness.Clock.Advance(TimeSpan.FromHours(7));
        await harness.Processor.ProcessNextAsync();

        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Skipped, delivery.Status);
        Assert.Contains("older than", delivery.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// GitHub sends <c>ping</c> the moment a webhook is configured. Deciding that nobody handles it must come
    /// *before* the installation token is minted, or a course whose App credentials are not set up yet records
    /// every ping as a credentials failure — a delivery log full of red for an event that was never going to
    /// do anything, which is exactly what teaches administrators to stop reading it.
    /// </summary>
    [Fact]
    public async Task EventNobodyHandles_IsSkippedWithoutMintingAToken()
    {
        var harness = new Harness();
        harness.Dispatcher.Returns();

        var id = await harness.QueueAsync("ping");
        await harness.Processor.ProcessNextAsync();

        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Skipped, delivery.Status);
        Assert.Equal("Event ping is not of interest", delivery.Error);

        harness.TokenProvider.Verify(
            p => p.GetForCourseAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PrunedPayload_IsFailedRatherThanDispatchedEmpty()
    {
        var harness = new Harness();
        var id = await harness.QueueAsync("pull_request");
        await harness.ClearPayloadAsync(id);

        await harness.Processor.ProcessNextAsync();

        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Failed, delivery.Status);
        Assert.Contains("no longer retained", delivery.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// The context handed to the handlers must be rebuilt from the stored row and the <em>current</em>
    /// configuration — this is the whole of what moving off the request thread had to reproduce.
    /// </summary>
    [Fact]
    public async Task ContextIsRebuiltFromTheStoredDelivery()
    {
        var harness = new Harness();
        var id = await harness.QueueAsync("pull_request", deliveryId: "delivery-42");
        await harness.SetWorkflowRunThresholdAsync(9);

        await harness.Processor.ProcessNextAsync();

        var context = Assert.Single(harness.Dispatcher.Seen);
        Assert.Equal(CourseId, context.CourseId);
        Assert.Equal("pull_request", context.GitHubEventName);
        Assert.Equal("delivery-42", context.DeliveryId);
        Assert.Equal(Harness.Payload, context.RequestBody);
        Assert.Equal(9, context.WorkflowRunThreshold);

        _ = await harness.ReloadAsync(id);
    }

    /// <summary>
    /// GitHub always sends X-GitHub-Delivery, but curl and the odd proxy do not. The synthetic id keeps the
    /// handlers' redelivery guard working, and — unlike the empty string the controller used to store — does
    /// not collide with the next header-less delivery on the filtered unique index.
    /// </summary>
    [Fact]
    public async Task MissingDeliveryHeader_GetsAStableSyntheticId()
    {
        var harness = new Harness();
        var id = await harness.QueueAsync("pull_request", deliveryId: null);

        await harness.Processor.ProcessNextAsync();

        Assert.Equal($"queue-{id}", Assert.Single(harness.Dispatcher.Seen).DeliveryId);
    }

    /// <summary>
    /// An administrator re-running a failed delivery must not make the handlers that already worked act a
    /// second time — there is no way to un-post a comment or un-merge a pull request. The skip-set is derived
    /// from what the row already records, so the re-run endpoint needs no flag of its own.
    /// </summary>
    [Fact]
    public async Task ReRunSkipsHandlersThatAlreadySucceeded()
    {
        var harness = new Harness();
        var id = await harness.QueueAsync("pull_request");
        await harness.SetOutcomesAsync(
            id,
            new WebhookHandlerOutcome("FirstHandler", 0, "action performed: commented", null, 12),
            new WebhookHandlerOutcome("SecondHandler", 1, null, "System.Exception: boom", 3));

        harness.Dispatcher.Returns(new WebhookHandlerOutcome("SecondHandler", 1, "action performed: retried", null, 5));
        await harness.Processor.ProcessNextAsync();

        Assert.Equal(new[] { "FirstHandler" }, harness.Dispatcher.SkippedSeen!.Order());

        // ⚠️ The earlier success is carried forward rather than overwritten. Drop it and a *second* re-run
        // would find no record of FirstHandler and run it again — the exact duplicate this guards against.
        var delivery = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Succeeded, delivery.Status);

        var outcomes = Harness.OutcomesOf(delivery);
        Assert.Equal(new[] { "FirstHandler", "SecondHandler" }, outcomes.Select(o => o.HandlerName));
        Assert.Equal("action performed: commented", outcomes[0].Result);
        Assert.Equal("action performed: retried", outcomes[1].Result);
    }

    /// <summary>
    /// A row still marked Processing at startup can only be the wreckage of a killed run: one worker, one
    /// process. It is recorded as such and never resumed — the handlers that already ran did so for real.
    /// </summary>
    [Fact]
    public async Task StartupSweep_MarksStrandedDeliveriesInterrupted()
    {
        var harness = new Harness();
        var id = await harness.QueueAsync("pull_request");
        var delivery = await harness.ReloadAsync(id);
        delivery.Status = GitHubWebhookDeliveryStatus.Processing;
        await harness.Db.SaveChangesAsync();

        Assert.Equal(1, await harness.Processor.SweepInterruptedAsync());

        var swept = await harness.ReloadAsync(id);
        Assert.Equal(GitHubWebhookDeliveryStatus.Interrupted, swept.Status);
        Assert.NotNull(swept.CompletedAt);
    }

    [Fact]
    public async Task EmptyQueue_ReportsNothingProcessed()
    {
        var harness = new Harness();
        Assert.False(await harness.Processor.ProcessNextAsync());
    }

    /// <summary>The queue is FIFO by insertion order, which is what gives per-repository ordering for free.</summary>
    [Fact]
    public async Task DeliveriesAreProcessedInArrivalOrder()
    {
        var harness = new Harness();
        await harness.QueueAsync("pull_request", deliveryId: "first");
        await harness.QueueAsync("pull_request", deliveryId: "second");

        await harness.Processor.ProcessNextAsync();
        await harness.Processor.ProcessNextAsync();

        Assert.Equal(new[] { "first", "second" }, harness.Dispatcher.Seen.Select(c => c.DeliveryId));
    }

    [Fact]
    public async Task ADeliveryWaitingForItsRetryTimeIsNotClaimed()
    {
        var harness = new Harness();
        var id = await harness.QueueAsync("pull_request");
        var delivery = await harness.ReloadAsync(id);
        delivery.NextAttemptAt = harness.Now.AddMinutes(5);
        await harness.Db.SaveChangesAsync();

        Assert.False(await harness.Processor.ProcessNextAsync());

        harness.Clock.Advance(TimeSpan.FromMinutes(6));
        Assert.True(await harness.Processor.ProcessNextAsync());
    }

    /// <summary>
    /// Retention drops payloads first and rows later, and never touches work that has not run — the payload of
    /// a Pending row is the work itself.
    /// </summary>
    [Fact]
    public async Task Retention_DropsOldPayloadsAndKeepsUnprocessedWork()
    {
        var harness = new Harness();
        var stale = await harness.QueueAsync("pull_request", deliveryId: "stale");
        var waiting = await harness.QueueAsync("pull_request", deliveryId: "waiting");

        var staleRow = await harness.ReloadAsync(stale);
        staleRow.Status = GitHubWebhookDeliveryStatus.Succeeded;
        staleRow.OutcomesJson = "[]";
        await harness.Db.SaveChangesAsync();

        harness.Clock.Advance(TimeSpan.FromDays(20));
        await harness.Processor.RunRetentionAsync();

        var afterPayloadPass = await harness.ReloadAsync(stale);
        Assert.Null(afterPayloadPass.Payload);
        Assert.Equal("[]", afterPayloadPass.OutcomesJson);
        Assert.NotNull((await harness.ReloadAsync(waiting)).Payload);

        harness.Clock.Advance(TimeSpan.FromDays(100));
        await harness.Processor.RunRetentionAsync();

        Assert.Empty(harness.Db.GitHubWebhookDeliveries.Where(d => d.DeliveryId == "stale"));
        Assert.Single(harness.Db.GitHubWebhookDeliveries.Where(d => d.DeliveryId == "waiting"));
    }

    /// <summary>Everything one of these tests needs, wired over EF InMemory and a movable clock.</summary>
    private sealed class Harness
    {
        public const string Payload = """{"repository":{"full_name":"bmeaut/viaubc01-abc123"}}""";

        public Harness()
        {
            Clock = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"processor-{Guid.NewGuid()}")
                .Options;

            // No course is ever resolved on this path, by design — the processor reads with explicit ids.
            Db = new ApplicationDbContext(options, new StubCurrentCourseProvider());

            Db.Courses.Add(new Course
            {
                Id = CourseId,
                Slug = "viaubc01",
                Name = "Course",
                GitHubOrganization = "bmeaut",
                RepoNamePrefix = "viaubc01",
                GitHubConfig = new CourseGitHubConfig { Enabled = true, WorkflowRunThreshold = 5, GitHubWebhookSecret = "s" },
            });
            Db.SaveChanges();

            TokenProvider = new Mock<ICourseGitHubAppTokenProvider>();
            TokenProvider
                .Setup(p => p.GetForCourseAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GitHubInstallationToken("installation-token", 1, new Dictionary<string, string>(), "all"));

            var clientFactory = new Mock<ICourseGitHubClientFactory>();
            clientFactory.Setup(f => f.CreateForToken(It.IsAny<string>())).Returns(Mock.Of<IGitHubClient>());

            Processor = new GitHubWebhookDeliveryProcessor(
                Db,
                TokenProvider.Object,
                clientFactory.Object,
                Dispatcher,
                Clock,
                Options.Create(new WebhookOptions()),
                NullLogger<GitHubWebhookDeliveryProcessor>.Instance);
        }

        public FakeTimeProvider Clock { get; }

        public DateTimeOffset Now => Clock.GetUtcNow();

        public ApplicationDbContext Db { get; }

        public RecordingDispatcher Dispatcher { get; } = new();

        public Mock<ICourseGitHubAppTokenProvider> TokenProvider { get; }

        public GitHubWebhookDeliveryProcessor Processor { get; }

        public async Task<int> QueueAsync(string eventName, string? deliveryId = "delivery-1")
        {
            var delivery = new GitHubWebhookDelivery
            {
                CourseId = CourseId,
                DeliveryId = deliveryId,
                EventName = eventName,
                RepositoryFullName = "bmeaut/viaubc01-abc123",
                Payload = Payload,
                ReceivedAt = Now,
                Status = GitHubWebhookDeliveryStatus.Pending,
                NextAttemptAt = Now,
            };

            Db.GitHubWebhookDeliveries.Add(delivery);
            await Db.SaveChangesAsync();
            return delivery.Id;
        }

        public async Task<GitHubWebhookDelivery> ReloadAsync(int id)
        {
            var delivery = await Db.GitHubWebhookDeliveries.SingleAsync(d => d.Id == id);
            await Db.Entry(delivery).ReloadAsync();
            return delivery;
        }

        public static IReadOnlyList<WebhookHandlerOutcome> OutcomesOf(GitHubWebhookDelivery delivery) =>
            System.Text.Json.JsonSerializer.Deserialize<List<WebhookHandlerOutcome>>(delivery.OutcomesJson!)!;

        public async Task SetIntegrationEnabledAsync(bool enabled)
        {
            var config = await Db.CourseGitHubConfigs.SingleAsync(g => g.CourseId == CourseId);
            config.Enabled = enabled;
            await Db.SaveChangesAsync();
        }

        public async Task SetWorkflowRunThresholdAsync(int threshold)
        {
            var config = await Db.CourseGitHubConfigs.SingleAsync(g => g.CourseId == CourseId);
            config.WorkflowRunThreshold = threshold;
            await Db.SaveChangesAsync();
        }

        public async Task SetOutcomesAsync(int id, params WebhookHandlerOutcome[] outcomes)
        {
            var delivery = await Db.GitHubWebhookDeliveries.SingleAsync(d => d.Id == id);
            delivery.OutcomesJson = System.Text.Json.JsonSerializer.Serialize(outcomes);
            delivery.HandlerCount = outcomes.Length;
            delivery.FailedHandlerCount = outcomes.Count(o => !o.Succeeded);
            await Db.SaveChangesAsync();
        }

        public async Task ClearPayloadAsync(int id)
        {
            var delivery = await Db.GitHubWebhookDeliveries.SingleAsync(d => d.Id == id);
            delivery.Payload = null;
            await Db.SaveChangesAsync();
        }
    }

    /// <summary>Records the contexts it is given and replays a canned set of outcomes.</summary>
    private sealed class RecordingDispatcher : IGitHubWebhookDispatcher
    {
        private WebhookHandlerOutcome[] outcomes =
        {
            new("FirstHandler", 0, "action performed: did the thing", null, 7),
        };

        public List<GitHubWebhookContext> Seen { get; } = new();

        public IReadOnlySet<string>? SkippedSeen { get; private set; }

        public void Returns(params WebhookHandlerOutcome[] planned) => outcomes = planned;

        /// <summary>"No planned outcomes" stands in for "no handler subscribes to this event".</summary>
        public bool HasHandlersFor(string gitHubEventName) => outcomes.Length > 0;

        public async Task<IReadOnlyList<WebhookHandlerOutcome>> ProcessAsync(
            GitHubWebhookContext context,
            Func<IReadOnlyList<WebhookHandlerOutcome>, CancellationToken, Task>? onProgress = null,
            IReadOnlySet<string>? skipHandlers = null,
            CancellationToken cancellationToken = default)
        {
            Seen.Add(context);
            SkippedSeen = skipHandlers;

            var produced = new List<WebhookHandlerOutcome>();
            foreach (var outcome in outcomes)
            {
                produced.Add(outcome);
                if (onProgress is not null)
                    await onProgress(produced, cancellationToken);
            }

            return produced;
        }
    }

    private sealed class StubCurrentCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId => null;
    }
}
