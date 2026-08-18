using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.GitHubWebhooks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Octokit;
using Xunit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// The one test that proves the two halves are actually connected: a signed delivery posted to the endpoint
/// is answered 202 and then, without anything else prodding it, reaches the handlers and ends up recorded.
///
/// The only test host that leaves <c>GitHubWebhookDeliveryWorker</c> switched on.
/// </summary>
public class GitHubWebhookDeliveryDrainTests : IClassFixture<GitHubWebhookDeliveryDrainTests.DrainAppFactory>
{
    private const string Secret = "dev-webhook-secret";

    private readonly DrainAppFactory factory;

    public GitHubWebhookDeliveryDrainTests(DrainAppFactory factory) => this.factory = factory;

    [Fact]
    public async Task AcceptedDelivery_IsDrainedByTheWorker()
    {
        var body = """{"repository":{"full_name":"bmeaut/viaubc01-abc123"}}""";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/integrations/github")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-GitHub-Event", "pull_request");
        request.Headers.Add("X-GitHub-Delivery", "drain-1");
        request.Headers.Add("X-Hub-Signature-256", Sign(body, Secret));

        var response = await factory.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Event-driven, never a sleep: the dispatcher completes this the moment the worker reaches it.
        var context = await factory.Dispatcher.Dispatched.Task.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal("pull_request", context.GitHubEventName);
        Assert.Equal("drain-1", context.DeliveryId);
        Assert.Equal(body, context.RequestBody);

        // The worker writes the terminal state after the dispatcher returns, so poll briefly for it rather
        // than racing the same signal that released this test.
        var delivery = await WaitForTerminalAsync("drain-1");
        Assert.Equal(GitHubWebhookDeliveryStatus.Succeeded, delivery.Status);
        Assert.Equal(1, delivery.HandlerCount);
        Assert.Equal(0, delivery.FailedHandlerCount);
    }

    private static string Sign(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return "sha256=" + Convert.ToHexString(hash).ToLower(CultureInfo.InvariantCulture);
    }

    private async Task<GitHubWebhookDelivery> WaitForTerminalAsync(string deliveryId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);

        while (DateTime.UtcNow < deadline)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var delivery = await db.GitHubWebhookDeliveries.AsNoTracking().SingleOrDefaultAsync(d => d.DeliveryId == deliveryId);

            if (delivery is not null && delivery.IsTerminal)
                return delivery;

            await Task.Delay(50);
        }

        throw new TimeoutException($"Delivery '{deliveryId}' never reached a terminal state.");
    }

    /// <summary>Completes a task the moment the worker dispatches, so the test needs no polling to start.</summary>
    public sealed class SignallingDispatcher : IGitHubWebhookDispatcher
    {
        public TaskCompletionSource<GitHubWebhookContext> Dispatched { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool HasHandlersFor(string gitHubEventName) => true;

        public async Task<IReadOnlyList<WebhookHandlerOutcome>> ProcessAsync(
            GitHubWebhookContext context,
            Func<IReadOnlyList<WebhookHandlerOutcome>, CancellationToken, Task>? onProgress = null,
            IReadOnlySet<string>? skipHandlers = null,
            CancellationToken cancellationToken = default)
        {
            var outcomes = new List<WebhookHandlerOutcome>
            {
                new("StubHandler", 0, "action performed: stub", null, 1),
            };

            if (onProgress is not null)
                await onProgress(outcomes, cancellationToken);

            Dispatched.TrySetResult(context);
            return outcomes;
        }
    }

    public sealed class DrainAppFactory : WebApplicationFactory<Program>
    {
        public SignallingDispatcher Dispatcher { get; } = new();

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Deliberately NOT calling WithoutWebhookWorker(): the worker running is the thing under test.
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType == typeof(IGitHubWebhookDispatcher) ||
                    d.ServiceType == typeof(ICourseGitHubAppTokenProvider) ||
                    d.ServiceType == typeof(ICourseGitHubClientFactory) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)))
                    .ToList();
                foreach (var descriptor in toRemove)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("GitHubWebhookDrainTests"));
                services.AddSingleton<IGitHubWebhookDispatcher>(Dispatcher);

                // The worker mints a token and builds a client for real; both are stubbed so no test reaches
                // api.github.com.
                var tokenProvider = new Mock<ICourseGitHubAppTokenProvider>();
                tokenProvider
                    .Setup(p => p.GetForCourseAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new GitHubInstallationToken("installation-token", 1, new Dictionary<string, string>(), "all"));
                services.AddSingleton(tokenProvider.Object);

                var clientFactory = new Mock<ICourseGitHubClientFactory>();
                clientFactory.Setup(f => f.CreateForToken(It.IsAny<string>())).Returns(Mock.Of<IGitHubClient>());
                services.AddSingleton(clientFactory.Object);
            });

            var host = base.CreateHost(builder);

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!db.Courses.IgnoreQueryFilters().Any())
                {
                    db.Courses.Add(new Course
                    {
                        Slug = "viaubc01",
                        Name = "Healthy course",
                        GitHubOrganization = "bmeaut",
                        RepoNamePrefix = "viaubc01",
                        GitHubConfig = new CourseGitHubConfig { GitHubWebhookSecret = Secret, WorkflowRunThreshold = 7, Enabled = true },
                    });
                    db.SaveChanges();
                }
            }

            return host;
        }
    }
}
