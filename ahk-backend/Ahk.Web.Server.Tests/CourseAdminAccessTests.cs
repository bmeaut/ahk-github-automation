using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Admin;
using Ahk.Web.Server.Admin.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// What a <see cref="CourseRole.Admin"/> may do in the course they administer, and — the part that matters —
/// what they may not. The screen hides the GitHub integration, the callback tokens and the delete action from
/// them, but hiding is not a gate: these assert the server refuses those endpoints outright and withholds the
/// credentials from the payload it does return.
///
/// <para>Exercised over real cookie sessions, because the policy reads the signed-in principal's role and
/// memberships; a controller called directly would prove nothing about the pipeline that guards it.</para>
/// </summary>
public class CourseAdminAccessTests : IClassFixture<CourseAdminAccessTests.CourseAdminAppFactory>
{
    private const string Password = "Passw0rd!";

    /// <summary>Enums cross the wire as names (<c>JsonStringEnumConverter</c> in <c>Program</c>), so the test
    /// client has to read them the same way.</summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CourseAdminAppFactory factory;

    public CourseAdminAccessTests(CourseAdminAppFactory factory) => this.factory = factory;

    // ---- What a course admin can see ----

    [Fact]
    public async Task CourseAdmin_ReadsOwnCourse_WithoutIntegrationOrTokens()
    {
        var client = await SignInAsync("courseadmin");

        var response = await client.GetAsync($"/api/admin/courses/{factory.CourseAId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var course = await response.Content.ReadFromJsonAsync<CourseDetailDto>(Json);
        Assert.NotNull(course);

        // The statistics and settings the screen shows are there...
        Assert.Equal("course-a", course!.Slug);
        Assert.Equal(1, course.StudentCount);
        Assert.NotEmpty(course.Members);

        // ...and nothing a course admin has no business holding is.
        Assert.Null(course.GitHubConfig);
        Assert.Empty(course.WebhookTokens);
    }

    [Fact]
    public async Task SiteAdmin_StillReadsIntegrationAndTokens()
    {
        var client = await SignInAsync("admin");

        var course = await client.GetFromJsonAsync<CourseDetailDto>($"/api/admin/courses/{factory.CourseAId}", Json);

        Assert.NotNull(course);
        Assert.NotNull(course!.GitHubConfig);
        Assert.True(course.GitHubConfig!.HasWebhookSecret);
        Assert.Single(course.WebhookTokens);
    }

    [Fact]
    public async Task CourseAdmin_RunsTheHealthCheckForOwnCourse()
    {
        var client = await SignInAsync("courseadmin");

        var response = await client.GetAsync($"/api/admin/health/{factory.CourseAId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CourseAdmin_SearchesForStaffCandidates()
    {
        var client = await SignInAsync("courseadmin");

        var matches = await client.GetFromJsonAsync<List<CourseMemberCandidateDto>>(
            $"/api/admin/courses/{factory.CourseAId}/member-candidates?search=newstaff");

        Assert.NotNull(matches);
        Assert.Contains(matches!, m => m.UserName == "newstaff");
    }

    /// <summary>A one-character term would return an arbitrary slice of the directory, not a search result.</summary>
    [Fact]
    public async Task MemberCandidates_BelowTheMinimumTerm_ReturnsNothing()
    {
        var client = await SignInAsync("courseadmin");

        var matches = await client.GetFromJsonAsync<List<CourseMemberCandidateDto>>(
            $"/api/admin/courses/{factory.CourseAId}/member-candidates?search=n");

        Assert.Empty(matches!);
    }

    // ---- What a course admin cannot reach ----

    [Theory]
    [InlineData("GET", "/api/admin/courses")]
    [InlineData("POST", "/api/admin/courses")]
    [InlineData("PUT", "/api/admin/courses/{a}")]
    [InlineData("DELETE", "/api/admin/courses/{a}?confirmSlug=course-a")]
    [InlineData("GET", "/api/admin/courses/{a}/github")]
    [InlineData("PUT", "/api/admin/courses/{a}/github")]
    [InlineData("GET", "/api/admin/courses/{a}/tokens")]
    [InlineData("POST", "/api/admin/courses/{a}/tokens")]
    [InlineData("DELETE", "/api/admin/courses/{a}/tokens/1")]
    [InlineData("GET", "/api/admin/health")]
    [InlineData("POST", "/api/admin/health/refresh-stale")]
    [InlineData("GET", "/api/admin/users")]
    [InlineData("GET", "/api/admin/webhook-deliveries")]
    public async Task CourseAdmin_IsRefusedTheSiteAdminSurface(string method, string url)
    {
        var client = await SignInAsync("courseadmin");

        var response = await SendAsync(client, method, url.Replace("{a}", factory.CourseAId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Being an admin of one course grants nothing in any other.</summary>
    [Theory]
    [InlineData("GET", "/api/admin/courses/{b}")]
    [InlineData("GET", "/api/admin/courses/{b}/members")]
    [InlineData("PUT", "/api/admin/courses/{b}/members")]
    [InlineData("DELETE", "/api/admin/courses/{b}/members/1")]
    [InlineData("GET", "/api/admin/courses/{b}/member-candidates?search=newstaff")]
    [InlineData("GET", "/api/admin/health/{b}")]
    public async Task CourseAdmin_IsRefusedAnotherCourse(string method, string url)
    {
        var client = await SignInAsync("courseadmin");

        var response = await SendAsync(client, method, url.Replace("{b}", factory.CourseBId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>An instructor staffs the course but does not administer it: the management surface is closed.</summary>
    [Theory]
    [InlineData("GET", "/api/admin/courses/{a}")]
    [InlineData("GET", "/api/admin/courses/{a}/members")]
    [InlineData("PUT", "/api/admin/courses/{a}/members")]
    [InlineData("DELETE", "/api/admin/courses/{a}/members/1")]
    [InlineData("GET", "/api/admin/courses/{a}/member-candidates?search=newstaff")]
    [InlineData("GET", "/api/admin/health/{a}")]
    public async Task Instructor_IsRefusedTheCourseAdminSurface(string method, string url)
    {
        var client = await SignInAsync("instructor");

        var response = await SendAsync(client, method, url.Replace("{a}", factory.CourseAId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- Staff management ----

    [Fact]
    public async Task CourseAdmin_AddsRerolesAndRemovesStaff()
    {
        var client = await SignInAsync("courseadmin");
        var newStaffId = await UserIdAsync("newstaff");
        var url = $"/api/admin/courses/{factory.CourseAId}/members";

        try
        {
            var added = await client.PutAsJsonAsync(url, new { userId = newStaffId, role = "Instructor" });
            Assert.Equal(HttpStatusCode.NoContent, added.StatusCode);

            var promoted = await client.PutAsJsonAsync(url, new { userId = newStaffId, role = "Admin" });
            Assert.Equal(HttpStatusCode.NoContent, promoted.StatusCode);

            var members = await client.GetFromJsonAsync<List<CourseMemberDto>>(url, Json);
            Assert.Contains(members!, m => m.UserId == newStaffId && m.Role == CourseRole.Admin);
        }
        finally
        {
            var removed = await client.DeleteAsync($"{url}/{newStaffId}");
            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        }
    }

    /// <summary>
    /// A course admin who demoted or removed themselves would lock themselves out of the course they
    /// administer — the same self-lockout the users endpoints refuse for the site role and for account deletion.
    /// </summary>
    [Fact]
    public async Task CourseAdmin_CannotDemoteOrRemoveThemselves()
    {
        var client = await SignInAsync("courseadmin");
        var selfId = await UserIdAsync("courseadmin");
        var url = $"/api/admin/courses/{factory.CourseAId}/members";

        var demoted = await client.PutAsJsonAsync(url, new { userId = selfId, role = "Instructor" });
        Assert.Equal(HttpStatusCode.BadRequest, demoted.StatusCode);

        var removed = await client.DeleteAsync($"{url}/{selfId}");
        Assert.Equal(HttpStatusCode.BadRequest, removed.StatusCode);

        // Still an admin of the course afterwards.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await db.CourseMemberships.IgnoreQueryFilters()
            .SingleAsync(m => m.UserId == selfId && m.CourseId == factory.CourseAId);
        Assert.Equal(CourseRole.Admin, membership.Role);
    }

    /// <summary>The guard is for course admins only: a site admin can always get back in, so it does not apply.</summary>
    [Fact]
    public async Task SiteAdmin_MayChangeTheirOwnMembership()
    {
        var client = await SignInAsync("admin");
        var selfId = await UserIdAsync("admin");
        var url = $"/api/admin/courses/{factory.CourseAId}/members";

        var added = await client.PutAsJsonAsync(url, new { userId = selfId, role = "Admin" });
        Assert.Equal(HttpStatusCode.NoContent, added.StatusCode);

        var removed = await client.DeleteAsync($"{url}/{selfId}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
    }

    // ---- The invariant that keeps the above true ----

    /// <summary>
    /// Both admin controllers carry a bare <c>[Authorize]</c> at class level, because a class-level role and an
    /// action-level policy are ANDed and a course admin could never satisfy both. That makes every action
    /// responsible for stating its own rule — and an action that forgets is open to any signed-in user,
    /// students included. This fails the build instead.
    /// </summary>
    [Fact]
    public void EveryAdminAction_StatesItsOwnAuthorization()
    {
        var controllers = new[] { typeof(CoursesAdminController), typeof(CourseHealthAdminController) };

        var unguarded = controllers
            .SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
            .Where(m => !m.GetCustomAttributes<AuthorizeAttribute>().Any())
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        Assert.Empty(unguarded);
    }

    // ---- Fixture ----

    /// <summary>
    /// A cookie-keeping client signed in as one of the seeded accounts. The base address must be https: the
    /// application cookie is issued with <c>Secure</c>, and a cookie container drops those on a plain http
    /// response — which surfaces as every later request being 401, not as a login failure.
    /// </summary>
    private async Task<HttpClient> SignInAsync(string userName)
    {
        var client = factory.CreateDefaultClient(new Uri("https://localhost"), new CookieContainerHandler());
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = Password, rememberMe = false });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        // A body every write endpoint can bind: the point is the 403, which lands before model binding.
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });

        return await client.SendAsync(request);
    }

    private async Task<int> UserIdAsync(string userName)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByNameAsync(userName);
        return user!.Id;
    }

    public sealed class CourseAdminAppFactory : WebApplicationFactory<Program>
    {
        public int CourseAId { get; private set; }

        public int CourseBId { get; private set; }

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

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("CourseAdminAccessTests"));
            });

            var host = base.CreateHost(builder);
            Seed(host.Services);
            return host;
        }

        /// <summary>
        /// Two courses, and the three kinds of account whose reach differs: a site admin, an admin of course A,
        /// and an instructor of course A. Neither course carries GitHub App credentials, so the health checks
        /// report "not configured" locally instead of reaching for api.github.com.
        /// </summary>
        private void Seed(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = sp.GetRequiredService<RoleManager<ApplicationRole>>();

            if (!roles.RoleExistsAsync(Roles.Admin).GetAwaiter().GetResult())
                roles.CreateAsync(new ApplicationRole { Name = Roles.Admin }).GetAwaiter().GetResult();

            var admin = Create(users, "admin", Roles.Admin);
            var courseAdmin = Create(users, "courseadmin", role: null);
            var instructor = Create(users, "instructor", role: null);
            Create(users, "newstaff", role: null);

            var courseA = new Course
            {
                Slug = "course-a",
                Name = "Course A",
                GitHubOrganization = "ahk-course-a",
                GitHubConfig = new CourseGitHubConfig { GitHubWebhookSecret = "secret-a", Enabled = true },
            };
            var courseB = new Course { Slug = "course-b", Name = "Course B", GitHubConfig = new CourseGitHubConfig() };
            db.Courses.AddRange(courseA, courseB);
            db.SaveChanges();

            CourseAId = courseA.Id;
            CourseBId = courseB.Id;

            db.CourseMemberships.AddRange(
                new CourseMembership { UserId = courseAdmin.Id, CourseId = courseA.Id, Role = CourseRole.Admin },
                new CourseMembership { UserId = instructor.Id, CourseId = courseA.Id, Role = CourseRole.Instructor });

            db.CourseWebhookTokens.Add(new CourseWebhookToken
            {
                CourseId = courseA.Id,
                Token = "tok-a",
                Secret = "sec-a",
                Description = "seeded",
            });

            db.Students.Add(new Student { CourseId = courseA.Id, Neptun = "ABC123" });
            db.SaveChanges();

            // Referenced so the unused-variable analyzer sees the admin account is deliberate.
            Assert.NotEqual(0, admin.Id);
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
