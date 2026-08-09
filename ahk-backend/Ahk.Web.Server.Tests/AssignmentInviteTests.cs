using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.GitHub;
using Ahk.Web.Services.Submissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// The invite state machine, against a mocked GitHub. This is the path a student walks unattended, so each
/// branch is pinned: what is still missing, what happens when the repository is already there, and that
/// accepting twice does not produce two repositories.
/// </summary>
public class AssignmentInviteTests
{
    private const int CourseId = 1;
    private const string InviteToken = "invite-token";

    private sealed class FixedCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId => CourseId;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public Fixture(bool archived = false, string? organization = "ahk-org")
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            Db = new ApplicationDbContext(options, new FixedCourseProvider());

            Db.Courses.Add(new Course { Id = CourseId, Slug = "viaubc01", Name = "Sample Course", GitHubOrganization = organization });
            Db.Assignments.Add(new Assignment
            {
                Id = 10,
                CourseId = CourseId,
                Name = "Homework 1",
                TemplateRepoName = "ahk-org/viaubc01-hw1",
                InviteToken = InviteToken,
                ArchivedAt = archived ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            });
            Db.SaveChanges();

            GitHub = new Mock<IGitHubRepositoryService>(MockBehavior.Strict);
            Tokens = new Mock<ICourseGitHubAppTokenProvider>();
            Tokens
                .Setup(t => t.GetForCourseAsync(It.IsAny<Course>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GitHubInstallationToken("gh-token", 42, new Dictionary<string, string> { ["administration"] = "write" }, "all"));

            Service = new AssignmentInviteService(
                Db,
                GitHub.Object,
                Tokens.Object,
                new SubmissionResolver(Db),
                NullLogger<AssignmentInviteService>.Instance);
        }

        public ApplicationDbContext Db { get; }

        public Mock<IGitHubRepositoryService> GitHub { get; }

        public Mock<ICourseGitHubAppTokenProvider> Tokens { get; }

        public AssignmentInviteService Service { get; }

        /// <summary>A student who has everything the flow needs, unless a field is nulled out by the caller.</summary>
        public ApplicationUser AddUser(string? neptun = "ABC123", string? gitHubUsername = "octocat")
        {
            var user = new ApplicationUser { Id = 7, UserName = "student@bme.hu", NeptunCode = neptun, GitHubUsername = gitHubUsername };
            Db.Users.Add(user);
            Db.SaveChanges();
            return user;
        }

