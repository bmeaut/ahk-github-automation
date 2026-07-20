using System.Linq;
using System.Net;
using Ahk.Web.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    [Fact]
    public async Task AdminCourses_WithoutAuth_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/admin/courses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownCourseSlug_Returns404_BeforeAuth()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/no-such-course/probe/notes");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // "Testing" (not Development) so the dev data seeder / SQL Server connection are skipped.
            builder.UseEnvironment("Testing");
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

            return base.CreateHost(builder);
        }
    }
}
