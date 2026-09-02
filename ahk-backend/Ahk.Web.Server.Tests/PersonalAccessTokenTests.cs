using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth;
using Ahk.Web.Server.Auth.Dto;
using Ahk.Web.Services.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Personal access tokens: what they open, what they must not, and that they carry the owner's identity rather
/// than a weaker one.
///
/// <para>The token replaces the legacy function key, so the security-relevant cases are the negative ones — a
/// revoked token, a locked-out owner, a course the owner does not staff, and every endpoint that does not
/// accept the scheme, the token endpoints included (a token must never mint another token).</para>
/// </summary>
public class PersonalAccessTokenTests : IClassFixture<PersonalAccessTokenTests.TokenAppFactory>
{
    private const string Password = "Passw0rd!";

    private readonly TokenAppFactory factory;

    public PersonalAccessTokenTests(TokenAppFactory factory) => this.factory = factory;

    /// <summary>
    /// The cookie scheme is named as a literal in <see cref="AuthSchemes"/> because an attribute argument must
    /// be a compile-time constant, and <c>IdentityConstants.ApplicationScheme</c> is not one. If that name ever
    /// changed upstream, both read endpoints would silently stop accepting cookies.
    /// </summary>
    [Fact]
    public void TheSpelledOutCookieScheme_MatchesIdentity()
    {
        Assert.Equal(AuthSchemes.ApplicationCookie, IdentityConstants.ApplicationScheme);
        Assert.Equal(
            AuthSchemes.CookieOrPersonalToken,
            IdentityConstants.ApplicationScheme + "," + PersonalAccessTokenAuthenticationHandler.SchemeName);
    }

    // ---- What a token opens ----

