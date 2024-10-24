using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests;

[TestClass]
public class ActionWorkflowRunHandlerTest
{
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    public async Task WorkflowRunsBelowLimit(int workflowRuns)
    {
        var gitHubMock = GitHubClientMockFactory.CreateDefault();

        var payload = getPayload();
        IGitHubClientFactory gh = gitHubMock.WithWorkflowRunsCount("aabbcc", "reporep", "someone", workflowRuns)
            .CreateFactory();

        var eh = new ActionWorkflowRunHandler(gh, MemoryCacheMockFactory.Instance,
            ServiceProviderMock.GetMockedObject());
        EventHandlerResult result = await eh.Execute(payload);

        Assert.IsTrue(result.Result.Contains("workflow_run ok, has less then threshold",
            System.StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never());
    }

    [TestMethod]
    public async Task WorkflowRunsIgnoreLimitForOrgMember()
    {
        var gitHubMock = GitHubClientMockFactory.CreateDefault();

        var payload = getPayload("orgmember");
        IGitHubClientFactory gh = gitHubMock
            .WithOrganizationMemberGet("orgmember", true)
            .WithWorkflowRunsCount("aabbcc", "reporep", "orgmember", 999)
            .CreateFactory();

        var eh = new ActionWorkflowRunHandler(gh, MemoryCacheMockFactory.Instance,
            ServiceProviderMock.GetMockedObject());
        EventHandlerResult result = await eh.Execute(payload);

        Assert.IsTrue(result.Result.Contains("workflow_run ok, not triggered by student",
            System.StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(283462325, 2, It.IsAny<string>()),
            Times.Never());
    }

    [TestMethod]
    public async Task WorkflowRunsReachedWarningLimit()
    {
        var gitHubMock = GitHubClientMockFactory.CreateDefault();

        var payload = getPayload();
        IGitHubClientFactory gh = gitHubMock
            .WithWorkflowRunsCount("aabbcc", "reporep", "someone", 6)
            .WithPullRequestGetAll(c => c.ReturnsAsync(new[]
            {
                GitHubMockData.CreatePullRequest(1, Octokit.ItemState.Open, 111),
                GitHubMockData.CreatePullRequest(2, Octokit.ItemState.Open, 111)
            }))
            .CreateFactory();

        var eh = new ActionWorkflowRunHandler(gh, MemoryCacheMockFactory.Instance,
            ServiceProviderMock.GetMockedObject());
        EventHandlerResult result = await eh.Execute(payload);

        Assert.IsTrue(result.Result.Contains("workflow_run warning, threshold exceeded",
            System.StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(283462325, 2, It.IsAny<string>()),
            Times.Once());
    }

    private static string getPayload(string sender = @"someone") =>
        SampleData.WorkflowRun.Replace(@"!!!SENDER", sender, System.StringComparison.InvariantCultureIgnoreCase);
}
