using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class PullRequestReviewToAssigneeHandlerTest
    {
        [TestMethod]
        public async Task NoPullRequestInPayloadIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.PrReviewRequested
                .Replace("\"pull_request\": {", "\"non_pull_request\": {");

            var eh = new PullRequestReviewToAssigneeHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("no pull request"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Assignee.AddAssignees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Octokit.AssigneesUpdate>()),
                Times.Never());
        }

        [TestMethod]
        public async Task NotReviewRequestActionNotActionable()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var eh = new PullRequestReviewToAssigneeHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(SampleData.PrOpen);

            Assert.IsTrue(result.Result.Contains("not of interest"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Assignee.AddAssignees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Octokit.AssigneesUpdate>()),
                Times.Never());
        }

        [TestMethod]
        public async Task NoReviewerRequestedDataInPayload()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.PrReviewRequested
                .Replace("requested_reviewers", "non_requested_reviewers");

            var eh = new PullRequestReviewToAssigneeHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("no requested reviewer"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Assignee.AddAssignees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Octokit.AssigneesUpdate>()),
                Times.Never());
        }

        [TestMethod]
        public async Task ReviewerRequestedPRIsAssigned()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var eh = new PullRequestReviewToAssigneeHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(SampleData.PrReviewRequested);

            Assert.IsTrue(result.Result.Contains("assignee set"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Assignee.AddAssignees("aaaaaaaa", "reporeporepo", 223, It.IsAny<Octokit.AssigneesUpdate>()),
                Times.Once());
        }
    }
}