        /// <summary>Wires the mock for the happy path: the repository does not exist yet and gets created.</summary>
        public void ExpectRepositoryCreated(bool invitationCreated = false)
        {
            GitHub
                .Setup(g => g.GetRepositoryAsync("ahk-org", "viaubc01-hw1-abc123", "gh-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync((GitHubRepository?)null);

            GitHub
                .Setup(g => g.GenerateFromTemplateAsync("ahk-org", "viaubc01-hw1", "ahk-org", "viaubc01-hw1-abc123", "gh-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GitHubRepository("ahk-org/viaubc01-hw1-abc123", "https://github.com/ahk-org/viaubc01-hw1-abc123", false, "main"));

            GitHub
                .Setup(g => g.EnsureActionsEnabledAsync("ahk-org", "viaubc01-hw1-abc123", "gh-token", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            GitHub
                .Setup(g => g.AddCollaboratorAsync("ahk-org", "viaubc01-hw1-abc123", "octocat", "gh-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CollaboratorResult(invitationCreated, invitationCreated ? 99 : null));
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    [Fact]
    public async Task WithoutNeptunCode_TheFlowStopsAndExplainsWhy()
    {
        await using var fixture = new Fixture();
        var user = fixture.AddUser(neptun: null);

        var state = await fixture.Service.GetStateAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.NeedsNeptun, state.Status);
        Assert.Contains("eduID", state.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WithoutGitHubUsername_TheStudentIsAskedForOne()
    {
        await using var fixture = new Fixture();
        var user = fixture.AddUser(gitHubUsername: null);

        var state = await fixture.Service.GetStateAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.NeedsGitHubUsername, state.Status);

        // The repository name is already known, so the screen can tell them what they are about to get.
        Assert.Equal("viaubc01-hw1-abc123", state.RepositoryName);
    }

    [Fact]
    public async Task WithEverythingInPlace_TheStudentIsAskedToConfirm()
    {
        await using var fixture = new Fixture();
        var user = fixture.AddUser();

        var state = await fixture.Service.GetStateAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.ReadyToAccept, state.Status);
        Assert.Equal("ahk-org", state.Organization);
        Assert.Equal("viaubc01-hw1-abc123", state.RepositoryName);
    }

    [Fact]
    public async Task AnArchivedAssignment_TurnsNewStudentsAway()
    {
        await using var fixture = new Fixture(archived: true);
        var user = fixture.AddUser();

        var state = await fixture.Service.GetStateAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.Closed, state.Status);
    }

    /// <summary>Archiving closes the door to newcomers; it does not take a repository away from someone who has one.</summary>
    [Fact]
    public async Task AnArchivedAssignment_StillShowsTheRepositoryToStudentsWhoAccepted()
    {
        await using var fixture = new Fixture(archived: true);
        var user = fixture.AddUser();

        fixture.Db.AssignmentAcceptances.Add(new AssignmentAcceptance
        {
            CourseId = CourseId,
            AssignmentId = 10,
            UserId = user.Id,
            GitHubRepoName = "ahk-org/viaubc01-hw1-abc123",
            RepoUrl = "https://github.com/ahk-org/viaubc01-hw1-abc123",
            GitHubUsername = "octocat",
        });
        await fixture.Db.SaveChangesAsync();

        var state = await fixture.Service.GetStateAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.Accepted, state.Status);
        Assert.Equal("https://github.com/ahk-org/viaubc01-hw1-abc123", state.RepoUrl);
    }

    [Fact]
    public async Task AnUnknownToken_IsNotFound()
    {
        await using var fixture = new Fixture();
        var user = fixture.AddUser();

        var state = await fixture.Service.GetStateAsync(CourseId, "no-such-token", user);

        Assert.Equal(InviteStatus.NotFound, state.Status);
    }

    [Fact]
    public async Task Accepting_CreatesTheRepository_GrantsAccess_AndRecordsThePair()
    {
        await using var fixture = new Fixture();
        var user = fixture.AddUser();
        fixture.ExpectRepositoryCreated();

        var state = await fixture.Service.AcceptAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.Accepted, state.Status);
        Assert.Equal("https://github.com/ahk-org/viaubc01-hw1-abc123", state.RepoUrl);

        fixture.GitHub.Verify(
            g => g.GenerateFromTemplateAsync("ahk-org", "viaubc01-hw1", "ahk-org", "viaubc01-hw1-abc123", "gh-token", It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.GitHub.Verify(
            g => g.AddCollaboratorAsync("ahk-org", "viaubc01-hw1-abc123", "octocat", "gh-token", It.IsAny<CancellationToken>()),
            Times.Once);

        var acceptance = Assert.Single(await fixture.Db.AssignmentAcceptances.IgnoreQueryFilters().ToListAsync());
        Assert.Equal("ahk-org/viaubc01-hw1-abc123", acceptance.GitHubRepoName);
        Assert.False(acceptance.InvitationPending);

        // The student is enrolled and the submission exists, so grades and events have somewhere to land before
        // the first webhook ever arrives.
        Assert.Single(await fixture.Db.Students.IgnoreQueryFilters().Where(s => s.Neptun == "ABC123").ToListAsync());
        Assert.Single(await fixture.Db.Submissions.IgnoreQueryFilters().Where(s => s.GitHubRepoName == "ahk-org/viaubc01-hw1-abc123").ToListAsync());
    }

    /// <summary>A student outside the organization is only invited, and the invitation has to be tracked.</summary>
    [Fact]
    public async Task Accepting_RecordsAPendingInvitationWhenGitHubOnlyInvites()
    {
        await using var fixture = new Fixture();
        var user = fixture.AddUser();
        fixture.ExpectRepositoryCreated(invitationCreated: true);

        var state = await fixture.Service.AcceptAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.Accepted, state.Status);
        Assert.Equal("https://github.com/ahk-org/viaubc01-hw1-abc123/invitations", state.InvitationUrl);

        var acceptance = Assert.Single(await fixture.Db.AssignmentAcceptances.IgnoreQueryFilters().ToListAsync());
        Assert.True(acceptance.InvitationPending);
        Assert.Equal(99, acceptance.InvitationId);
        Assert.NotNull(acceptance.InvitationSentAt);
    }

    /// <summary>
    /// The repository may already exist — a re-run, a manual creation, a migrated course. Linking it is right;
    /// creating a second one is not, and GitHub would reject it anyway.
    /// </summary>
    [Fact]
    public async Task Accepting_LinksAnExistingRepositoryInsteadOfCreatingASecondOne()
    {
        await using var fixture = new Fixture();
        var user = fixture.AddUser();

        fixture.GitHub
            .Setup(g => g.GetRepositoryAsync("ahk-org", "viaubc01-hw1-abc123", "gh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubRepository("ahk-org/viaubc01-hw1-abc123", "https://github.com/ahk-org/viaubc01-hw1-abc123", false, "main"));

        fixture.GitHub
            .Setup(g => g.AddCollaboratorAsync("ahk-org", "viaubc01-hw1-abc123", "octocat", "gh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CollaboratorResult(false, null));

        var state = await fixture.Service.AcceptAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.Accepted, state.Status);
        fixture.GitHub.Verify(
            g => g.GenerateFromTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Reloading the page, or clicking Accept twice, must not hand out a second repository.</summary>
    [Fact]
    public async Task AcceptingTwice_ReturnsTheSameRepositoryAndTouchesGitHubOnce()
    {
        await using var fixture = new Fixture();
        var user = fixture.AddUser();
        fixture.ExpectRepositoryCreated();

        await fixture.Service.AcceptAsync(CourseId, InviteToken, user);
        var second = await fixture.Service.AcceptAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.Accepted, second.Status);
        Assert.Single(await fixture.Db.AssignmentAcceptances.IgnoreQueryFilters().ToListAsync());

        fixture.GitHub.Verify(
            g => g.GenerateFromTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>A course with no organization cannot create anything; say so rather than failing at GitHub.</summary>
    [Fact]
    public async Task ACourseWithoutAnOrganization_ReportsItIsNotConfigured()
    {
        await using var fixture = new Fixture(organization: null);
        var user = fixture.AddUser();

        var state = await fixture.Service.GetStateAsync(CourseId, InviteToken, user);

        Assert.Equal(InviteStatus.NotConfigured, state.Status);
    }

    [Theory]
    // No prefix set → falls back to the template repository's own name (the original behaviour).
    [InlineData("ahk-org/viaubc01-hw1", null, "ABC123", "viaubc01-hw1-abc123")]
    [InlineData("org/hw", null, "xyz789", "hw-xyz789")]
    [InlineData("no-owner", null, "ABC123", "no-owner-abc123")]
    // Prefix set → it is used instead of the template name, lowercased like every repository name.
    [InlineData("ahk-org/viaubc01-hw1", "custom-prefix", "ABC123", "custom-prefix-abc123")]
    [InlineData("ahk-org/viaubc01-hw1", "Custom", "xyz789", "custom-xyz789")]
    public void RepositoryName_UsesPrefixOrFallsBackToTemplateName_Lowercased(string template, string? prefix, string neptun, string expected)
    {
        var assignment = new Assignment { TemplateRepoName = template, RepoNamePrefix = prefix };
        Assert.Equal(expected, AssignmentInviteService.BuildRepositoryName(assignment, neptun));
    }
}
