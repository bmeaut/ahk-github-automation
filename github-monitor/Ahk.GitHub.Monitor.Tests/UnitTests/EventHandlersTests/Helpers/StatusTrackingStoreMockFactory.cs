using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    internal class StatusTrackingStoreMockFactory
    {
        public static IStatusTrackingStore Default = Create().Object;

        public static Mock<IStatusTrackingStore> Create()
        {
            var m = new Mock<IStatusTrackingStore>();
            m.Setup(c => c.StoreEvent(It.IsAny<RepositoryCreateEvent>())).Returns(Task.CompletedTask);
            m.Setup(c => c.StoreEvent(It.IsAny<BranchCreateEvent>())).Returns(Task.CompletedTask);
            m.Setup(c => c.StoreEvent(It.IsAny<WorkflowRunEvent>())).Returns(Task.CompletedTask);
            m.Setup(c => c.StoreEvent(It.IsAny<PullRequestEvent>())).Returns(Task.CompletedTask);
            return m;
        }
    }
}
