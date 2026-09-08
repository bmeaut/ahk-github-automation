using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.GitHub;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// One GitHub account backs one portal account. The repository a student is given is shared with whatever
/// login is stored on their profile, so two accounts claiming one login means one of them is aiming an
/// invitation at a stranger.
///
/// <para>EF InMemory does not enforce the filtered unique indexes, so what these prove is the controller's own
/// check — the indexes added in <c>GitHubIdentityVerification</c> are the backstop in production, the same
/// division of labour as <see cref="UserNeptunTests"/>.</para>
/// </summary>
public sealed class GitHubIdentityTests : IDisposable
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly Mock<IGitHubRepositoryService> gitHub = new();
    private readonly Mock<IStudentAssignmentService> studentAssignments = new();
    private readonly ProfileController controller;

    public GitHubIdentityTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"GitHubIdentityTests-{Guid.NewGuid()}")
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

        var tokens = new Mock<ICourseGitHubAppTokenProvider>();

        this.controller = new ProfileController(this.userManager, this.gitHub.Object, tokens.Object, this.db, this.studentAssignments.Object);
    }

    [Fact]
    public async Task AFreshLogin_IsStoredButNotYetVerified()
    {
        var user = await CreateUserAsync("alice");
        SignIn(user);
        GitHubKnows("Octocat", 583231);

        var result = await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "octocat" }, default);

        var dto = Assert.IsType<GitHubProfileResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("Octocat", dto.GitHubUsername);   // GitHub's own casing, not what was typed
        Assert.False(dto.Verified);

        var stored = await this.userManager.FindByNameAsync("alice");
        Assert.Equal("Octocat", stored!.GitHubUsername);
        Assert.Equal(583231, stored.GitHubUserId);
        Assert.Null(stored.GitHubVerifiedAt);
    }

    /// <summary>The heart of it: B may not claim the login A already holds.</summary>
    [Fact]
    public async Task ALoginAnotherAccountHolds_IsRejected()
    {
        var alice = await CreateUserAsync("alice");
        SignIn(alice);
        GitHubKnows("Octocat", 583231);
        await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "octocat" }, default);

        var bob = await CreateUserAsync("bob");
        SignIn(bob);

        var result = await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "octocat" }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Null((await this.userManager.FindByNameAsync("bob"))!.GitHubUsername);
    }

    /// <summary>
    /// Same account, renamed on GitHub. The login differs, so only the numeric id catches it — which is why
    /// both columns are checked and both are indexed.
    /// </summary>
    [Fact]
    public async Task AnAccountAlreadyHeldUnderItsPreviousName_IsRejectedByItsId()
    {
        var alice = await CreateUserAsync("alice");
        SignIn(alice);
        GitHubKnows("Octocat", 583231);
        await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "octocat" }, default);

        var bob = await CreateUserAsync("bob");
        SignIn(bob);
        GitHubKnows("TheOctocat", 583231);

        var result = await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "theoctocat" }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>Re-entering one's own login must not trip the clash check, and nothing is re-shared for it.</summary>
    [Fact]
    public async Task ReEnteringOnesOwnLogin_IsAccepted_AndSharesNothing()
    {
        var alice = await CreateUserAsync("alice");
        SignIn(alice);
        GitHubKnows("Octocat", 583231);

        await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "octocat" }, default);

        var result = await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "Octocat" }, default);

        Assert.IsType<OkObjectResult>(result.Result);
        this.studentAssignments.Verify(
            s => s.ShareExistingRepositoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// The misspelling case this exists for: an unconfirmed login can be corrected, the correction withdraws
    /// nothing (there was nothing corroborated), and every repository already created is shared with the new
    /// account — the earlier invitations went to whoever owns the name that was typed first.
    /// </summary>
    [Fact]
    public async Task ChangingAnUnconfirmedAccount_IsAccepted_AndReSharesTheRepositories()
    {
        var alice = await CreateUserAsync("alice");
        SignIn(alice);
        GitHubKnows("Octcat", 111);
        await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "octcat" }, default);

        this.studentAssignments
            .Setup(s => s.ShareExistingRepositoriesAsync(alice.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RepositoryShareResult(2, 1));

        GitHubKnows("Octocat", 583231);
        var result = await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "octocat" }, default);

        var dto = Assert.IsType<GitHubProfileResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("Octocat", dto.GitHubUsername);
        Assert.False(dto.Verified);
        Assert.Equal(2, dto.RepositoriesShared);
        Assert.Equal(1, dto.RepositoriesFailed);

        Assert.Equal("Octocat", (await this.userManager.FindByNameAsync("alice"))!.GitHubUsername);
    }

    /// <summary>
    /// A confirmed link is not a typo waiting to be fixed: someone signed in as that account and accepted an
    /// invitation with it. Re-pointing it would hand a stranger's account the repositories it earned.
    /// </summary>
    [Fact]
    public async Task ChangingAConfirmedAccount_IsRefused()
    {
        var alice = await CreateUserAsync("alice");
        SignIn(alice);
        GitHubKnows("Octocat", 583231);
        await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "octocat" }, default);

        // As an accepted invitation would have done.
        alice.GitHubVerifiedAt = DateTimeOffset.UtcNow;
        await this.userManager.UpdateAsync(alice);

        GitHubKnows("Hubot", 999);
        var result = await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "hubot" }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);

        var stored = await this.userManager.FindByNameAsync("alice");
        Assert.Equal("Octocat", stored!.GitHubUsername);
        Assert.NotNull(stored.GitHubVerifiedAt);
        this.studentAssignments.Verify(
            s => s.ShareExistingRepositoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// The same account under a new name: GitHub's numeric id says it is still theirs, so the confirmation
    /// survives — but the acceptance rows address collaborators by login, so the repositories are re-shared.
    /// </summary>
    [Fact]
    public async Task AConfirmedAccountRenamedOnGitHub_IsAccepted_AndStaysConfirmed()
    {
        var alice = await CreateUserAsync("alice");
        SignIn(alice);
        GitHubKnows("Octocat", 583231);
        await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "octocat" }, default);

        alice.GitHubVerifiedAt = DateTimeOffset.UtcNow;
        await this.userManager.UpdateAsync(alice);

        GitHubKnows("TheOctocat", 583231);
        var result = await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "theoctocat" }, default);

        var dto = Assert.IsType<GitHubProfileResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("TheOctocat", dto.GitHubUsername);
        Assert.True(dto.Verified);
        this.studentAssignments.Verify(
            s => s.ShareExistingRepositoriesAsync(alice.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>A login GitHub has never heard of is a typo, and says so.</summary>
    [Fact]
    public async Task AnUnknownLogin_IsRejected()
    {
        var user = await CreateUserAsync("alice");
        SignIn(user);
        this.gitHub
            .Setup(g => g.GetUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitHubUser?)null);

        var result = await this.controller.SetGitHubUsername(new SetGitHubUsernameRequest { GitHubUsername = "nobody-here" }, default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
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

    private async Task<ApplicationUser> CreateUserAsync(string userName)
    {
        var user = new ApplicationUser { UserName = userName };
        await this.userManager.CreateAsync(user);
        return user;
    }

    /// <summary>Puts the user behind the controller's <c>User</c>, which is all UserManager.GetUserAsync reads.</summary>
    private void SignIn(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(new IdentityOptions().ClaimsIdentity.UserIdClaimType, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        });

        this.controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
    }

    private sealed class NoCourse : ICurrentCourseProvider
    {
        public int? CurrentCourseId => null;
    }
}
