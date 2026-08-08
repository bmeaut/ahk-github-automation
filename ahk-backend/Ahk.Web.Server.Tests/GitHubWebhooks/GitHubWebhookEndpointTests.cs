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

    /// <summary>A correctly signed delivery for an event nobody handles is a 200 that says so.</summary>
    [Fact]
    public async Task ValidDeliveryForUnhandledEvent_Returns200WithResult()
    {
        var response = await PostAsync(Body("bmeaut/viaubc01-abc123"), eventName: "ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Event ping is not of interest", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>
    /// The dispatcher must receive the resolved course and the per-course run threshold — the two things the
    /// port had to add, and the two a handler cannot obtain for itself.
    /// </summary>
    [Fact]
    public async Task ValidDelivery_PassesCourseAndThresholdToDispatcher()
    {
        factory.Dispatcher.Reset();

        var response = await PostAsync(Body("bmeaut/viaubc01-abc123"), eventName: "pull_request", deliveryId: "delivery-42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var context = Assert.Single(factory.Dispatcher.Seen);
        Assert.Equal("pull_request", context.GitHubEventName);
        Assert.Equal("delivery-42", context.DeliveryId);
        Assert.Equal(7, context.WorkflowRunThreshold);
        Assert.NotEqual(0, context.CourseId);
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

    /// <summary>Records what the endpoint handed it, so the handlers themselves stay out of these tests.</summary>
    public sealed class RecordingDispatcher : IGitHubWebhookDispatcher
    {
        public List<GitHubWebhookContext> Seen { get; } = new();

        public void Reset() => Seen.Clear();

        public Task ProcessAsync(GitHubWebhookContext context, WebhookResult result, CancellationToken cancellationToken = default)
        {
            Seen.Add(context);
            result.LogInfo($"Event {context.GitHubEventName} is not of interest");
            return Task.CompletedTask;
        }
    }

    public sealed class WebhookAppFactory : WebApplicationFactory<Program>
    {
        public RecordingDispatcher Dispatcher { get; } = new();

        protected override IHost CreateHost(IHostBuilder builder)
        {
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

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("GitHubWebhookEndpointTests"));
                services.AddSingleton<IGitHubWebhookDispatcher>(Dispatcher);

                // Nothing may reach api.github.com from a test.
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
