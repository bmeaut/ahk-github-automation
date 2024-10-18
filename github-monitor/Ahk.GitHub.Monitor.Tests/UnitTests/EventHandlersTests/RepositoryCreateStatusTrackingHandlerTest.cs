using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.StatusTracking;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class RepositoryCreateStatusTrackingHandlerTest
    {
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

            var eh = new RepositoryCreateStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("repository create lifecycle handled", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.Is<RepositoryCreateEvent>(e => e.GitHubRepositoryUrl == "aabbcc/ddeeff")), Times.Once());
        }
    }
}
