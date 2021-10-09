using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
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
            var gh = gitHubMock.WithWorkflowRunsCount("aabbcc", "reporep", "someone", workflowRuns).CreateFactory();

            var eh = new ActionWorkflowRunHandler(gh, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("workflow_run ok, has less then threshold"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Actions.DisableActionsForRepository("aabbcc", "reporep"),
                Times.Never());
        }

        [TestMethod]
        public async Task WorkflowRunsIgnoreLimitForOrgMember()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = getPayload(sender: "orgmember");
            var gh = gitHubMock
                .WithOrganizationMemberGet("orgmember", true)
                .WithWorkflowRunsCount("aabbcc", "reporep", "orgmember", 999)
                .CreateFactory();

            var eh = new ActionWorkflowRunHandler(gh, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("workflow_run ok, not triggered by student"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(283462325, 2, It.IsAny<string>()),
                Times.Never());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Actions.DisableActionsForRepository("aabbcc", "reporep"),
                Times.Never());
        }

        [TestMethod]
        public async Task WorkflowRunsReachedWarningLimit()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = getPayload();
            var gh = gitHubMock
                .WithWorkflowRunsCount("aabbcc", "reporep", "someone", 6)
                .WithPullRequestGetAll(c => c.ReturnsAsync(new[]
                {
                    GitHubMockData.CreatePullRequest(1, Octokit.ItemState.Open, 111),
                    GitHubMockData.CreatePullRequest(2, Octokit.ItemState.Open, 111),
                }))
                .CreateFactory();

            var eh = new ActionWorkflowRunHandler(gh, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("workflow_run warning, threshold exceeded"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(283462325, 2, It.IsAny<string>()),
                Times.Once());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Actions.DisableActionsForRepository("aabbcc", "reporep"),
                Times.Never());
        }

        [TestMethod]
        public async Task WorkflowRunsExceededActionsDisabled()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = getPayload();
            var gh = gitHubMock
                .WithWorkflowRunsCount("aabbcc", "reporep", "someone", 8)
                .WithPullRequestGetAll(c => c.ReturnsAsync(new[]
                {
                    GitHubMockData.CreatePullRequest(1, Octokit.ItemState.Open, 111),
                }))
                .CreateFactory();

            var eh = new ActionWorkflowRunHandler(gh, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("workflow_run limit exceeded, actions disabled"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Issue.Comment.Create(283462325, 1, It.IsAny<string>()),
                Times.Once());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Actions.DisableActionsForRepository("aabbcc", "reporep"),
                Times.Once());
        }

        private static string getPayload(string sender = @"someone") => SampleData.WorkflowRun.Replace(@"!!!SENDER", sender);
    }
}
