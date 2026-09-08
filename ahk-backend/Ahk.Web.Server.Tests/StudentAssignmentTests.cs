using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// The student's home page. Its whole job is to answer "can I actually open this repository yet", which means
/// reconciling what the database remembers with what GitHub currently says — a pending invitation may have been
/// accepted, or may have quietly expired.
/// </summary>
public class StudentAssignmentTests
{
    private const int CourseId = 1;
    private const int UserId = 7;

    /// <summary>
    /// Null current course, deliberately: these endpoints carry no {course} segment. If the service ever stops
    /// using IgnoreQueryFilters, these tests go from green to empty results.
    /// </summary>
    private sealed class NoCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId => null;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public Fixture(bool invitationPending, long? invitationId)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            Db = new ApplicationDbContext(options, new NoCourseProvider());

            Db.Courses.Add(new Course { Id = CourseId, Slug = "viaubc01", Name = "Sample Course", GitHubOrganization = "ahk-org" });
            Db.Users.Add(new ApplicationUser { Id = UserId, UserName = "student@bme.hu", GitHubUsername = "octocat" });
            Db.Assignments.Add(new Assignment { Id = 10, CourseId = CourseId, Name = "Homework 1", TemplateRepoName = "ahk-org/viaubc01-hw1", InviteToken = "t" });
            Db.AssignmentAcceptances.Add(new AssignmentAcceptance
            {
                Id = 100,
                CourseId = CourseId,
                AssignmentId = 10,
                UserId = UserId,
                GitHubRepoName = "ahk-org/viaubc01-hw1-abc123",
                RepoUrl = "https://github.com/ahk-org/viaubc01-hw1-abc123",
                GitHubUsername = "octocat",
                InvitationPending = invitationPending,
                InvitationId = invitationId,
                InvitationSentAt = invitationPending ? DateTimeOffset.UtcNow.AddDays(-8) : null,
            });
            Db.SaveChanges();

            GitHub = new Mock<IGitHubRepositoryService>();
            Tokens = new Mock<ICourseGitHubAppTokenProvider>();
            Tokens
                .Setup(t => t.GetForCourseAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GitHubInstallationToken("gh-token", 42, new Dictionary<string, string>(), "all"));

