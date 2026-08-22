using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Admin impersonation, exercised end-to-end over real cookies: the only way to prove the thing that matters,
/// which is that the <em>session</em> genuinely changes hands and genuinely changes back. Asserting on the
/// controller alone would prove nothing about what the cookie carries.
///
/// <para>The security-relevant cases are the negative ones — a non-admin cannot start an impersonation, an
/// ordinary session cannot "return" to anything, and one impersonation cannot open another.</para>
/// </summary>
public class ImpersonationTests : IClassFixture<ImpersonationTests.ImpersonationAppFactory>
{
    private const string Password = "Passw0rd!";

    private readonly ImpersonationAppFactory factory;

    public ImpersonationTests(ImpersonationAppFactory factory) => this.factory = factory;

    [Fact]
    public async Task Admin_Impersonates_Student_SessionBecomesTheStudent()
    {
        var client = await SignInAsync("admin");
        var studentId = await UserIdAsync("student");

        var response = await client.PostAsync($"/api/auth/impersonate/{studentId}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Equal("student", body!.UserName);
        Assert.Equal("admin", body.ImpersonatorUserName);

        // The session reports the student on its own, not just in the response that started it.
        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal("student", me!.UserName);
        Assert.Equal("admin", me.ImpersonatorUserName);
        Assert.Empty(me.Roles);

        // And it really lost the admin rights: this is the assertion the whole feature stands on.
        var adminApi = await client.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, adminApi.StatusCode);
    }

    [Fact]
    public async Task Stop_RestoresTheAdminSession()
    {
        var client = await SignInAsync("admin");
        var studentId = await UserIdAsync("student");
        await client.PostAsync($"/api/auth/impersonate/{studentId}", null);

        var response = await client.PostAsync("/api/auth/impersonate/stop", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal("admin", me!.UserName);
        Assert.Null(me.ImpersonatorUserName);
        Assert.Contains(Roles.Admin, me.Roles);

        var adminApi = await client.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.OK, adminApi.StatusCode);
    }

    /// <summary>The gate: only a site admin may start one. A signed-in instructor is still just a caller.</summary>
    [Fact]
    public async Task NonAdmin_CannotStartImpersonation()
    {
        var client = await SignInAsync("instructor");
        var studentId = await UserIdAsync("student");

        var response = await client.PostAsync($"/api/auth/impersonate/{studentId}", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Still themselves afterwards.
        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal("instructor", me!.UserName);
    }

    [Fact]
    public async Task Anonymous_CannotStartImpersonation()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsync("/api/auth/impersonate/1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// "Return to admin" is authority-free on its own: without the marker in the signed cookie there is nothing
    /// to return to, so an ordinary user calling it must not gain anything.
    /// </summary>
    [Fact]
    public async Task Stop_OnAnOrdinarySession_IsRejectedAndChangesNothing()
    {
        var client = await SignInAsync("instructor");

        var response = await client.PostAsync("/api/auth/impersonate/stop", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal("instructor", me!.UserName);
        Assert.DoesNotContain(Roles.Admin, me.Roles);
    }

    /// <summary>
    /// No chaining, even when the impersonated account is itself a site admin — that session has the role, so
    /// only the marker stops it, and "who is really acting" stays a single hop.
    /// </summary>
    [Fact]
    public async Task ImpersonatedSession_CannotStartASecondImpersonation()
    {
        var client = await SignInAsync("admin");
        var otherAdminId = await UserIdAsync("admin2");
        var studentId = await UserIdAsync("student");

        var first = await client.PostAsync($"/api/auth/impersonate/{otherAdminId}", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsync($"/api/auth/impersonate/{studentId}", null);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

        // Still the second admin, and the way back still points at the first one.
        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal("admin2", me!.UserName);
        Assert.Equal("admin", me.ImpersonatorUserName);
    }

    [Fact]
    public async Task Admin_CannotImpersonateSelf()
    {
        var client = await SignInAsync("admin");
        var adminId = await UserIdAsync("admin");

        var response = await client.PostAsync($"/api/auth/impersonate/{adminId}", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownUser_Is404()
    {
        var client = await SignInAsync("admin");

        var response = await client.PostAsync("/api/auth/impersonate/999999", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// A client that keeps cookies, signed in as one of the seeded accounts. The base address must be https:
    /// the application cookie is issued with <c>Secure</c>, and a cookie container drops those on a plain http
    /// response — which surfaces as every later request being 401, not as a login failure.
    /// </summary>
    private async Task<HttpClient> SignInAsync(string userName)
    {
        var client = factory.CreateDefaultClient(new Uri("https://localhost"), new CookieContainerHandler());
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = Password, rememberMe = false });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private async Task<int> UserIdAsync(string userName)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByNameAsync(userName);
        return user!.Id;
    }

    public sealed class ImpersonationAppFactory : WebApplicationFactory<Program>
    {
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
                    (d.ServiceType.IsGenericType && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)))
                    .ToList();
                foreach (var descriptor in toRemove)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("ImpersonationTests"));
            });

            var host = base.CreateHost(builder);
            SeedUsers(host.Services);
            return host;
        }

        private static void SeedUsers(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            if (!roles.RoleExistsAsync(Roles.Admin).GetAwaiter().GetResult())
                roles.CreateAsync(new ApplicationRole { Name = Roles.Admin }).GetAwaiter().GetResult();

            Create(users, "admin", Roles.Admin);
            Create(users, "admin2", Roles.Admin);
            Create(users, "instructor", role: null);
            Create(users, "student", role: null);
        }

        private static void Create(UserManager<ApplicationUser> users, string userName, string? role)
        {
            if (users.FindByNameAsync(userName).GetAwaiter().GetResult() is not null)
                return;

            var user = new ApplicationUser { UserName = userName, Email = $"{userName}@example.com", EmailConfirmed = true };
            users.CreateAsync(user, Password).GetAwaiter().GetResult();

            if (role is not null)
                users.AddToRoleAsync(user, role).GetAwaiter().GetResult();
        }
    }
}
