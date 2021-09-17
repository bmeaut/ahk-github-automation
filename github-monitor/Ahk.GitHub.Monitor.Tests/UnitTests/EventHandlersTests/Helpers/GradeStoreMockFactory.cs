using Ahk.GitHub.Monitor.Services;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    internal class GradeStoreMockFactory
    {
        public static IGradeStore Default = Create().Object;

        public static Mock<IGradeStore> Create()
        {
            var m = new Mock<IGradeStore>();
            m.Setup(c => c.StoreGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<double>>())).Returns(Task.CompletedTask);
            return m;
        }
    }
}
