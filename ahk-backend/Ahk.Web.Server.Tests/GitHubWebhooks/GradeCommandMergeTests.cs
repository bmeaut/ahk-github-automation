using System.Net;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHubWebhooks;
using Ahk.Web.Services.GitHubWebhooks.Handlers.GradeComment;
using Ahk.Web.Services.Grading;
using Ahk.Web.Services.Grading.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;
using Octokit.Internal;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// What <c>/ahk ok</c> does when the pull request cannot simply be merged.
///
/// <para>Both cases here are real production failures. A teacher graded a draft pull request: GitHub's
/// <c>mergeable</c> is <em>true</em> for a draft — it reports conflicts, not permission — so the merge was
/// attempted, refused with 405, and the grade write that came after it never ran. The teacher saw their review
/// land and had no signal at all that nothing had been recorded.</para>
/// </summary>
public class GradeCommandMergeTests
{
    private const long RepositoryId = 55;
    private const int PullRequestNumber = 12;

    /// <summary>
    /// A draft refuses the whole command: no grade, no approving review, no merge. The grade service is a
    /// strict mock with no setups, so any call to it fails the test rather than being asserted after the fact.
    /// </summary>
    [Fact]
    public async Task DraftPullRequest_IsNotGradedMergedOrApproved()
    {
        var github = new GitHubDouble(draft: true, mergeable: true);
        var grades = new Mock<IGradeService>(MockBehavior.Strict);
        var handler = NewHandler(grades.Object);

        var result = await handler.ExecuteAsync(github.Context("/ahk ok 5"));

        Assert.Equal("action performed: not graded: the pull request is still a draft", result.Result);

        // The comment is the only thing a teacher grading through a review ever sees.
        Assert.Contains("still a draft", Assert.Single(github.Comments), StringComparison.Ordinal);
        Assert.Contains("Ez a pull request még piszkozat", Assert.Single(github.Comments), StringComparison.Ordinal);
        Assert.Equal(ReactionType.Confused, Assert.Single(github.Reactions));

        // MockBehavior.Strict would already have thrown; this states the intent.
        grades.VerifyNoOtherCalls();
    }

