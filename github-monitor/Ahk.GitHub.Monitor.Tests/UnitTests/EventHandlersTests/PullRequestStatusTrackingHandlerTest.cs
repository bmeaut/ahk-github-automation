using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class PullRequestStatusTrackingHandlerTest
    {
        [TestMethod]
        public async Task PullRequestStatusEventIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

            var payload = SampleData.PrOpen
                .Replace("\"action\": \"opened\"", "\"action\": \"something\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new PullRequestStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("action not of interest", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.IsAny<Services.PullRequestEvent>()), Times.Never());
        }

        [DataTestMethod]
        [DataRow("opened")]
        [DataRow("assigned")]
        [DataRow("review_requested")]
        [DataRow("closed")]
        public async Task PullRequestStatusEventIssued(string actualEventName)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

            var payload = SampleData.PrOpen
                .Replace("\"action\": \"opened\"", $"\"action\": \"{actualEventName}\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new PullRequestStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("pull request lifecycle handled", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.Is<Services.PullRequestEvent>(e => e.Repository == "aabbcc/qqwwee" && e.Action == actualEventName && e.Neptun == "ABC123")), Times.Once());
        }
    }
}
