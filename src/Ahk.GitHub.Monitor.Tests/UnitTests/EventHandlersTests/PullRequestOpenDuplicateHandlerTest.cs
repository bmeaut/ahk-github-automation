using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class PullRequestOpenDuplicateHandlerTest
    {
        [TestMethod]
        public async Task NoPullRequestInPayloadIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.PrOpen
                .Replace("\"pull_request\": {", "\"non_pull_request\": {");

            var eh = new PullRequestOpenDuplicateHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("no pull request"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
        }

        [TestMethod]
        public async Task PullRequestActionIsNotOpenIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.PrOpen
                .Replace("\"action\": \"opened\"", "\"action\": \"aaaa\"");

            var eh = new PullRequestOpenDuplicateHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("not of interest"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
        }

        [TestMethod]
        public async Task NoOtherPullRequestNoAction()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithPullRequestGetAll(c => c.ReturnsAsync(new Octokit.PullRequest[0]));

            var eh = new PullRequestOpenDuplicateHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(SampleData.PrOpen);

            Assert.IsTrue(result.Result.Contains("no other PRs"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
        }

        [TestMethod]
        public async Task OtherOpenPullRequestYieldsWarning()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithPullRequestGetAll(c => c.ReturnsAsync(new[]
                                        {
                                            GitHubMockData.CreatePullRequest(189, Octokit.ItemState.Open, 111),
                                            GitHubMockData.CreatePullRequest(324, Octokit.ItemState.Open, 111),
                                        }));

            var eh = new PullRequestOpenDuplicateHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(SampleData.PrOpen);

            Assert.IsTrue(result.Result.Contains("multiple open PRs"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(339316008, 189, It.IsAny<string>()),
                Times.Once());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(339316008, 324, It.IsAny<string>()),
                Times.Once());
        }

        [TestMethod]
        public async Task OtherClosedPullRequestYieldsWarning()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithPullRequestGetAll(c => c.ReturnsAsync(new[]
                                        {
                                            GitHubMockData.CreatePullRequest(189, Octokit.ItemState.Open, 556677),
                                            GitHubMockData.CreatePullRequest(23, Octokit.ItemState.Closed, 556677),
                                        }))
                                .WithIssueEventGetAll(c => c.ReturnsAsync(new[] { GitHubMockData.CreateIssueEvent(Octokit.EventInfoState.Closed, 444444) }));

            var eh = new PullRequestOpenDuplicateHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(SampleData.PrOpen);

            Assert.IsTrue(result.Result.Contains("already closed PRs"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(339316008, 189, It.IsAny<string>()),
                Times.Once());
        }

        [TestMethod]
        public async Task OtherClosedPullRequestByOwnerYieldsNoWarning()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithPullRequestGetAll(c => c.ReturnsAsync(new[]
                                        {
                                            GitHubMockData.CreatePullRequest(23, Octokit.ItemState.Closed, 556677),
                                            GitHubMockData.CreatePullRequest(189, Octokit.ItemState.Open, 556677),
                                        }))
                                .WithIssueEventGetAll(c => c.ReturnsAsync(new[] { GitHubMockData.CreateIssueEvent(Octokit.EventInfoState.Closed, 556677) }));

            var eh = new PullRequestOpenDuplicateHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(SampleData.PrOpen);

            Assert.IsTrue(result.Result.Contains("no other evaluated PRs"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
        }
    }
}
