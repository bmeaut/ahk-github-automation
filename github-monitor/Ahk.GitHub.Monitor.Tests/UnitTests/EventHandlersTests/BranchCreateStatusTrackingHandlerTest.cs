using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class BranchCreateStatusTrackingHandlerTest
    {
        [TestMethod]
        public async Task BranchCreateStatusEventNotTriggeredForNonBranchEvent()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

            var payload = SampleData.BranchCreate.Body
                .Replace("\"ref_type\": \"branch\"", "\"ref_type\": \"tag\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new BranchCreateStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("branch create ignored for", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.IsAny<Services.BranchCreateEvent>()), Times.Never());
        }

        [DataTestMethod]
        [DataRow("master")]
        [DataRow("main")]
        public async Task RepositoryCreateStatusEventIssuedForDefaultBranch(string defaultBranchName)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

            var payload = SampleData.BranchCreate.Body
                .Replace("\"ref\": \"master\"", $"\"ref\": \"{defaultBranchName}\"", System.StringComparison.InvariantCultureIgnoreCase)
                .Replace("\"master_branch\": \"master\"", $"\"master_branch\": \"{defaultBranchName}\"", System.StringComparison.InvariantCultureIgnoreCase)
                .Replace("\"default_branch\": \"master\"", $"\"default_branch\": \"{defaultBranchName}\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new BranchCreateStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("repository create lifecycle handled", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.Is<Services.RepositoryCreateEvent>(e => e.Repository == "aabbcc/ddeeff")), Times.Once());
        }

        [TestMethod]
        public async Task BranchCreateStatusEventIssued()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

            var payload = SampleData.BranchCreate.Body
                .Replace("\"ref\": \"master\"", "\"ref\": \"feature\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new BranchCreateStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("branch create lifecycle handled", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.Is<Services.BranchCreateEvent>(e => e.Repository == "aabbcc/ddeeff")), Times.Once());
        }
    }
}
