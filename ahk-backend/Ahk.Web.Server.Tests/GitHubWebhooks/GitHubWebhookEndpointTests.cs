using System.Diagnostics;
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
/// The webhook endpoint's response contract, end to end through the real pipeline.
///
/// Every status here is read by a human in the GitHub App's <em>Advanced → Recent Deliveries</em> tab, so each
/// one is part of the diagnostic surface rather than an implementation detail — which is why they are pinned
/// individually, error strings included.
/// </summary>
public class GitHubWebhookEndpointTests : IClassFixture<GitHubWebhookEndpointTests.WebhookAppFactory>
{
    private const string Secret = "dev-webhook-secret";
    private const string Url = "/api/integrations/github";

    private readonly WebhookAppFactory factory;

    public GitHubWebhookEndpointTests(WebhookAppFactory factory) => this.factory = factory;

    [Fact]
    public async Task MissingEventHeader_Returns400()
    {
        var response = await PostAsync(Body("bmeaut/viaubc01-abc123"), eventName: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("X-GitHub-Event header missing", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingSignatureHeader_Returns400()
    {
        var response = await PostAsync(Body("bmeaut/viaubc01-abc123"), signature: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("X-Hub-Signature-256 header missing", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>The pre-parse must reject anything it cannot route, without ever throwing on hostile input.</summary>
    [Theory]
    [InlineData("not json at all")]
    [InlineData("{}")]
    [InlineData("{\"repository\":null}")]
    [InlineData("{\"repository\":{}}")]
    [InlineData("[]")]
    public async Task BodyWithoutRepositoryName_Returns400(string body)
    {
        var response = await PostAsync(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("no repository information in webhook payload", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>
    /// 202, not 4xx. An organization contains repositories that are not a course, and a delivery log full of
    /// red teaches administrators to ignore it.
    /// </summary>
    [Fact]
    public async Task RepositoryInNoCourse_Returns202()
    {
        var response = await PostAsync(Body("someoneelse/unrelated-repo"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Contains("not mapped to a course", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CourseWithIntegrationTurnedOff_Returns202()
    {
        var response = await PostAsync(Body("bmeaut/paused-abc123"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Contains("turned off", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>
    /// A course with no secret is a misconfiguration an administrator has to fix, not a delivery to shrug off —
    /// so it keeps the legacy 500 rather than joining the 202 cases.
    /// </summary>
    [Fact]
    public async Task CourseWithoutWebhookSecret_Returns500()
    {
        var response = await PostAsync(Body("bmeaut/nosecret-abc123"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("GitHub secret not configured", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task WrongSignature_Returns400()
    {
        var response = await PostAsync(Body("bmeaut/viaubc01-abc123"), signature: "sha256=deadbeef");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Payload signature not valid", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>
    /// A body signed with a *different* course's secret must be indistinguishable from any other bad
    /// signature. This is what contains the parse-before-verify ordering: guessing a repository name buys an
    /// attacker nothing.
    /// </summary>
    [Fact]
    public async Task BodySignedWithAnotherCoursesSecret_Returns400()
    {
        var body = Body("bmeaut/viaubc01-abc123");
        var response = await PostAsync(body, signature: Sign(body, "a-different-courses-secret"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Payload signature not valid", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>
    /// A correctly signed delivery is accepted, not processed. 202 rather than 200 because that is now the
    /// literal truth — and because GitHub colours any non-2xx red, which the three "nothing to do" branches
    /// above already rely on.
    /// </summary>
    [Fact]
    public async Task ValidDeliveryForUnhandledEvent_Returns202AndQueues()
    {
        var response = await PostAsync(Body("bmeaut/viaubc01-abc123"), eventName: "ping", deliveryId: "delivery-ping");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Contains("queued", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        Assert.NotNull(await FindDeliveryAsync("delivery-ping"));
    }

    /// <summary>
    /// The queued row must carry the body byte for byte: the handlers deserialize it again later, and a
    /// delivery that survives the hand-off in a mangled form is worse than one that never arrived.
    /// </summary>
    [Fact]
    public async Task ValidDelivery_PersistsTheDeliveryVerbatim()
    {
        var body = Body("bmeaut/viaubc01-abc123");

        var response = await PostAsync(body, eventName: "pull_request", deliveryId: "delivery-42");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var delivery = await FindDeliveryAsync("delivery-42");
        Assert.NotNull(delivery);
        Assert.Equal(body, delivery.Payload);
        Assert.Equal("pull_request", delivery.EventName);
        Assert.Equal("bmeaut/viaubc01-abc123", delivery.RepositoryFullName);
        Assert.Equal(GitHubWebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.AttemptCount);
        Assert.NotEqual(0, delivery.CourseId);
    }

    /// <summary>
    /// The point of the change, stated as an absence rather than a stopwatch: the accept path mints no
    /// installation token, builds no GitHub client and runs no handler. The factory registers those three as
    /// strict mocks with no setups and a dispatcher that throws, so reaching any of them fails the test
    /// deterministically — where a wall-clock assertion would only flake on a slow CI agent.
    /// </summary>
    [Fact]
    public async Task ValidDelivery_DoesNoGitHubWorkOnTheRequestThread()
    {
        var started = Stopwatch.StartNew();

        var response = await PostAsync(Body("bmeaut/viaubc01-abc123"), eventName: "pull_request", deliveryId: "delivery-fast");

        started.Stop();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Generous by an order of magnitude against GitHub's ten seconds; this is a smoke check, the real
        // assertion is that the strict mocks above were never touched.
        Assert.True(started.Elapsed < TimeSpan.FromSeconds(2), $"accept took {started.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// The endpoint is anonymous by design — the HMAC is the authentication. A 401 here would mean a fallback
    /// authorization policy had crept into Program.cs and silently broken every delivery.
    /// </summary>
    [Fact]
    public async Task WithoutCredentials_IsNotUnauthorized()
    {
        var response = await PostAsync("not json at all");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string Body(string repositoryFullName)
        => $"{{\"repository\":{{\"full_name\":\"{repositoryFullName}\"}}}}";

    private async Task<GitHubWebhookDelivery?> FindDeliveryAsync(string deliveryId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.GitHubWebhookDeliveries.AsNoTracking().SingleOrDefaultAsync(d => d.DeliveryId == deliveryId);
    }

    /// <summary>
    /// Signs the way GitHub does, implemented here rather than reused from the validator so the endpoint tests
    /// do not merely agree with the code they are testing.
    /// </summary>
    private static string Sign(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return "sha256=" + Convert.ToHexString(hash).ToLower(CultureInfo.InvariantCulture);
    }

    private async Task<HttpResponseMessage> PostAsync(
        string body, string? eventName = "push", string? signature = "", string? deliveryId = "delivery-1")
    {
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        if (eventName is not null)
            request.Headers.Add("X-GitHub-Event", eventName);

        if (deliveryId is not null)
            request.Headers.Add("X-GitHub-Delivery", deliveryId);

        // The empty-string default means "sign it correctly"; null means "omit the header entirely".
        if (signature is not null)
            request.Headers.Add("X-Hub-Signature-256", signature.Length == 0 ? Sign(body, Secret) : signature);

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Refuses to run. The accept path must never reach a handler, and a dispatcher that throws says so
    /// louder than an assertion after the fact.
    /// </summary>
    private sealed class ExplodingDispatcher : IGitHubWebhookDispatcher
    {
        public bool HasHandlersFor(string gitHubEventName) => true;

        public Task<IReadOnlyList<WebhookHandlerOutcome>> ProcessAsync(
            GitHubWebhookContext context,
            Func<IReadOnlyList<WebhookHandlerOutcome>, CancellationToken, Task>? onProgress = null,
            IReadOnlySet<string>? skipHandlers = null,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("The webhook accept path must not dispatch handlers.");
    }

    public sealed class WebhookAppFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.WithoutWebhookWorker();
            builder.WithoutHealthRefreshWorker();
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

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("GitHubWebhookEndpointTests"));

                // Strict, and deliberately without a single setup: the accept path is supposed to mint no
                // token, build no client and run no handler, so any call here fails the test outright rather
                // than being quietly satisfied. It is also what keeps api.github.com out of a test run.
                services.AddSingleton<IGitHubWebhookDispatcher>(new ExplodingDispatcher());
                services.AddSingleton(new Mock<ICourseGitHubAppTokenProvider>(MockBehavior.Strict).Object);
                services.AddSingleton(new Mock<ICourseGitHubClientFactory>(MockBehavior.Strict).Object);
            });

            var host = base.CreateHost(builder);

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!db.Courses.IgnoreQueryFilters().Any())
                {
                    // Three courses in one organization, so repo-prefix resolution is exercised rather than
                    // assumed, and each of the configuration failure modes has somewhere to happen.
                    db.Courses.AddRange(
                        new Course
                        {
                            Slug = "viaubc01",
                            Name = "Healthy course",
                            GitHubOrganization = "bmeaut",
                            RepoNamePrefix = "viaubc01",
                            GitHubConfig = new CourseGitHubConfig { GitHubWebhookSecret = Secret, WorkflowRunThreshold = 7, Enabled = true },
                        },
                        new Course
                        {
                            Slug = "paused",
                            Name = "Integration turned off",
                            GitHubOrganization = "bmeaut",
                            RepoNamePrefix = "paused",
                            GitHubConfig = new CourseGitHubConfig { GitHubWebhookSecret = Secret, Enabled = false },
                        },
                        new Course
                        {
                            Slug = "nosecret",
                            Name = "No webhook secret stored",
                            GitHubOrganization = "bmeaut",
                            RepoNamePrefix = "nosecret",
                            GitHubConfig = new CourseGitHubConfig { GitHubWebhookSecret = null, Enabled = true },
                        });
                    db.SaveChanges();
                }
            }

            return host;
        }
    }
}