    [Theory]
    [InlineData("/api/course-a/statuses")]
    [InlineData("/api/course-a/grades")]
    [InlineData("/api/course-a/grades/csv")]
    public async Task AMembersToken_ReadsTheCourse(string url)
    {
        var response = await GetWithTokenAsync(url, await TokenForAsync("instructor"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// The two roles the request that asked for this feature named. A course admin staffs the course; a site
    /// admin staffs nothing and gets in on the role alone — both through the same policy, over both schemes.
    /// </summary>
    [Theory]
    [InlineData("courseadmin")]
    [InlineData("admin")]
    public async Task CourseAdminsAndSiteAdmins_ReadTheCourse_WithEitherScheme(string userName)
    {
        var withToken = await GetWithTokenAsync("/api/course-a/statuses", await TokenForAsync(userName));
        Assert.Equal(HttpStatusCode.OK, withToken.StatusCode);

        var client = await SignInAsync(userName);
        var withCookie = await client.GetAsync("/api/course-a/grades");
        Assert.Equal(HttpStatusCode.OK, withCookie.StatusCode);
    }

    // ---- What it does not open ----

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("ahkp_deadbeef")]
    [InlineData("")]
    public async Task AnUnknownToken_Is401(string value)
    {
        var response = await GetWithTokenAsync("/api/course-a/statuses", value);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ARevokedToken_Is401_Immediately()
    {
        var (id, value) = await IssueTokenAsync("instructor");
        Assert.Equal(HttpStatusCode.OK, (await GetWithTokenAsync("/api/course-a/statuses", value)).StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var tokens = scope.ServiceProvider.GetRequiredService<IPersonalAccessTokenService>();
            Assert.True(await tokens.RevokeAsync(await UserIdAsync("instructor"), id));
        }

        // No cache to wait out: revocation takes effect on the next request.
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithTokenAsync("/api/course-a/statuses", value)).StatusCode);
    }

    /// <summary>A locked-out account cannot sign in; a token it minted earlier must not be a way round that.</summary>
    [Fact]
    public async Task ALockedOutOwnersToken_Is401()
    {
        var (_, value) = await IssueTokenAsync("lockedout");

        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByNameAsync("lockedout");
            await users.SetLockoutEnabledAsync(user!, true);
            await users.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddDays(1));
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithTokenAsync("/api/course-a/statuses", value)).StatusCode);
    }

    /// <summary>Authentication succeeds and authorization then refuses: the policy still runs for a token.</summary>
    [Fact]
    public async Task AValidTokenOfANonMember_Is403()
    {
        var response = await GetWithTokenAsync("/api/course-b/grades", await TokenForAsync("instructor"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// The scheme is opted into by two controllers only. That is what keeps a token out of the admin API and
    /// away from the token endpoints — a token can never mint or revoke another token.
    /// </summary>
    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/admin/users")]
    [InlineData("/api/profile/tokens")]
    public async Task ATokenDoesNotAuthenticateAnythingElse(string url)
    {
        var response = await GetWithTokenAsync(url, await TokenForAsync("admin"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- The owner's own endpoints ----

    [Fact]
    public async Task TheOwner_CreatesListsAndRevokesTheirOwnTokens()
    {
        var client = await SignInAsync("instructor");

        var created = await client.PostAsJsonAsync("/api/profile/tokens", new { description = "laptop" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var issued = await created.Content.ReadFromJsonAsync<PersonalAccessTokenDto>();
        Assert.StartsWith(PersonalAccessTokenService.Prefix, issued!.Token, StringComparison.Ordinal);

        // Listed with the value: storing it in the clear is what lets the owner copy it again.
        var mine = await client.GetFromJsonAsync<List<PersonalAccessTokenDto>>("/api/profile/tokens");
        Assert.Contains(mine!, t => t.Id == issued.Id && t.Token == issued.Token && t.Description == "laptop");

        var revoked = await client.DeleteAsync($"/api/profile/tokens/{issued.Id}");
        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
    }

    /// <summary>
    /// The id comes from the client, so it is scoped to the caller: one user guessing another's token id must
    /// not be able to revoke it.
    /// </summary>
    [Fact]
    public async Task OneUser_CannotRevokeAnothersToken()
    {
        var (id, value) = await IssueTokenAsync("instructor");

        var client = await SignInAsync("outsider");
        var response = await client.DeleteAsync($"/api/profile/tokens/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetWithTokenAsync("/api/course-a/statuses", value)).StatusCode);
    }

    [Fact]
    public async Task UsingAToken_StampsLastUsed()
    {
        var (id, value) = await IssueTokenAsync("instructor");

        Assert.Null(await LastUsedAsync(id));
        await GetWithTokenAsync("/api/course-a/statuses", value);
        Assert.NotNull(await LastUsedAsync(id));
    }

    // ---- The admin surface ----

    [Fact]
    public async Task AnAdmin_SeesHintsNotValues_AndCanRevoke()
    {
        var (id, value) = await IssueTokenAsync("instructor");
        var ownerId = await UserIdAsync("instructor");
        var client = await SignInAsync("admin");

        var listed = await client.GetFromJsonAsync<List<UserAccessTokenDto>>($"/api/admin/users/{ownerId}/tokens");
        var row = Assert.Single(listed!, t => t.Id == id);
        Assert.Equal(value[^4..], row.TokenHint);

        // The DTO has no value field at all, so the wire cannot carry one.
        var raw = await client.GetStringAsync($"/api/admin/users/{ownerId}/tokens");
        Assert.DoesNotContain(value, raw, StringComparison.Ordinal);

        var revoked = await client.DeleteAsync($"/api/admin/users/{ownerId}/tokens/{id}");
        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithTokenAsync("/api/course-a/statuses", value)).StatusCode);
    }

    /// <summary>The token id is scoped to the user in the route, so a mistyped id cannot revoke someone else's.</summary>
    [Fact]
    public async Task AnAdminRevoking_UnderTheWrongUser_Is404()
    {
        var (id, _) = await IssueTokenAsync("instructor");
        var client = await SignInAsync("admin");

        var response = await client.DeleteAsync($"/api/admin/users/{await UserIdAsync("outsider")}/tokens/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Helpers ----

    private HttpClient CreateClient() => factory.CreateDefaultClient(new Uri("https://localhost"));

    private async Task<HttpResponseMessage> GetWithTokenAsync(string url, string token)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    /// <summary>A cookie-keeping client signed in as one of the seeded accounts (https: the cookie is Secure).</summary>
    private async Task<HttpClient> SignInAsync(string userName)
    {
        var client = factory.CreateDefaultClient(new Uri("https://localhost"), new CookieContainerHandler());
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = Password, rememberMe = false });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private async Task<(int Id, string Value)> IssueTokenAsync(string userName)
    {
        using var scope = factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IPersonalAccessTokenService>();
        var token = await tokens.CreateAsync(await UserIdAsync(userName), $"issued for {userName}");
        return (token.Id, token.Token);
    }

    private async Task<string> TokenForAsync(string userName) => (await IssueTokenAsync(userName)).Value;

    private async Task<DateTimeOffset?> LastUsedAsync(int tokenId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.PersonalAccessTokens.AsNoTracking().Where(t => t.Id == tokenId)
            .Select(t => t.LastUsedAt).SingleAsync();
    }

    private async Task<int> UserIdAsync(string userName)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByNameAsync(userName);
        return user!.Id;
    }

    public sealed class TokenAppFactory : WebApplicationFactory<Program>
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
                    (d.ServiceType.IsGenericType && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)))
                    .ToList();
                foreach (var descriptor in toRemove)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("PersonalAccessTokenTests"));
            });

            var host = base.CreateHost(builder);
            Seed(host.Services);
            return host;
        }

        /// <summary>
        /// Two courses and the accounts whose reach differs: an instructor and a course admin of course A, a
        /// site admin who staffs neither, an outsider, and an account to lock out.
        /// </summary>
        private static void Seed(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = sp.GetRequiredService<RoleManager<ApplicationRole>>();

            if (!roles.RoleExistsAsync(Roles.Admin).GetAwaiter().GetResult())
                roles.CreateAsync(new ApplicationRole { Name = Roles.Admin }).GetAwaiter().GetResult();

            Create(users, "admin", Roles.Admin);
            var instructor = Create(users, "instructor", role: null);
            var courseAdmin = Create(users, "courseadmin", role: null);
            var lockedOut = Create(users, "lockedout", role: null);
            Create(users, "outsider", role: null);

            var courseA = new Course { Slug = "course-a", Name = "Course A" };
            var courseB = new Course { Slug = "course-b", Name = "Course B" };
            db.Courses.AddRange(courseA, courseB);
            db.SaveChanges();

            db.CourseMemberships.AddRange(
                new CourseMembership { UserId = instructor.Id, CourseId = courseA.Id, Role = CourseRole.Instructor },
                new CourseMembership { UserId = courseAdmin.Id, CourseId = courseA.Id, Role = CourseRole.Admin },
                new CourseMembership { UserId = lockedOut.Id, CourseId = courseA.Id, Role = CourseRole.Instructor });

            db.Students.Add(new Student { CourseId = courseA.Id, Neptun = "ABC123" });
            db.SaveChanges();
        }

        private static ApplicationUser Create(UserManager<ApplicationUser> users, string userName, string? role)
        {
            var existing = users.FindByNameAsync(userName).GetAwaiter().GetResult();
            if (existing is not null)
                return existing;

            var user = new ApplicationUser { UserName = userName, Email = $"{userName}@example.com", EmailConfirmed = true };
            users.CreateAsync(user, Password).GetAwaiter().GetResult();

            if (role is not null)
                users.AddToRoleAsync(user, role).GetAwaiter().GetResult();

            return user;
        }
    }
}
