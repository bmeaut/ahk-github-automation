using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Admin;
using Ahk.Web.Server.Admin.Dto;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// The administrator's tools for untangling GitHub account mix-ups: finding a user by their GitHub login,
/// unlinking it (confirmed or not), and linking one to a user who has none.
/// </summary>
public sealed class AdminGitHubLinkTests : IDisposable
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly Mock<IGitHubRepositoryService> gitHub = new();
    private readonly Mock<IStudentAssignmentService> studentAssignments = new();
    private readonly UsersAdminController controller;

    public AdminGitHubLinkTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AdminGitHubLinkTests-{Guid.NewGuid()}")
            .Options;
        this.db = new ApplicationDbContext(options, new NoCourse());

        var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, int>(this.db);
        this.userManager = new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        this.controller = new UsersAdminController(
            this.db,
            this.userManager,
            new Mock<IPersonalAccessTokenService>(MockBehavior.Strict).Object,
            this.gitHub.Object,
            this.studentAssignments.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    [Fact]
    public async Task Search_FindsAUserByPartOfTheirGitHubLogin()
    {
        await CreateUserAsync("alice", gitHub: "Octocat");
        await CreateUserAsync("bob", gitHub: "hubot");

        var result = await this.controller.List(search: "ctoc", courseId: null);

        var page = Assert.IsType<UserListResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("alice", Assert.Single(page.Items).UserName);
    }

    [Fact]
    public async Task Reset_ClearsEvenAConfirmedLink()
    {
        var alice = await CreateUserAsync("alice", gitHub: "Octocat", verified: true);

        var result = await this.controller.ResetGitHub(alice.Id, default);

        var dto = Assert.IsType<UserDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Null(dto.GitHubUsername);

        var stored = await this.userManager.FindByNameAsync("alice");
        Assert.Null(stored!.GitHubUsername);
        Assert.Null(stored.GitHubUserId);
        Assert.Null(stored.GitHubVerifiedAt);
    }

    [Fact]
    public async Task Reset_OfAnUnknownUser_IsNotFound()
    {
        var result = await this.controller.ResetGitHub(4242, default);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Set_StoresGitHubsOwnCasing_Unconfirmed_AndReSharesTheRepositories()
    {
        var alice = await CreateUserAsync("alice");
        GitHubKnows("Octocat", 583231);
        this.studentAssignments
            .Setup(s => s.ShareExistingRepositoriesAsync(alice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepositoryShareResult(2, 1));

        var result = await this.controller.SetGitHub(alice.Id, new SetUserGitHubRequest { GitHubUsername = "https://github.com/octocat/" }, default);

        var response = Assert.IsType<UserGitHubLinkResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("Octocat", response.User.GitHubUsername);
        Assert.Null(response.User.GitHubVerifiedAt);
        Assert.Equal(2, response.RepositoriesShared);
        Assert.Equal(1, response.RepositoriesFailed);

        var stored = await this.userManager.FindByNameAsync("alice");
        Assert.Equal(583231, stored!.GitHubUserId);
        this.gitHub.Verify(g => g.GetUserAsync("octocat", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Setting never re-points a link: the admin resets it first, so the change is deliberate.</summary>
    [Fact]
    public async Task Set_OnAnAlreadyLinkedUser_IsRefused()
    {
        var alice = await CreateUserAsync("alice", gitHub: "Octcat");
        GitHubKnows("Octocat", 583231);

        var result = await this.controller.SetGitHub(alice.Id, new SetUserGitHubRequest { GitHubUsername = "octocat" }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Octcat", (await this.userManager.FindByNameAsync("alice"))!.GitHubUsername);
        this.gitHub.Verify(g => g.GetUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Set_ALoginAnotherUserHolds_IsRefused()
    {
        await CreateUserAsync("alice", gitHub: "Octocat", gitHubId: 583231);
        var bob = await CreateUserAsync("bob");
        GitHubKnows("Octocat", 583231);

        var result = await this.controller.SetGitHub(bob.Id, new SetUserGitHubRequest { GitHubUsername = "octocat" }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null((await this.userManager.FindByNameAsync("bob"))!.GitHubUsername);
        this.studentAssignments.Verify(
            s => s.ShareExistingRepositoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Set_ALoginGitHubDoesNotKnow_IsRefused()
    {
        var alice = await CreateUserAsync("alice");
        this.gitHub
            .Setup(g => g.GetUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitHubUser?)null);

        var result = await this.controller.SetGitHub(alice.Id, new SetUserGitHubRequest { GitHubUsername = "nobody-here" }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Set_WhenGitHubIsUnreachable_IsABadGateway()
    {
        var alice = await CreateUserAsync("alice");
        this.gitHub
            .Setup(g => g.GetUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("down"));

        var result = await this.controller.SetGitHub(alice.Id, new SetUserGitHubRequest { GitHubUsername = "octocat" }, default);

        Assert.Equal(StatusCodes.Status502BadGateway, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }

    public void Dispose()
    {
        this.userManager.Dispose();
        this.db.Dispose();
    }

    private void GitHubKnows(string login, long id) =>
        this.gitHub
            .Setup(g => g.GetUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubUser(login, id));

    private async Task<ApplicationUser> CreateUserAsync(string userName, string? gitHub = null, long? gitHubId = null, bool verified = false)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            GitHubUsername = gitHub,
            GitHubUserId = gitHub is null ? null : gitHubId ?? Random.Shared.NextInt64(1, long.MaxValue),
            GitHubVerifiedAt = verified ? DateTimeOffset.UtcNow : null,
        };
        await this.userManager.CreateAsync(user);
        return user;
    }

    private sealed class NoCourse : ICurrentCourseProvider
    {
        public int? CurrentCourseId => null;
    }
}
