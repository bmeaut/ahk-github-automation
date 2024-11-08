using System;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.StatusTracking;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;
using Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests;

[TestClass]
public class RepositoryCreateStatusTrackingHandlerTest
{
    [DataTestMethod]
    [DataRow("master")]
    [DataRow("main")]
    public async Task RepositoryCreateStatusEventIssuedForDefaultBranch(string defaultBranchName)
    {
        var gitHubMock = GitHubClientMockFactory.CreateDefault();
        Mock<IStatusTrackingStore> statusTrackingStoreMock = StatusTrackingStoreMockFactory.Create();

        var payload = SampleData.BranchCreate.Body
            .Replace("\"ref\": \"master\"", $"\"ref\": \"{defaultBranchName}\"",
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("\"master_branch\": \"master\"", $"\"master_branch\": \"{defaultBranchName}\"",
                StringComparison.InvariantCultureIgnoreCase)
            .Replace("\"default_branch\": \"master\"", $"\"default_branch\": \"{defaultBranchName}\"",
                StringComparison.InvariantCultureIgnoreCase);

        var eh = new RepositoryCreateStatusTrackingHandler(gitHubMock.CreateFactory(),
            statusTrackingStoreMock.Object, MemoryCacheMockFactory.Instance, ServiceProviderMock.GetMockedObject());
        EventHandlerResult result = await eh.Execute(payload);

        Assert.IsTrue(result.Result.Contains("repository create lifecycle handled",
            StringComparison.InvariantCultureIgnoreCase));
        statusTrackingStoreMock.Verify(
            c => c.StoreEvent(It.Is<RepositoryCreateEvent>(e =>
                e.GitHubRepositoryUrl == "https://github.com/aabbcc/ddeeff" ||
                e.GitHubRepositoryUrl == "https://www.github.com/aabbcc/ddeeff")),
            Times.Once());
    }
}
