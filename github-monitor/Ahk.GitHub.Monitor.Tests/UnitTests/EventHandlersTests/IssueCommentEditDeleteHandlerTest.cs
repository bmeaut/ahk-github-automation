using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class IssueCommentEditDeleteHandlerTest
    {
        [TestMethod]
        public async Task CommentNoIssueInPayloadIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.CommentEdit
                .Replace("\"issue\": {", "\"aaaaa\": {", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new IssueCommentEditDeleteHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("no issue information", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
        }

        [TestMethod]
        public async Task CommentActionRequiresNoAction()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.CommentDelete
                .Replace("\"action\": \"deleted\"", "\"action\": \"aaaaaa\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new IssueCommentEditDeleteHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("not of interest", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
        }

        [TestMethod]
        public async Task CommentDeleteAllowedForOwnComment()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.CommentDelete
                .Replace("\"login\": \"github-actions[bot]\"", "\"login\": \"aaaaaa\"", System.StringComparison.InvariantCultureIgnoreCase)
                .Replace("\"login\": \"senderlogin\"", "\"login\": \"aaaaaa\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new IssueCommentEditDeleteHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("referencing own comment", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
        }

        [TestMethod]
        public async Task CommentEditAllowedForOwnComment()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.CommentEdit
                .Replace("\"login\": \"github-actions[bot]\"", "\"login\": \"aaaaaa\"", System.StringComparison.InvariantCultureIgnoreCase)
                .Replace("\"login\": \"senderlogin\"", "\"login\": \"aaaaaa\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new IssueCommentEditDeleteHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("referencing own comment", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
        }

        [TestMethod]
        public async Task CommentDeleteYieldsWarning()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var eh = new IssueCommentEditDeleteHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(SampleData.CommentDelete);

            Assert.IsTrue(result.Result.Contains("comment action resulting in warning", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(283683683, 78, It.IsAny<string>()),
                Times.Once());
        }

        [TestMethod]
        public async Task CommentEditYieldsWarning()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var eh = new IssueCommentEditDeleteHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(SampleData.CommentEdit);

            Assert.IsTrue(result.Result.Contains("comment action resulting in warning", System.StringComparison.InvariantCultureIgnoreCase));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(283683683, 45, It.IsAny<string>()),
                Times.Once());
        }
    }
}
