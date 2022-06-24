using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    internal class LifecycleStoreMockFactory
    {
        public static ILifecycleStore Default = Create().Object;

        public static Mock<ILifecycleStore> Create()
        {
            var m = new Mock<ILifecycleStore>();
            m.Setup(c => c.StoreEvent(It.IsAny<RepositoryCreateEvent>())).Returns(Task.CompletedTask);
            m.Setup(c => c.StoreEvent(It.IsAny<BranchCreateEvent>())).Returns(Task.CompletedTask);
            m.Setup(c => c.StoreEvent(It.IsAny<WorkflowRunEvent>())).Returns(Task.CompletedTask);
            m.Setup(c => c.StoreEvent(It.IsAny<PullRequestEvent>())).Returns(Task.CompletedTask);
            return m;
        }
    }
}
