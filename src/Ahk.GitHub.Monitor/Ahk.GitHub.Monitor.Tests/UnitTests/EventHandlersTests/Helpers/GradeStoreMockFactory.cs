using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.GradeStore;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests.Helpers;

internal abstract class GradeStoreMockFactory
{
    public static IGradeStore Default = Create().Object;

    public static Mock<IGradeStore> Create()
    {
        var m = new Mock<IGradeStore>();
        m.Setup(c => c.StoreGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Dictionary<int, double>>())).Returns(Task.CompletedTask);
        m.Setup(c => c.ConfirmAutoGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return m;
    }
}
