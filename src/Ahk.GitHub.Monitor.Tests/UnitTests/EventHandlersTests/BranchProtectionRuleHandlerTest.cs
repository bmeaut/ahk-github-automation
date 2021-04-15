using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class BranchProtectionRuleHandlerTest
    {
        [DataTestMethod]
        [DataRow("master")]
        [DataRow("main")]
        public async Task BranchProtectionRuleAppliedForDefaultBranch(string defaultBranchName)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.BranchCreate.Body
                .Replace("\"ref\": \"master\"", $"\"ref\": \"{defaultBranchName}\"")
                .Replace("\"master_branch\": \"master\"", $"\"master_branch\": \"{defaultBranchName}\"")
                .Replace("\"default_branch\": \"master\"", $"\"default_branch\": \"{defaultBranchName}\"");

            var eh = new BranchProtectionRuleHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("branch protection rule applied"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Repository.Branch.UpdateBranchProtection(339388815, defaultBranchName,
                    It.Is<Octokit.BranchProtectionSettingsUpdate>(val => val.RequiredPullRequestReviews.RequiredApprovingReviewCount > 0)),
                Times.Once());
        }

        [TestMethod]
        public async Task BranchProtectionRuleAppliedForFeatureBranch()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.BranchCreate.Body
                .Replace("\"ref\": \"master\"", "\"ref\": \"feature\"");

            var eh = new BranchProtectionRuleHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("branch protection rule applied"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Repository.Branch.UpdateBranchProtection(339388815, "feature", It.IsAny<Octokit.BranchProtectionSettingsUpdate>()),
                Times.Once());
        }

        [TestMethod]
        public async Task BranchProtectionRuleNotAppliedIfNotBranch()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.BranchCreate.Body
                .Replace("\"ref_type\": \"branch\"", "\"ref_type\": \"aaaaa\"");

            var eh = new BranchProtectionRuleHandler(gitHubMock.CreateFactory());
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("not of interest"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Repository.Branch.UpdateBranchProtection(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<Octokit.BranchProtectionSettingsUpdate>()),
                Times.Never());
        }
    }
}
