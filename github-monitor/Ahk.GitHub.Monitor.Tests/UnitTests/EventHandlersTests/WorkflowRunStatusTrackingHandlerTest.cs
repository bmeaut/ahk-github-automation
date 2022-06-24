using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class WorkflowRunStatusTrackingHandlerTest
    {
        [TestMethod]
        public async Task WorkflowRunNotCompletedEventIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

            var payload = SampleData.WorkflowRun
                .Replace("\"action\": \"completed\"", "\"action\": \"requested\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new WorkflowRunStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("action not of interest", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.IsAny<Services.WorkflowRunEvent>()), Times.Never());
        }

        [TestMethod]
        public async Task WorkflowRunCompletedEventIssued()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

            var payload = SampleData.WorkflowRun;

            var eh = new WorkflowRunStatusTrackingHandler(gitHubMock.CreateFactory(), statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("workflow_run lifecycle handled", System.StringComparison.InvariantCultureIgnoreCase));
            statusTrackingStoreMock.Verify(c => c.StoreEvent(It.Is<Services.WorkflowRunEvent>(e => e.Repository == "aabbcc/reporep" && e.Conclusion == "success")), Times.Once());
        }
    }
}
