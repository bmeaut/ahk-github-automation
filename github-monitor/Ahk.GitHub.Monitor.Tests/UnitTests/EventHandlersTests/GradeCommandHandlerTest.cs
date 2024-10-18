using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.StatusTracking;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class GradeCommandHandlerTest
    {
        public enum CommentType { IssueComment, ReviewComment }

        [TestMethod]
        public async Task IssueCommentNoIssueInPayloadIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var prStatusTrackingStoreMock = new Mock<PullRequestStatusTrackingHandler>(
                gitHubMock.CreateFactory(),
                new StatusTrackingStoreNoop(),
                MemoryCacheMockFactory.Instance,
                NullLogger.Instance
            );

            var payload = SampleData.CommentEdit
                .Replace("\"issue\": {", "\"aaaaa\": {", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new GradeCommandIssueCommentHandler(gitHubMock.CreateFactory(), GradeStoreMockFactory.Default, MemoryCacheMockFactory.Instance, NullLogger.Instance, prStatusTrackingStoreMock.Object);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("no issue information", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Get(It.IsAny<long>(), It.IsAny<int>()),
                Times.Never());
        }

        [TestMethod]
        public async Task IssueCommentActionIsNotCreated()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var prStatusTrackingStoreMock = new Mock<PullRequestStatusTrackingHandler>(
                gitHubMock.CreateFactory(),
                new StatusTrackingStoreNoop(),
                MemoryCacheMockFactory.Instance,
                NullLogger.Instance
            );


            var eh = new GradeCommandIssueCommentHandler(gitHubMock.CreateFactory(), GradeStoreMockFactory.Default, MemoryCacheMockFactory.Instance, NullLogger.Instance, prStatusTrackingStoreMock.Object);
            var result = await eh.Execute(SampleData.CommentDelete);

            Assert.IsTrue(result.Result.Contains("not of interest", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Get(It.IsAny<long>(), It.IsAny<int>()),
                Times.Never());
        }

        [TestMethod]
        public async Task ReviewCommentActionIsNotSubmitted()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.PrReviewComment.Replace("submitted", "edited", System.StringComparison.InvariantCultureIgnoreCase);
            var eh = new GradeCommandReviewCommentHandler(gitHubMock.CreateFactory(), GradeStoreMockFactory.Default, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("not of interest", System.StringComparison.InvariantCultureIgnoreCase));
        }

        [DataTestMethod]
        [DataRow("aaaa", CommentType.IssueComment)]
        [DataRow("/ahk hello", CommentType.IssueComment)]
        [DataRow("/ahk okkkkkkk", CommentType.IssueComment)]
        [DataRow("/ahkkkkk ok", CommentType.IssueComment)]
        [DataRow("a/ahk ok", CommentType.IssueComment)]
        [DataRow("aaaa", CommentType.ReviewComment)]
        [DataRow("/ahk hello", CommentType.ReviewComment)]
        [DataRow("/ahk okkkkkkk", CommentType.ReviewComment)]
        [DataRow("/ahkkkkk ok", CommentType.ReviewComment)]
        [DataRow("a/ahk ok", CommentType.ReviewComment)]
        public async Task CommandNotRecognized(string commentText, CommentType commentType)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var gradeStoreMock = GradeStoreMockFactory.Create();

            var eh = createHandler(commentType, gitHubMock.CreateFactory(), gradeStoreMock.Object);
            var result = await eh.Execute(getPayloadWithComment(commentType, commentText));

            Assert.IsTrue(result.Result.Contains("not recognized as command", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Get(It.IsAny<long>(), It.IsAny<int>()),
                Times.Never());
            gradeStoreMock.Verify(c =>
                c.StoreGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<double>>()),
                Times.Never());
            gradeStoreMock.Verify(c =>
                c.ConfirmAutoGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never());
        }

        [DataTestMethod]
        [DataRow(CommentType.IssueComment)]
        [DataRow(CommentType.ReviewComment)]
        public async Task ActorIsNotOrgMemberCommandNotAllowed(CommentType commentType)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithOrganizationMemberGet("abcabc", false);
            var gradeStoreMock = GradeStoreMockFactory.Create();

            var eh = createHandler(commentType, gitHubMock.CreateFactory(), gradeStoreMock.Object);
            var result = await eh.Execute(getPayloadWithComment(commentType, @"/ahk ok"));

            Assert.IsTrue(result.Result.Contains("not allowed for user", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Merge(336882879, 24, It.IsAny<Octokit.MergePullRequest>()),
                Times.Never());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Organization.Member.CheckMember("org1", "abcabc"),
                Times.Once());
            gradeStoreMock.Verify(c =>
                c.StoreGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<double>>()),
                Times.Never());
            gradeStoreMock.Verify(c =>
                c.ConfirmAutoGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never());
        }

        [DataTestMethod]
        [DataRow(CommentType.IssueComment)]
        [DataRow(CommentType.ReviewComment)]
        public async Task PRAlreadyClosedGradesAccepted(CommentType commentType)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithOrganizationMemberGet("abcabc", true)
                                .WithPullRequestGet(336882879, 24, GitHubMockData.CreatePullRequest(24, Octokit.ItemState.Closed, mergeable: false));
            var gradeStoreMock = GradeStoreMockFactory.Create();

            var eh = createHandler(commentType, gitHubMock.CreateFactory(), gradeStoreMock.Object);
            var result = await eh.Execute(getPayloadWithComment(commentType, @"/ahk ok"));

            Assert.IsTrue(result.Result.Contains("grade done", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Merge(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<Octokit.MergePullRequest>()),
                Times.Never());
            gradeStoreMock.Verify(c =>
                c.StoreGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<double>>()),
                Times.Never());
            gradeStoreMock.Verify(c =>
                c.ConfirmAutoGrade("ABC123", "org1/repo1", 24, "https://www.github.com/org1/repo1/pull/24", It.IsAny<string>(), @"https://github.com/org1/repo1/pull/1#issuecomment-821112111"),
                Times.Once());
        }

        [DataTestMethod]
        [DataRow("/ahk ok", false, CommentType.IssueComment)]
        [DataRow("/ahk ok\rsomething", false, CommentType.IssueComment)]
        [DataRow("/ahk ok\r\nsomething", false, CommentType.IssueComment)]
        [DataRow("something\r\n\r\n/ahk ok", false, CommentType.IssueComment)]
        [DataRow("/ahk ok 1", true, CommentType.IssueComment)]
        [DataRow("/ahk ok 2\rsomething", true, CommentType.IssueComment)]
        [DataRow("/ahk ok 3\r\nsomething", true, CommentType.IssueComment)]
        [DataRow("something\r\n\r\n/ahk ok 4", true, CommentType.IssueComment)]
        [DataRow("/ahk ok 1 2", true, CommentType.IssueComment)]
        [DataRow("/ahk ok 2 3.5\rsomething", true, CommentType.IssueComment)]
        [DataRow("/ahk ok 3.4 5\r\nsomething", true, CommentType.IssueComment)]
        [DataRow("something\r\n\r\n/ahk ok 4 5.6", true, CommentType.IssueComment)]
        [DataRow("/ahk ok", false, CommentType.ReviewComment)]
        [DataRow("/ahk ok\rsomething", false, CommentType.ReviewComment)]
        [DataRow("/ahk ok\r\nsomething", false, CommentType.ReviewComment)]
        [DataRow("something\r\n\r\n/ahk ok", false, CommentType.ReviewComment)]
        [DataRow("/ahk ok 1", true, CommentType.ReviewComment)]
        [DataRow("/ahk ok 2\rsomething", true, CommentType.ReviewComment)]
        [DataRow("/ahk ok 3\r\nsomething", true, CommentType.ReviewComment)]
        [DataRow("something\r\n\r\n/ahk ok 4", true, CommentType.ReviewComment)]
        [DataRow("/ahk ok 1 2", true, CommentType.ReviewComment)]
        [DataRow("/ahk ok 2 3.5\rsomething", true, CommentType.ReviewComment)]
        [DataRow("/ahk ok 3.4 5\r\nsomething", true, CommentType.ReviewComment)]
        [DataRow("something\r\n\r\n/ahk ok 4 5.6", true, CommentType.ReviewComment)]
        public async Task PRNeedsMergeGradesAccepted(string commentText, bool gradesExpected, CommentType commentType)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithOrganizationMemberGet("abcabc", true)
                                .WithPullRequestGet(336882879, 24, GitHubMockData.CreatePullRequest(24, Octokit.ItemState.Open, mergeable: true))
                                .WithNeptunTxtContent("NEPT12");
            var gradeStoreMock = GradeStoreMockFactory.Create();

            var eh = createHandler(commentType, gitHubMock.CreateFactory(), gradeStoreMock.Object);
            var result = await eh.Execute(getPayloadWithComment(commentType, commentText));

            Assert.IsTrue(result.Result.Contains("grade done", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Review.Create(336882879, 24, It.IsAny<Octokit.PullRequestReviewCreate>()),
                Times.Once());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Merge(336882879, 24, It.IsAny<Octokit.MergePullRequest>()),
                Times.Once());

            gradeStoreMock.Verify(c =>
                c.StoreGrade("NEPT12", "org1/repo1", 24, "https://www.github.com/org1/repo1/pull/24", It.IsAny<string>(), @"https://github.com/org1/repo1/pull/1#issuecomment-821112111", It.IsAny<IReadOnlyCollection<double>>()),
                gradesExpected ? Times.Once() : Times.Never());
            gradeStoreMock.Verify(c =>
                c.ConfirmAutoGrade("NEPT12", "org1/repo1", 24, "https://www.github.com/org1/repo1/pull/24", It.IsAny<string>(), @"https://github.com/org1/repo1/pull/1#issuecomment-821112111"),
                gradesExpected ? Times.Never() : Times.Once());
        }

        private static string getPayloadWithComment(CommentType commentType, string value, string user = "abcabc")
        {
            var text = commentType switch
            {
                CommentType.IssueComment => SampleData.CommentCommand,
                CommentType.ReviewComment => SampleData.PrReviewComment,
                _ => throw new System.NotImplementedException(),
            };
            return text.Replace("xxx-body-placeholder-xxx", value, System.StringComparison.InvariantCultureIgnoreCase)
                       .Replace("xxx-comment-creator-user-xxx", user, System.StringComparison.InvariantCultureIgnoreCase);
        }

        private static IGitHubEventHandler createHandler(CommentType commentType, IGitHubClientFactory gitHubClientFactory, IGradeStore gradeStore, PullRequestStatusTrackingHandler prStatusTrackingHandler)
            => commentType switch
            {
                CommentType.IssueComment => new GradeCommandIssueCommentHandler(gitHubClientFactory, gradeStore, MemoryCacheMockFactory.Instance, NullLogger.Instance, prStatusTrackingHandler),
                CommentType.ReviewComment => new GradeCommandReviewCommentHandler(gitHubClientFactory, gradeStore, MemoryCacheMockFactory.Instance, NullLogger.Instance),
                _ => throw new System.NotImplementedException(),
            };
    }
}
