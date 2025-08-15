using System;
using System.Net;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.EventHandlers;
using Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;
using Ahk.GitHub.Monitor.Services;
using Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests;

[TestClass]
public class RepositoryEventBaseTest
{
    [TestMethod]
    public async Task NonJsonPayloadReturnsError()
    {
        var eh = new TestHandler(GitHubClientMockFactory.CreateDefault().CreateFactory(),
            MemoryCacheMockFactory.Instance);
        EventHandlerResult result = await eh.Execute("invalid payload");

        Assert.IsTrue(result.Result.Contains("payload error", StringComparison.InvariantCultureIgnoreCase));
    }

    [TestMethod]
    public async Task EmptyPayloadReturnsError()
    {
        var eh = new TestHandler(GitHubClientMockFactory.CreateDefault().CreateFactory(),
            MemoryCacheMockFactory.Instance);
        EventHandlerResult result = await eh.Execute(string.Empty);

        Assert.IsTrue(result.Result.Contains("payload error", StringComparison.InvariantCultureIgnoreCase));
    }

    [TestMethod]
    public async Task InvalidPayloadReturnsError()
    {
        var eh = new TestHandler(GitHubClientMockFactory.CreateDefault().CreateFactory(),
            MemoryCacheMockFactory.Instance);
        EventHandlerResult result = await eh.Execute("{a:1}");

        Assert.IsTrue(result.Result.Contains("payload error", StringComparison.InvariantCultureIgnoreCase));
    }

    [TestMethod]
    public async Task NoAhkMonitorConfigYaml1()
    {
        GitHubClientMockFactory gitHubMock = GitHubClientMockFactory.CreateCustom()
            .WithAhkMonitorConfigYamlContent(c =>
                c.ThrowsAsync(new NotFoundException(string.Empty, HttpStatusCode.NotFound)));

        var eh = new TestHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance);
        EventHandlerResult result = await eh.Execute(SampleData.BranchCreate.Body);

        Assert.IsTrue(result.Result.Contains("no ahk-monitor.yml or disabled",
            StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Repository.Content.GetAllContentsByRef(It.IsAny<long>(), ".github/ahk-monitor.yml",
                    It.IsAny<string>()),
            Times.Once());
    }

    [TestMethod]
    public async Task NoAhkMonitorConfigYaml2()
    {
        GitHubClientMockFactory gitHubMock = GitHubClientMockFactory.CreateCustom()
            .WithAhkMonitorConfigYamlContent(c => c.ReturnsAsync(new RepositoryContent[0]));

        var eh = new TestHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance);
        EventHandlerResult result = await eh.Execute(SampleData.BranchCreate.Body);

        Assert.IsTrue(result.Result.Contains("no ahk-monitor.yml or disabled",
            StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Repository.Content.GetAllContentsByRef(It.IsAny<long>(), ".github/ahk-monitor.yml",
                    It.IsAny<string>()),
            Times.Once());
    }

    [TestMethod]
    public async Task AhkMonitorConfigYamlInvalid()
    {
        GitHubClientMockFactory gitHubMock = GitHubClientMockFactory.CreateCustom()
            .WithAhkMonitorConfigYamlContent(c =>
                c.ReturnsAsync(new[] { GitHubMockData.CreateAhkMonitorYamlFileContent("not valid content") }));

        var eh = new TestHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance);
        EventHandlerResult result = await eh.Execute(SampleData.BranchCreate.Body);

        Assert.IsTrue(result.Result.Contains("no ahk-monitor.yml or disabled",
            StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Repository.Content.GetAllContentsByRef(It.IsAny<long>(), ".github/ahk-monitor.yml",
                    It.IsAny<string>()),
            Times.Once());
    }

    /*[TestMethod]
    public async Task NeptunFileNotFound()
    {
        var gitHubMock = GitHubClientMockFactory.CreateCustom()
                .WithNeptunTxtContent(c => c.ThrowsAsync(new Octokit.NotFoundException(string.Empty, System.Net.HttpStatusCode.NotFound)));

        var eh = new TestHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance);
        await eh.Execute(SampleData.BranchCreate.Body);

        var result = await eh.GetNeptunForTest();

        Assert.IsNull(result);
        gitHubMock.GitHubClientMock.Verify(c =>
            c.Repository.Content.GetAllContentsByRef(It.IsAny<long>(), "neptun.txt", It.IsAny<string>()),
            Times.Once());
    }

    [DataTestMethod]
    [DataRow("AB123N", "AB123N")]
    [DataRow("AB123N ", "AB123N")]
    [DataRow("AB123N  ", "AB123N")]
    [DataRow(" AB123N  ", "AB123N")]
    [DataRow("AB123N\n", "AB123N")]
    [DataRow("AB123N\r", "AB123N")]
    [DataRow("AB123N\r\n", "AB123N")]
    public async Task NeptunFileReadCorrectly(string textFileValue, string expected)
    {
        var gitHubMock = GitHubClientMockFactory.CreateCustom()
                .WithNeptunTxtContent(textFileValue);

        var eh = new TestHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance);
        await eh.Execute(SampleData.BranchCreate.Body);

        var result = await eh.GetNeptunForTest();

        Assert.AreEqual(expected, result);
        gitHubMock.GitHubClientMock.Verify(c =>
            c.Repository.Content.GetAllContentsByRef(It.IsAny<long>(), "neptun.txt", It.IsAny<string>()),
            Times.Once());
    }*/

    [TestMethod]
    public async Task EnabledAndHandlerCalled()
    {
        var gitHubMock = GitHubClientMockFactory.CreateDefault();
        var eh = new TestHandler(gitHubMock.CreateFactory(), MemoryCacheMockFactory.Instance);
        EventHandlerResult result = await eh.Execute(SampleData.BranchCreate.Body);

        Assert.IsTrue(result.Result.Contains("TestHandler ok", StringComparison.InvariantCultureIgnoreCase));
        gitHubMock.GitHubClientMock.Verify(c =>
                c.Repository.Content.GetAllContentsByRef(It.IsAny<long>(), ".github/ahk-monitor.yml",
                    It.IsAny<string>()),
            Times.Once());
    }

    private class TestHandler : RepositoryEventBase<ActivityPayload>
    {
        public TestHandler(IGitHubClientFactory gitHubClientFactory,
            IMemoryCache cache)
            : base(gitHubClientFactory, cache, ServiceProviderMock.GetMockedObject())
        {
        }

        protected override Task<EventHandlerResult> executeCore(ActivityPayload webhookPayload) =>
            Task.FromResult(EventHandlerResult.ActionPerformed("TestHandler ok"));
    }
}
