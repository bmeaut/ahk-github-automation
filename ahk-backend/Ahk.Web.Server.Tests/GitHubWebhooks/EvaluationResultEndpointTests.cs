using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Grading;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// The CI callback, end to end. The client on the other side of this contract is a Go binary already running
/// inside student repositories that are updated on their own schedule, so both the accepted request and every
/// rejection message are fixed by compatibility rather than by taste.
/// </summary>
public class EvaluationResultEndpointTests : IClassFixture<EvaluationResultEndpointTests.CallbackAppFactory>
{
    private const string Url = "/api/integrations/evaluation-result";
    private const string Token = "ci-token-value";
    private const string Secret = "ci-secret-value";
    private const string RevokedToken = "revoked-token-value";

    /// <summary>Exactly what publish-results-pr emits, imageFiles included.</summary>
    private const string SampleBody =
        """{"gitHubRepoName":"bmeaut/viaubc01-abc123","gitHubBranch":"refs/pull/12/merge","gitHubPullRequestNum":12,"gitHubCommitHash":"aa11cc33","neptunCode":"abc123","imageFiles":[],"result":[{"exerciseName":"ex1","taskName":"t1","points":2,"comment":"ok"},{"exerciseName":"ex1","taskName":"t2","points":3},{"exerciseName":"ex2","taskName":"t3","points":1}],"origin":"https://github.com/bmeaut/viaubc01-abc123/commit/aa11cc33"}""";

    private readonly CallbackAppFactory factory;

    public EvaluationResultEndpointTests(CallbackAppFactory factory) => this.factory = factory;

    [Fact]
    public async Task ValidRequest_RecordsAnUnconfirmedGrade()
    {
        var response = await PostAsync(SampleBody);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var grade = await db.GradeRecords.IgnoreQueryFilters()
            .Include(g => g.Points)
            .Include(g => g.Submission)
            .OrderByDescending(g => g.Id)
            .FirstAsync();

        Assert.False(grade.Confirmed);
        Assert.Equal(GradeService.AutomatedActor, grade.Actor);
        Assert.Equal("ABC123", grade.Neptun);
        Assert.Equal(12, grade.PrNumber);
        Assert.Equal("bmeaut/viaubc01-abc123", grade.Submission!.GitHubRepoName);

        // The Date header is the grade's timestamp, not the moment the server happened to handle it.
        Assert.Equal(factory.Now.UtcDateTime, grade.Date.UtcDateTime, TimeSpan.FromSeconds(1));

        // Per-task detail is discarded; points are summed per exercise and ordered by name.
        Assert.Equal(new[] { ("ex1", 5d), ("ex2", 1d) }, grade.Points.OrderBy(p => p.Order).Select(p => (p.Name, p.Point)));
    }

