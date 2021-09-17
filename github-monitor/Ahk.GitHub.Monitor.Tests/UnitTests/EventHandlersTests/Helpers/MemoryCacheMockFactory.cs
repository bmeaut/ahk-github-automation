using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    internal class MemoryCacheMockFactory
    {
        public static readonly IMemoryCache Instance = create();

        private static IMemoryCache create()
        {
            var m = new Mock<IMemoryCache>();
            object unused;
            m.Setup(c => c.TryGetValue(It.IsAny<object>(), out unused)).Returns(false);
            m.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);
            return m.Object;
        }
    }
}
