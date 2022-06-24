using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class RepositoryCreateStatusTrackingHandlerTest
    {
        [TestMethod]
        public async Task RepositoryNotCreatedEventIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

            var payload = SampleData.RepositoryCreate
                .Replace("\"action\": \"created\"", "\"action\": \"deleted\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new RepositoryCreateStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("action not of interest", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.IsAny<Services.BranchCreateEvent>()), Times.Never());
        }

        [TestMethod]
        public async Task RepositoryCreateStatusEventIssued()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

            var payload = SampleData.RepositoryCreate;

            var eh = new RepositoryCreateStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("repository create lifecycle handled", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.Is<Services.RepositoryCreateEvent>(e => e.Repository == "aabbcc/ddeeff")), Times.Once());
        }
    }
}
