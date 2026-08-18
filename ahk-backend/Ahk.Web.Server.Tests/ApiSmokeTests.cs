using System.Linq;
using System.Net;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Boots the real pipeline (auth + controllers + course middleware) with the SQL Server DbContext swapped for
/// an in-memory one, and checks the fundamental contracts hold without a database or authenticated user.
/// </summary>
public class ApiSmokeTests : IClassFixture<ApiSmokeTests.TestAppFactory>
{
    private readonly TestAppFactory factory;

    public ApiSmokeTests(TestAppFactory factory) => this.factory = factory;

    [Fact]
    public async Task Me_WithoutAuth_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// The auth cookies must keep app-specific names. Browsers scope cookies by host and ignore the port, so
    /// on the framework default every ASP.NET Identity app on localhost shares one cookie — and a foreign one
    /// carrying a GUID user id crashes this app's int-keyed Identity inside SecurityStampValidator.
    /// </summary>
    [Fact]
    public void AuthCookies_AreNamedForThisApp()
    {
        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();

        Assert.Equal(Program.ApplicationCookieName, options.Get(IdentityConstants.ApplicationScheme).Cookie.Name);
        Assert.Equal(Program.ExternalCookieName, options.Get(IdentityConstants.ExternalScheme).Cookie.Name);
        Assert.DoesNotContain("AspNetCore.Identity", Program.ApplicationCookieName, StringComparison.Ordinal);
    }

    /// <summary>A cookie this app cannot read must sign the caller out, not surface as a 500.</summary>
    [Fact]
    public async Task UnreadableAuthCookie_Returns401_NotAnError()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{Program.ApplicationCookieName}=not-a-real-cookie-value");

        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Every host/admin surface is behind the site-admin role, including the ones added for the admin UI.</summary>
    [Theory]
    [InlineData("/api/admin/courses")]
    [InlineData("/api/admin/courses/1")]
    [InlineData("/api/admin/courses/1/github")]
    [InlineData("/api/admin/courses/1/members")]
    [InlineData("/api/admin/courses/1/tokens")]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/users/1")]
    [InlineData("/api/admin/health")]
    [InlineData("/api/admin/health/1")]
    public async Task AdminEndpoints_WithoutAuth_Return401(string url)
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownCourseSlug_Returns404_BeforeAuth()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/no-such-course/statuses");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// The student-facing surfaces need a session, but nothing more. They are reachable by people who are not
    /// members of any course — that is the point of an invite link — so they must sit behind plain
    /// authentication rather than the CourseMember policy.
    /// </summary>
    [Theory]
    [InlineData("/api/my/assignments")]
    [InlineData("/api/viaubc01/invite/some-token")]
    public async Task StudentEndpoints_WithoutAuth_Return401(string url)
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Assignment administration stays behind course membership, unlike the invite endpoint.</summary>
    [Fact]
    public async Task AssignmentAdministration_WithoutAuth_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/viaubc01/assignments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // "Testing" (not Development) so the dev data seeder / SQL Server connection are skipped.
            builder.UseEnvironment("Testing");
            builder.WithoutWebhookWorker();
            builder.ConfigureServices(services =>
            {
                // Drop the SqlServer registration (the options, EF's options-configuration, and the context)
                // before swapping in the in-memory provider, otherwise two providers end up registered.
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)))
                    .ToList();
                foreach (var descriptor in toRemove)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("ApiSmokeTests"));
            });

            var host = base.CreateHost(builder);

            // CourseResolutionMiddleware 404s an unknown {course} slug before authorization ever runs, so the
            // course-scoped tests need a real course to aim at — otherwise they would assert 404 and prove
            // nothing about the policy on the endpoint.
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!db.Courses.Any(c => c.Slug == "viaubc01"))
                {
                    db.Courses.Add(new Course { Slug = "viaubc01", Name = "Sample Course" });
                    db.SaveChanges();
                }
            }

            return host;
        }
    }
}