            Service = new StudentAssignmentService(Db, GitHub.Object, Tokens.Object, NullLogger<StudentAssignmentService>.Instance);
        }

        public ApplicationDbContext Db { get; }

        public Mock<IGitHubRepositoryService> GitHub { get; }

        public Mock<ICourseGitHubAppTokenProvider> Tokens { get; }

        public StudentAssignmentService Service { get; }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    [Fact]
    public async Task ASettledRepository_IsListedAsActiveWithoutAskingGitHub()
    {
        await using var fixture = new Fixture(invitationPending: false, invitationId: null);

        var repositories = await fixture.Service.ListForUserAsync(UserId);

        var repository = Assert.Single(repositories);
        Assert.Equal(RepositoryAccess.Active, repository.Access);
        Assert.Equal("viaubc01", repository.CourseSlug);
        Assert.Equal("Homework 1", repository.AssignmentName);
        Assert.Null(repository.InvitationUrl);

        fixture.GitHub.Verify(
            g => g.IsCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>The student accepted the invitation on GitHub; the portal should notice and stop nagging them.</summary>
    [Fact]
    public async Task AnInvitationAcceptedOnGitHub_IsClearedAndReportedAsActive()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);

        fixture.GitHub
            .Setup(g => g.IsCollaboratorAsync("ahk-org", "viaubc01-hw1-abc123", "octocat", "gh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var repositories = await fixture.Service.ListForUserAsync(UserId);

        Assert.Equal(RepositoryAccess.Active, Assert.Single(repositories).Access);

        var stored = await fixture.Db.AssignmentAcceptances.IgnoreQueryFilters().SingleAsync();
        Assert.False(stored.InvitationPending);
        Assert.Null(stored.InvitationId);
    }

    /// <summary>
    /// Accepting the invitation is what corroborates the student's claim to that GitHub login: only someone
    /// signed in as "octocat" can accept an invitation addressed to it.
    /// </summary>
    [Fact]
    public async Task AnAcceptedInvitation_CorroboratesTheClaimToThatGitHubLogin()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);

        fixture.GitHub
            .Setup(g => g.IsCollaboratorAsync("ahk-org", "viaubc01-hw1-abc123", "octocat", "gh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await fixture.Service.ListForUserAsync(UserId);

        var user = await fixture.Db.Users.SingleAsync();
        Assert.NotNull(user.GitHubVerifiedAt);
    }

    /// <summary>An invitation nobody has accepted yet proves nothing; the claim stays the student's own word.</summary>
    [Fact]
    public async Task APendingInvitation_LeavesTheClaimUnverified()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);

        fixture.GitHub
            .Setup(g => g.IsCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        fixture.GitHub
            .Setup(g => g.FindInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubInvitation(99, "octocat", false, DateTimeOffset.UtcNow));

        await fixture.Service.ListForUserAsync(UserId);

        Assert.Null((await fixture.Db.Users.SingleAsync()).GitHubVerifiedAt);
    }

    /// <summary>
    /// The student re-bound their profile to a different GitHub account, then an old invitation to the previous
    /// login settled. That says nothing about the login they claim now, so it must not be stamped.
    /// </summary>
    [Fact]
    public async Task AnAcceptedInvitationForAPreviousLogin_DoesNotCorroborateTheCurrentOne()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);

        var user = await fixture.Db.Users.SingleAsync();
        user.GitHubUsername = "different-account";
        await fixture.Db.SaveChangesAsync();

        fixture.GitHub
            .Setup(g => g.IsCollaboratorAsync("ahk-org", "viaubc01-hw1-abc123", "octocat", "gh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await fixture.Service.ListForUserAsync(UserId);

        Assert.Null((await fixture.Db.Users.SingleAsync()).GitHubVerifiedAt);
    }

    /// <summary>No access and no invitation left on GitHub means it lapsed — offer the resend, do not leave them waiting.</summary>
    [Fact]
    public async Task AnInvitationGitHubNoLongerHas_IsReportedAsExpired()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);

        fixture.GitHub
            .Setup(g => g.IsCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        fixture.GitHub
            .Setup(g => g.FindInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitHubInvitation?)null);

        var repositories = await fixture.Service.ListForUserAsync(UserId);

        Assert.Equal(RepositoryAccess.InvitationExpired, Assert.Single(repositories).Access);
    }

    [Fact]
    public async Task AnInvitationStillWaiting_IsReportedAsPendingWithSomewhereToGo()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);

        fixture.GitHub
            .Setup(g => g.IsCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        fixture.GitHub
            .Setup(g => g.FindInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubInvitation(99, "octocat", Expired: false, DateTimeOffset.UtcNow.AddDays(-1)));

        var repository = Assert.Single(await fixture.Service.ListForUserAsync(UserId));

        Assert.Equal(RepositoryAccess.InvitationPending, repository.Access);
        Assert.Equal("https://github.com/ahk-org/viaubc01-hw1-abc123/invitations", repository.InvitationUrl);
    }

    /// <summary>GitHub cannot extend an invitation, so the stale one has to be withdrawn before a new one is sent.</summary>
    [Fact]
    public async Task Resending_WithdrawsTheStaleInvitationAndStoresTheNewOne()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);

        fixture.GitHub
            .Setup(g => g.IsCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        fixture.GitHub
            .Setup(g => g.FindInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubInvitation(99, "octocat", Expired: true, DateTimeOffset.UtcNow.AddDays(-8)));
        fixture.GitHub
            .Setup(g => g.DeleteInvitationAsync("ahk-org", "viaubc01-hw1-abc123", 99, "gh-token", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        fixture.GitHub
            .Setup(g => g.AddCollaboratorAsync("ahk-org", "viaubc01-hw1-abc123", "octocat", "gh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CollaboratorResult(true, 123));

        var result = await fixture.Service.ResendInvitationAsync(UserId, 100);

        Assert.NotNull(result);
        Assert.Equal(RepositoryAccess.InvitationPending, result!.Access);

        fixture.GitHub.Verify(g => g.DeleteInvitationAsync("ahk-org", "viaubc01-hw1-abc123", 99, "gh-token", It.IsAny<CancellationToken>()), Times.Once);

        var stored = await fixture.Db.AssignmentAcceptances.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(123, stored.InvitationId);
        Assert.True(stored.InvitationPending);
    }

    /// <summary>They accepted between drawing the page and clicking Resend; do not send a pointless invitation.</summary>
    [Fact]
    public async Task Resending_DoesNothingWhenTheStudentAlreadyHasAccess()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);

        fixture.GitHub
            .Setup(g => g.IsCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await fixture.Service.ResendInvitationAsync(UserId, 100);

        Assert.Equal(RepositoryAccess.Active, result!.Access);
        fixture.GitHub.Verify(
            g => g.AddCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>An acceptance belonging to someone else is not found, not someone else's repository.</summary>
    [Fact]
    public async Task Resending_AnotherStudentsAcceptance_IsRefused()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);

        Assert.Null(await fixture.Service.ResendInvitationAsync(userId: 999, acceptanceId: 100));
        Assert.Empty(await fixture.Service.ListForUserAsync(999));
    }

    // ---- Correcting a misspelled GitHub username ----

    /// <summary>
    /// The repository was created for a login the student typed wrong, so it is shared with a stranger. After
    /// the correction the real account has to be invited to it, and the acceptance has to name that account —
    /// every later collaborator and invitation lookup addresses GitHub by this login.
    /// </summary>
    [Fact]
    public async Task CorrectingTheLogin_InvitesTheNewAccountToTheRepository()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);
        await CorrectTheLoginAsync(fixture, "Octocat-Real");

        fixture.GitHub
            .Setup(g => g.AddCollaboratorAsync("ahk-org", "viaubc01-hw1-abc123", "Octocat-Real", "gh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CollaboratorResult(true, 555));

        var result = await fixture.Service.ShareExistingRepositoriesAsync(UserId);

        Assert.Equal(1, result.Shared);
        Assert.Equal(0, result.Failed);

        var stored = await fixture.Db.AssignmentAcceptances.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Octocat-Real", stored.GitHubUsername);
        Assert.True(stored.InvitationPending);
        Assert.Equal(555, stored.InvitationId);
        Assert.NotNull(stored.InvitationSentAt);

        // The invitation aimed at the misspelling is left where it is: it belongs to a stranger, who can only
        // decline it, and withdrawing it would cost a call per repository for nothing.
        fixture.GitHub.Verify(
            g => g.DeleteInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>The roster copy of the login follows too, or the course screens keep showing the misspelling.</summary>
    [Fact]
    public async Task CorrectingTheLogin_UpdatesTheStudentRow()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);
        await CorrectTheLoginAsync(fixture, "Octocat-Real");

        fixture.GitHub
            .Setup(g => g.AddCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CollaboratorResult(true, 555));

        await fixture.Service.ShareExistingRepositoriesAsync(UserId);

        var student = await fixture.Db.Students.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Octocat-Real", student.GitHubUsername);
    }

    /// <summary>
    /// One repository GitHub refuses must not cost the others, nor undo the rename that is already saved. It is
    /// counted and left on its old login, which is what makes it recoverable from the student's own page.
    /// </summary>
    [Fact]
    public async Task ARepositoryGitHubRefuses_IsCountedAndLeftAlone()
    {
        await using var fixture = new Fixture(invitationPending: true, invitationId: 99);
        await CorrectTheLoginAsync(fixture, "Octocat-Real");

        fixture.GitHub
            .Setup(g => g.AddCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitHubOperationException("adding a collaborator", System.Net.HttpStatusCode.Forbidden, "no"));

        var result = await fixture.Service.ShareExistingRepositoriesAsync(UserId);

        Assert.Equal(0, result.Shared);
        Assert.Equal(1, result.Failed);
        Assert.Equal("octocat", (await fixture.Db.AssignmentAcceptances.IgnoreQueryFilters().SingleAsync()).GitHubUsername);
    }

    /// <summary>Nothing to correct: the repository is already shared with the login on the profile.</summary>
    [Fact]
    public async Task ARepositoryAlreadyOnTheCurrentLogin_IsLeftUntouched()
    {
        await using var fixture = new Fixture(invitationPending: false, invitationId: null);

        var result = await fixture.Service.ShareExistingRepositoriesAsync(UserId);

        Assert.Equal(0, result.Shared);
        Assert.Equal(0, result.Failed);
        fixture.GitHub.Verify(
            g => g.AddCollaboratorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Puts the fixture in the state the profile endpoint leaves behind: the user now claims a different login,
    /// while the acceptance and the roster row still name the one the repository was shared with.
    /// </summary>
    private static async Task CorrectTheLoginAsync(Fixture fixture, string login)
    {
        var user = await fixture.Db.Users.SingleAsync();
        user.GitHubUsername = login;
        user.NeptunCode = "ABC123";

        fixture.Db.Students.Add(new Student { Id = 1, CourseId = CourseId, Neptun = "ABC123", GitHubUsername = "octocat" });
        await fixture.Db.SaveChangesAsync();
    }
}
