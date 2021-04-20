using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class IssueCommentCommandHandlerTest
    {
        [TestMethod]
        public async Task CommentNoIssueInPayloadIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.CommentEdit
                .Replace("\"issue\": {", "\"aaaaa\": {");

            var eh = new IssueCommentCommandHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("no issue information"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Get(It.IsAny<long>(), It.IsAny<int>()),
                Times.Never());
        }

        [TestMethod]
        public async Task CommentActionIsNotCreated()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var eh = new IssueCommentCommandHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(SampleData.CommentDelete);

            Assert.IsTrue(result.Result.Contains("not of interest"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Get(It.IsAny<long>(), It.IsAny<int>()),
                Times.Never());
        }

        [DataTestMethod]
        [DataRow("aaaa")]
        [DataRow("@ahk hello")]
        [DataRow("@ahk okkkkkkk")]
        [DataRow("@ahkkkkk ok")]
        [DataRow("a@ahk ok")]
        public async Task CommandNotRecognized(string commentText)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var eh = new IssueCommentCommandHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(getPayloadWithComment(commentText));

            Assert.IsTrue(result.Result.Contains("not recognized as command"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Get(It.IsAny<long>(), It.IsAny<int>()),
                Times.Never());
        }

        [TestMethod]
        public async Task ActorIsNotOrgMemberCommandNotAllowed()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithOrganizationMemberGet("abcabc", false);

            var eh = new IssueCommentCommandHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(getPayloadWithComment(@"@ahk ok"));

            Assert.IsTrue(result.Result.Contains("not allowed for user"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(336882879, 24, It.Is<string>(tx => tx.Contains("not allowed for abcabc"))),
                Times.Once());
        }

        [TestMethod]
        public async Task PRNotValidStateUserAllowedCommandNotAllowed()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithOrganizationMemberGet("abcabc", true)
                                .WithPullRequestGet(336882879, 24, GitHubMockData.CreatePullRequest(24, Octokit.ItemState.Closed, mergeable: false));

            var eh = new IssueCommentCommandHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(getPayloadWithComment(@"@ahk ok"));

            Assert.IsTrue(result.Result.Contains("not valid due to PR state"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(336882879, 24, It.Is<string>(tx => tx.Contains("cannot be merged"))),
                Times.Once());
        }

        [DataTestMethod]
        [DataRow("@ahk ok")]
        [DataRow("@ahk ok\rsomething")]
        [DataRow("@ahk ok\r\nsomething")]
        [DataRow("something\r\n\r\n@ahk ok")]
        public async Task PRValidStateUserAllowedCommandPerformed(string commentText)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithOrganizationMemberGet("abcabc", true)
                                .WithPullRequestGet(336882879, 24, GitHubMockData.CreatePullRequest(24, Octokit.ItemState.Open, mergeable: true));

            var eh = new IssueCommentCommandHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(getPayloadWithComment(commentText));

            Assert.IsTrue(result.Result.Contains("accept done"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Review.Create(336882879, 24, It.IsAny<Octokit.PullRequestReviewCreate>()),
                Times.Once());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Merge(336882879, 24, It.IsAny<Octokit.MergePullRequest>()),
                Times.Once());
        }

        private static string getPayloadWithComment(string value, string user = "abcabc")
            => SampleData.CommentCommand.Replace("xxx-body-placeholder-xxx", value).Replace("xxx-comment-creator-user-xxx", user);
    }
}
