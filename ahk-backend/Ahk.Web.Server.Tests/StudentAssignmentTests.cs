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
            Db.Users.Add(new ApplicationUser { Id = UserId, UserName = "student@bme.hu" });
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
}