    /// <summary>
    /// The Go client sends <c>imageFiles</c>, which has never had a counterpart here. Tolerating unknown
    /// members is a compatibility requirement, not laxness — a strict deserializer would fail every student
    /// build at once.
    /// </summary>
    [Fact]
    public async Task UnknownMembersAreIgnored()
    {
        var body = """{"gitHubRepoName":"bmeaut/viaubc01-xyz999","neptunCode":"xyz999","imageFiles":["a.png"],"somethingBrandNew":{"nested":true},"result":[]}""";

        var response = await PostAsync(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MissingDateHeader_Returns400() =>
        await AssertBadRequestAsync(await PostAsync(SampleBody, date: null), "Date header missing");

    [Fact]
    public async Task UnparseableDateHeader_Returns400() =>
        await AssertBadRequestAsync(await PostAsync(SampleBody, date: "notadate"), "Date header value not valid RFC1123 string");

    [Theory]
    [InlineData(-25)]
    [InlineData(11)]
    public async Task DateOutsideSkewWindow_Returns400(int minutes)
    {
        var skewed = factory.Now.AddMinutes(minutes).UtcDateTime.ToString("R", CultureInfo.InvariantCulture);

        await AssertBadRequestAsync(
            await PostAsync(SampleBody, date: skewed), "Date header value is not close enough to current date");
    }

    /// <summary>Ten minutes either way is inside the window; the boundary is where clock drift actually lives.</summary>
    [Theory]
    [InlineData(-9)]
    [InlineData(9)]
    public async Task DateInsideSkewWindow_IsAccepted(int minutes)
    {
        var skewed = factory.Now.AddMinutes(minutes).UtcDateTime;
        var response = await PostAsync(SampleBody, date: skewed.ToString("R", CultureInfo.InvariantCulture));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MissingSignatureHeader_Returns400() =>
        await AssertBadRequestAsync(await PostAsync(SampleBody, signature: null), "X-Ahk-Sha256 header missing");

    [Fact]
    public async Task MissingTokenHeader_Returns400() =>
        await AssertBadRequestAsync(await PostAsync(SampleBody, token: null), "X-Ahk-Token header missing");

    [Fact]
    public async Task UnknownToken_Returns400() =>
        await AssertBadRequestAsync(await PostAsync(SampleBody, token: "no-such-token"), "X-Ahk-Token invalid");

    /// <summary>
    /// Revoking must take effect immediately. The secret lookup is cached for an hour, so this is really a test
    /// that revocation evicts the cache rather than waiting it out.
    /// </summary>
    [Fact]
    public async Task RevokedToken_Returns400() =>
        await AssertBadRequestAsync(await PostAsync(SampleBody, token: RevokedToken), "X-Ahk-Token invalid");

    [Fact]
    public async Task WrongSignature_Returns400() =>
        await AssertBadRequestAsync(await PostAsync(SampleBody, signature: "notavalidsignature="), "X-Ahk-Sha256 signature not valid");

    /// <summary>
    /// The signature covers the URL, so a request signed for a different address must fail. This is the
    /// failure mode behind a misconfigured AHK_APPURL, and the reason the documented value is byte-exact.
    /// </summary>
    [Fact]
    public async Task SignatureOverADifferentUrl_Returns400()
    {
        var date = factory.Now.UtcDateTime;
        var wrongUrlSignature = Sign("POST", "https://ahk.aut.bme.hu/api/evaluation-result", date, SampleBody, Secret);

        await AssertBadRequestAsync(await PostAsync(SampleBody, signature: wrongUrlSignature), "X-Ahk-Sha256 signature not valid");
    }

    [Fact]
    public async Task BodyThatIsNotJson_Returns400() =>
        await AssertBadRequestAsync(await PostAsync("notjson"), "Body cannot be deserialized as JSON");

    /// <summary>The two top-level fields the legacy DTO actually enforced.</summary>
    [Theory]
    [InlineData("""{"neptunCode":"abc123","result":[]}""")]
    [InlineData("""{"gitHubRepoName":"bmeaut/viaubc01-abc123","result":[]}""")]
    public async Task BodyMissingRequiredFields_Returns400(string body) =>
        await AssertBadRequestAsync(await PostAsync(body), "Body cannot be deserialized as JSON");

    /// <summary>Authentication here is the signature alone; a 401 would mean a fallback policy had crept in.</summary>
    [Fact]
    public async Task WithoutCredentials_IsNotUnauthorized()
    {
        var client = factory.CreateClient();
        using var content = new StringContent(SampleBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(Url, content);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AssertBadRequestAsync(HttpResponseMessage response, string expectedError)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(expectedError, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Reimplements the Go client's scheme rather than calling the validator, so these tests cannot pass by
    /// agreeing with a mistake in the code under test.
    /// </summary>
    private static string Sign(string verb, string url, DateTime date, string body, string secret)
    {
        var stringToSign = string.Concat(
            verb.ToUpperInvariant(),
            "\n",
            url.ToLowerInvariant(),
            "\n",
            date.ToString("R", CultureInfo.InvariantCulture),
            "\n",
            body);

        using var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(secret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
    }

    private async Task<HttpResponseMessage> PostAsync(
        string body, string? token = Token, string? signature = "", string? date = "")
    {
        var client = factory.CreateClient();

        // TestServer serves over http; UseHttpsRedirection no-ops without a configured https port, which is
        // what the existing smoke tests already rely on.
        var absoluteUrl = new Uri(client.BaseAddress!, Url).ToString();
        var dateValue = date is null ? null : (date.Length == 0 ? factory.Now.UtcDateTime.ToString("R", CultureInfo.InvariantCulture) : date);

        using var request = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        if (token is not null)
            request.Headers.Add("X-Ahk-Token", token);

        // Without validation: HttpClient parses Date itself and refuses to send a malformed one, which would
        // make the "not valid RFC1123" branch unreachable from a test. Over the wire nothing stops a caller
        // (or a proxy) from sending exactly that.
        if (dateValue is not null)
            request.Headers.TryAddWithoutValidation("Date", dateValue);

        if (signature is not null)
        {
            // When the date is missing or malformed the request is rejected before the signature is ever
            // checked, so signing over the frozen clock keeps the helper total.
            var signedDate = dateValue is not null
                && DateTime.TryParseExact(dateValue, "R", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
                    ? parsed
                    : factory.Now.UtcDateTime;

            request.Headers.Add("X-Ahk-Sha256", signature.Length == 0 ? Sign("POST", absoluteUrl, signedDate, body, Secret) : signature);
        }

        request.Headers.Add("X-Ahk-Delivery", "delivery-1");

        return await client.SendAsync(request);
    }

    public sealed class CallbackAppFactory : WebApplicationFactory<Program>
    {
        /// <summary>Frozen, so the ten-minute skew window can be tested from both sides.</summary>
        public DateTimeOffset Now { get; } = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.WithoutWebhookWorker();
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType == typeof(TimeProvider) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)))
                    .ToList();
                foreach (var descriptor in toRemove)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("EvaluationResultEndpointTests"));
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));
            });

            var host = base.CreateHost(builder);

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!db.Courses.IgnoreQueryFilters().Any())
                {
                    var course = new Course
                    {
                        Slug = "viaubc01",
                        Name = "Sample Course",
                        GitHubOrganization = "bmeaut",
                        RepoNamePrefix = "viaubc01",
                    };
                    db.Courses.Add(course);
                    db.SaveChanges();

                    db.CourseWebhookTokens.AddRange(
                        new CourseWebhookToken { CourseId = course.Id, Token = Token, Secret = Secret, Description = "active" },
                        new CourseWebhookToken
                        {
                            CourseId = course.Id,
                            Token = RevokedToken,
                            Secret = Secret,
                            Description = "revoked",
                            RevokedAt = DateTimeOffset.UtcNow,
                        });
                    db.SaveChanges();
                }
            }

            return host;
        }
    }
}
