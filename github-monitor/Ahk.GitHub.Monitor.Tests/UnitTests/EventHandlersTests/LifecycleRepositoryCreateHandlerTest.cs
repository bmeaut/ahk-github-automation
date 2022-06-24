using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class LifecycleRepositoryCreateHandlerTest
    {
        [TestMethod]
        public async Task RepositoryNotCreatedEventIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var lifecycleStoreMock = LifecycleStoreMockFactory.Create();

            var payload = SampleData.RepositoryCreate
                .Replace("\"action\": \"created\"", "\"action\": \"deleted\"", System.StringComparison.InvariantCultureIgnoreCase);

            var eh = new RepositoryCreateHandler(gitHubMock.CreateFactory(), lifecycleStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("action not of interest", System.StringComparison.InvariantCultureIgnoreCase));
            lifecycleStoreMock.Verify(c => c.StoreEvent(It.IsAny<Services.BranchCreateEvent>()), Times.Never());
        }

        [TestMethod]
        public async Task RepositoryCreateLifecycleEventIssued()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var lifecycleStoreMock = LifecycleStoreMockFactory.Create();

            var payload = SampleData.RepositoryCreate;

            var eh = new RepositoryCreateHandler(gitHubMock.CreateFactory(), lifecycleStoreMock.Object, MemoryCacheMockFactory.Instance, NullLogger.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("repository create lifecycle handled", System.StringComparison.InvariantCultureIgnoreCase));
            lifecycleStoreMock.Verify(c => c.StoreEvent(It.Is<Services.RepositoryCreateEvent>(e => e.Repository == "aabbcc/ddeeff")), Times.Once());
        }
    }
}
