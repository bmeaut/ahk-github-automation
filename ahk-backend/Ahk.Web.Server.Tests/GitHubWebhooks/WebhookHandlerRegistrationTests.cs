using Ahk.Web.Services;
using Ahk.Web.Services.GitHubWebhooks;
using Ahk.Web.Services.GitHubWebhooks.Handlers;
using Ahk.Web.Services.GitHubWebhooks.Handlers.GradeComment;
using Ahk.Web.Services.GitHubWebhooks.Handlers.StatusTracking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// Guards the shape of the handler set itself, without running a delivery.
/// </summary>
public class WebhookHandlerRegistrationTests
{
    /// <summary>
    /// ⚠️ The invariant behind <c>SubmissionEvent.GitHubDeliveryId</c>.
    ///
    /// The column is globally unique so a redelivered webhook does not duplicate rows, but one delivery fans
    /// out to several handlers. That only works while <strong>at most one handler per event name writes a
    /// status event</strong>. Add a second writer for an existing event and its rows silently vanish (the
    /// redelivery guard swallows them) or the unique index rejects them on SQL Server — either way, in
    /// production, not here. So the invariant is a failing build instead.
    ///
    /// If you genuinely need two writers for one event, key the delivery id per handler first.
    /// </summary>
    [Fact]
    public void AtMostOneStatusEventWriterPerEvent()
    {
        var offenders = ResolveHandlers()
            .Where(h => h is IStatusEventWriter)
            .GroupBy(h => h.GitHubEventName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(h => h.GetType().Name))}")
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"More than one handler writes a status event for the same GitHub event, which breaks the delivery-id "
            + $"redelivery guard: {string.Join("; ", offenders)}");
    }

    /// <summary>
    /// The exact handler set ported from github-monitor. Pinned so that dropping a registration — which costs
    /// nothing at compile time and shows up only as a rule that quietly stopped being enforced — fails here.
    /// </summary>
    [Fact]
    public void AllPortedHandlersAreRegistered()
    {
        var registered = ResolveHandlers()
            .Select(h => (h.GetType(), h.GitHubEventName))
            .ToHashSet();

        var expected = new HashSet<(Type, string)>
        {
            (typeof(BranchProtectionRuleHandler), "create"),
            (typeof(IssueCommentEditDeleteHandler), "issue_comment"),
            (typeof(PullRequestOpenDuplicateHandler), "pull_request"),
            (typeof(PullRequestReviewToAssigneeHandler), "pull_request"),
            (typeof(GradeCommandIssueCommentHandler), "issue_comment"),
            (typeof(GradeCommandReviewCommentHandler), "pull_request_review"),
            (typeof(ActionWorkflowRunHandler), "workflow_run"),
            (typeof(BranchCreateStatusTrackingHandler), "create"),
            (typeof(WorkflowRunStatusTrackingHandler), "workflow_run"),
            (typeof(PullRequestStatusTrackingHandler), "pull_request"),
        };

        Assert.Equal(expected, registered);
    }

    /// <summary>
    /// Handlers post comments, and the order they appear in under a pull request is the order they are
    /// registered in. DI returns an <c>IEnumerable&lt;T&gt;</c> in registration order, which is the whole
    /// reason github-monitor's explicit dispatch-config builder was not needed in the port — so that property
    /// is worth a test of its own.
    /// </summary>
    [Fact]
    public void RegistrationOrderMatchesTheOriginal()
    {
        var pullRequestHandlers = ResolveHandlers()
            .Where(h => h.GitHubEventName == "pull_request")
            .Select(h => h.GetType())
            .ToList();

        Assert.Equal(
            new[]
            {
                typeof(PullRequestOpenDuplicateHandler),
                typeof(PullRequestReviewToAssigneeHandler),
                typeof(PullRequestStatusTrackingHandler),
            },
            pullRequestHandlers);
    }

    /// <summary>
    /// ⚠️ The dispatcher records a handler by its simple type name, and an administrator re-running a failed
    /// delivery keys the "already succeeded, do not run again" set on exactly that string. Widen it to a full
    /// name, or rename a handler class, and the skip-set silently stops matching — so a re-run posts a second
    /// comment or attempts a second merge, which is the one thing the re-run exists to avoid.
    /// </summary>
    [Fact]
    public async Task DispatcherRecordsHandlersByTheirSimpleTypeName()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IGitHubWebhookHandler, StubHandler>();
        services.AddScoped<IGitHubWebhookDispatcher, GitHubWebhookDispatcher>();

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IGitHubWebhookDispatcher>();

        var outcomes = await dispatcher.ProcessAsync(new GitHubWebhookContext
        {
            CourseId = 1,
            GitHubEventName = StubHandler.EventName,
            DeliveryId = "d1",
            RequestBody = "{}",
            GitHubClient = Mock.Of<Octokit.IGitHubClient>(),
            WorkflowRunThreshold = 5,
        });

        Assert.Equal(nameof(StubHandler), Assert.Single(outcomes).HandlerName);
    }

    /// <summary>A handler whose only job is to be named, for the test above.</summary>
    private sealed class StubHandler : IGitHubWebhookHandler
    {
        public const string EventName = "stub_event";

        public string GitHubEventName => EventName;

        public Task<EventHandlerResult> ExecuteAsync(GitHubWebhookContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(EventHandlerResult.NoActionNeeded("stub"));
    }

    /// <summary>
    /// Resolves the handlers alone, with their collaborators stubbed. Registering the whole service graph would
    /// drag in the DbContext and the HTTP clients for a question that is purely about registration.
    /// </summary>
    private static IReadOnlyList<IGitHubWebhookHandler> ResolveHandlers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
        services.AddSingleton(Mock.Of<Ahk.Web.Services.StatusTracking.ISubmissionEventService>());
        services.AddSingleton(Mock.Of<Ahk.Web.Services.Grading.IGradeService>());
        services.AddAhkGitHubWebhooks();

        var provider = services.BuildServiceProvider();
        return provider.GetServices<IGitHubWebhookHandler>().ToList();
    }
}