    /// <summary>
    /// ⚠️ The ordering test. The merge fails the way GitHub failed in production, and the grade must still have
    /// been recorded — that is the whole reason the grade write moved ahead of the merge.
    /// </summary>
    [Fact]
    public async Task MergeFailing_StillLeavesTheGradeRecorded()
    {
        var github = new GitHubDouble(draft: false, mergeable: true, mergeThrows: true);
        var grades = new Mock<IGradeService>();
        grades.Setup(g => g.SetGradeAsync(It.IsAny<int>(), It.IsAny<SetGradeInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GradeRecord());

        var handler = NewHandler(grades.Object);

        await Assert.ThrowsAsync<ApiException>(() => handler.ExecuteAsync(github.Context("/ahk ok 5")));

        grades.Verify(
            g => g.SetGradeAsync(It.IsAny<int>(), It.Is<SetGradeInput>(i => i.Results.Contains(5d)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// A pull request GitHub has not finished assessing is a race, not a refusal, so it is still graded — but
    /// the result has to say it was not merged, or a skipped merge reads exactly like a completed one.
    /// </summary>
    [Fact]
    public async Task UnknownMergeability_IsGradedAndSaysItWasNotMerged()
    {
        var github = new GitHubDouble(draft: false, mergeable: null);
        var grades = new Mock<IGradeService>();
        grades.Setup(g => g.SetGradeAsync(It.IsAny<int>(), It.IsAny<SetGradeInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GradeRecord());

        var handler = NewHandler(grades.Object);

        var result = await handler.ExecuteAsync(github.Context("/ahk ok 5"));

        Assert.Contains("was not merged", result.Result, StringComparison.Ordinal);
        Assert.Contains("had not finished working out", result.Result, StringComparison.Ordinal);
        Assert.Empty(github.Merged);
        grades.Verify(
            g => g.SetGradeAsync(It.IsAny<int>(), It.IsAny<SetGradeInput>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AMergeablePullRequest_IsApprovedMergedAndGraded()
    {
        var github = new GitHubDouble(draft: false, mergeable: true);
        var grades = new Mock<IGradeService>();
        grades.Setup(g => g.SetGradeAsync(It.IsAny<int>(), It.IsAny<SetGradeInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GradeRecord());

        var handler = NewHandler(grades.Object);

        var result = await handler.ExecuteAsync(github.Context("/ahk ok 5"));

        Assert.Equal("action performed: comment operation to grade done; grades: 5", result.Result);
        Assert.Equal(PullRequestNumber, Assert.Single(github.Merged));
        Assert.Equal(PullRequestNumber, Assert.Single(github.Approved));
        Assert.Equal(ReactionType.Plus1, Assert.Single(github.Reactions));
        Assert.Empty(github.Comments);
    }

    private static GradeCommandIssueCommentHandler NewHandler(IGradeService grades) =>
        new(grades, new MemoryCache(new MemoryCacheOptions()), NullLogger<GradeCommandIssueCommentHandler>.Instance);

    /// <summary>
    /// The GitHub side of one <c>issue_comment</c> delivery: enough of the client for the opt-in gate, the
    /// organization-membership check and the pull request, with what was written to it recorded.
    /// </summary>
    private sealed class GitHubDouble
    {
        public GitHubDouble(bool draft, bool? mergeable, bool mergeThrows = false)
        {
            var contents = new Mock<IRepositoryContentsClient>();
            contents.Setup(c => c.GetAllContentsByRef(It.IsAny<long>(), ".github/ahk-monitor.yml", It.IsAny<string>()))
                .ReturnsAsync(new[] { FileContent("enabled: true") });
            contents.Setup(c => c.GetAllContentsByRef(It.IsAny<long>(), "neptun.txt", It.IsAny<string>()))
                .ReturnsAsync(new[] { FileContent("ABC123\n") });

            var repositories = new Mock<IRepositoriesClient>();
            repositories.SetupGet(r => r.Content).Returns(contents.Object);

            // The commenting user is a member of the organization, so the command is allowed.
            var members = new Mock<IOrganizationMembersClient>();
            members.Setup(m => m.CheckMember(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            var organization = new Mock<IOrganizationsClient>();
            organization.SetupGet(o => o.Member).Returns(members.Object);

            var reviews = new Mock<IPullRequestReviewsClient>();
            reviews.Setup(r => r.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<PullRequestReviewCreate>()))
                .ReturnsAsync(new PullRequestReview())
                .Callback((long _, int number, PullRequestReviewCreate _) => Approved.Add(number));

            var pullRequests = new Mock<IPullRequestsClient>();
            pullRequests.SetupGet(p => p.Review).Returns(reviews.Object);
            pullRequests.Setup(p => p.Get(It.IsAny<long>(), It.IsAny<int>()))
                .ReturnsAsync(PullRequestJson(draft, mergeable));
            pullRequests.Setup(p => p.Merge(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<MergePullRequest>()))
                .Returns((long _, int number, MergePullRequest _) => mergeThrows
                    ? throw new ApiException("Pull Request is still a draft", HttpStatusCode.MethodNotAllowed)
                    : Task.FromResult(Record(number)));

            var comments = new Mock<IIssueCommentsClient>();
            comments.Setup(c => c.Create(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new IssueComment())
                .Callback((long _, long _, string body) => Comments.Add(body));

            var issues = new Mock<IIssuesClient>();
            issues.SetupGet(i => i.Comment).Returns(comments.Object);

            var commentReactions = new Mock<IIssueCommentReactionsClient>();
            commentReactions.Setup(r => r.Create(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<NewReaction>()))
                .ReturnsAsync(new Reaction())
                .Callback((long _, long _, NewReaction reaction) => Reactions.Add(reaction.Content));

            var reactions = new Mock<IReactionsClient>();
            reactions.SetupGet(r => r.IssueComment).Returns(commentReactions.Object);

            var client = new Mock<IGitHubClient>();
            client.SetupGet(c => c.Repository).Returns(repositories.Object);
            client.SetupGet(c => c.Organization).Returns(organization.Object);
            client.SetupGet(c => c.PullRequest).Returns(pullRequests.Object);
            client.SetupGet(c => c.Issue).Returns(issues.Object);
            client.SetupGet(c => c.Reaction).Returns(reactions.Object);

            Client = client.Object;
        }

        public IGitHubClient Client { get; }

        public List<string> Comments { get; } = [];

        public List<ReactionType> Reactions { get; } = [];

        public List<int> Approved { get; } = [];

        public List<int> Merged { get; } = [];

        public GitHubWebhookContext Context(string commentBody) => new()
        {
            CourseId = 1,
            GitHubEventName = "issue_comment",
            DeliveryId = "delivery-1",
            RequestBody = IssueCommentPayload(commentBody),
            GitHubClient = Client,
            WorkflowRunThreshold = 5,
        };

        private PullRequestMerge Record(int number)
        {
            Merged.Add(number);
            return new PullRequestMerge();
        }

        /// <summary>
        /// Built by deserializing the JSON GitHub would send rather than through Octokit's long constructors —
        /// the repo's convention for its model doubles. <c>draft</c> and <c>mergeable</c> are independent:
        /// a draft reports <c>mergeable: true</c>, which is the trap this whole file exists for.
        /// </summary>
        private static PullRequest PullRequestJson(bool draft, bool? mergeable) =>
            new SimpleJsonSerializer().Deserialize<PullRequest>($$"""
            {
              "number": {{PullRequestNumber}},
              "state": "open",
              "draft": {{(draft ? "true" : "false")}},
              "mergeable": {{(mergeable is null ? "null" : mergeable.Value ? "true" : "false")}},
              "html_url": "https://github.com/bmeaut/viaubc01-abc123/pull/{{PullRequestNumber}}",
              "head": { "ref": "feature/homework" },
              "user": { "login": "student1", "id": 7 }
            }
            """);

        private static string IssueCommentPayload(string commentBody) => $$"""
        {
          "action": "created",
          "issue": { "number": {{PullRequestNumber}}, "pull_request": { "url": "https://api.github.com/x" } },
          "comment": { "id": 987, "body": "{{commentBody}}", "html_url": "https://github.com/c/1",
                       "user": { "login": "teacher1", "id": 3 } },
          "repository": { "id": {{RepositoryId}}, "name": "viaubc01-abc123", "full_name": "bmeaut/viaubc01-abc123",
                          "default_branch": "main",
                          "owner": { "login": "bmeaut", "id": 9, "type": "Organization" } },
          "installation": { "id": 123 }
        }
        """;

        private static RepositoryContent FileContent(string text)
        {
            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
            return new SimpleJsonSerializer().Deserialize<RepositoryContent>(
                $$"""{"name":"f","path":"f","sha":"s","size":{{text.Length}},"type":"file","encoding":"base64","content":"{{encoded}}"}""");
        }
    }
}
