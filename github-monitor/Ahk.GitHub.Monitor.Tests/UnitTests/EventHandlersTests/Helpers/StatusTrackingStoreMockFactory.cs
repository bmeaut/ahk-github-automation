using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests.Helpers;

internal static class StatusTrackingStoreMockFactory
{
    public static IStatusTrackingStore Default = Create().Object;

    public static Mock<IStatusTrackingStore> Create()
    {
        var m = new Mock<IStatusTrackingStore>();
        m.Setup(c => c.StoreEvent(It.IsAny<RepositoryCreateEvent>())).Returns(Task.CompletedTask);
        m.Setup(c => c.StoreEvent(It.IsAny<PullRequestOpenedEvent>())).Returns(Task.CompletedTask);
        m.Setup(c => c.StoreEvent(It.IsAny<PullRequestStatusChanged>())).Returns(Task.CompletedTask);
        m.Setup(c => c.StoreEvent(It.IsAny<TeacherAssignedEvent>())).Returns(Task.CompletedTask);
        return m;
    }
}
