using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class LifecycleBranchCreateHandlerTest
    {
        [DataTestMethod]
        [DataRow("master")]
        [DataRow("main")]
        public async Task BranchCreateLifecycleEventNotTriggeredForDefaultBranch(string defaultBranchName)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var lifecycleStoreMock = LifecycleStoreMockFactory.Create();

            var payload = SampleData.BranchCreate.Body
                .Replace("\"ref\": \"master\"", $"\"ref\": \"{defaultBranchName}\"", System.StringComparison.InvariantCultureIgnoreCase)
                .Replace("\"master_branch\": \"master\"", $"\"master_branch\": \"{defaultBranchName}\"", System.StringComparison.InvariantCultureIgnoreCase)
                .Replace("\"default_branch\": \"master\"", $"\"default_branch\": \"{defaultBranchName}\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new BranchCreateHandler(gitHubMock.CreateFactory(), lifecycleStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("branch create ignored for", System.StringComparison.InvariantCultureIgnoreCase));
            lifecycleStoreMock.Verify(c => c.StoreEvent(It.IsAny<Services.BranchCreateEvent>()), Times.Never());
        }

        [TestMethod]
        public async Task BranchCreateLifecycleEventIssued()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var lifecycleStoreMock = LifecycleStoreMockFactory.Create();

            var payload = SampleData.BranchCreate.Body
                .Replace("\"ref\": \"master\"", "\"ref\": \"feature\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new BranchCreateHandler(gitHubMock.CreateFactory(), lifecycleStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("branch create lifecycle handled", System.StringComparison.InvariantCultureIgnoreCase));
            lifecycleStoreMock.Verify(c => c.StoreEvent(It.Is<Services.BranchCreateEvent>(e => e.Repository == "aabbcc/ddeeff")), Times.Once());
        }
    }
}
