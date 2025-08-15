using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;
using Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests;

[TestClass]
public class PullRequestReviewToAssigneeHandlerTest
{
    [TestMethod]
    public async Task NoPullRequestInPayloadIgnored()
    {
        var gitHubMock = GitHubClientMockFactory.CreateDefault();

        var payload = SampleData.PrReviewRequested
            .Replace("\"pull_request\": {", "\"non_pull_request\": {",
                System.StringComparison.InvariantCultureIgnoreCase);

        var eh = new PullRequestReviewToAssigneeHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance,
            ServiceProviderMock.GetMockedObject());
        EventHandlerResult result = await eh.Execute(payload);

        Assert.IsTrue(result.Result.Contains("no pull request", System.StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Assignee.AddAssignees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<Octokit.AssigneesUpdate>()),
            Times.Never());
    }

    [TestMethod]
    public async Task NotReviewRequestActionNotActionable()
    {
        var gitHubMock = GitHubClientMockFactory.CreateDefault();

        var eh = new PullRequestReviewToAssigneeHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance,
            ServiceProviderMock.GetMockedObject());
        EventHandlerResult result = await eh.Execute(SampleData.PrOpen);

        Assert.IsTrue(result.Result.Contains("not of interest", System.StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Assignee.AddAssignees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<Octokit.AssigneesUpdate>()),
            Times.Never());
    }

    [TestMethod]
    public async Task NoReviewerRequestedDataInPayload()
    {
        var gitHubMock = GitHubClientMockFactory.CreateDefault();

        var payload = SampleData.PrReviewRequested
            .Replace("requested_reviewers", "non_requested_reviewers",
                System.StringComparison.InvariantCultureIgnoreCase);

        var eh = new PullRequestReviewToAssigneeHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance,
            ServiceProviderMock.GetMockedObject());
        EventHandlerResult result = await eh.Execute(payload);

        Assert.IsTrue(result.Result.Contains("no requested reviewer",
            System.StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Assignee.AddAssignees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<Octokit.AssigneesUpdate>()),
            Times.Never());
    }

    [TestMethod]
    public async Task ReviewerRequestedPRIsAssigned()
    {
        var gitHubMock = GitHubClientMockFactory.CreateDefault();

        var eh = new PullRequestReviewToAssigneeHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance,
            ServiceProviderMock.GetMockedObject());
        EventHandlerResult result = await eh.Execute(SampleData.PrReviewRequested);

        Assert.IsTrue(result.Result.Contains("assignee set", System.StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Assignee.AddAssignees("aaaaaaaa", "reporeporepo", 223, It.IsAny<Octokit.AssigneesUpdate>()),
            Times.Once());
    }
}
