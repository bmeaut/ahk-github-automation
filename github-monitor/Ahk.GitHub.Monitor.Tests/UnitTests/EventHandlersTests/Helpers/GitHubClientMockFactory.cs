using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Octokit;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    internal class GitHubClientMockFactory
    {
        private readonly Mock<IRepositoryContentsClient> repoContentClient = new Mock<IRepositoryContentsClient>(behavior: MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };
        private readonly Mock<IRepositoriesClient> repositoriesClient = new Mock<IRepositoriesClient>(behavior: MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };
        private readonly Mock<IPullRequestsClient> pullRequestsClient = new Mock<IPullRequestsClient>(behavior: MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };
        private readonly Mock<IIssuesEventsClient> issueEventsClient = new Mock<IIssuesEventsClient>(behavior: MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };
        private readonly Mock<IOrganizationMembersClient> organizationMembersClient = new Mock<IOrganizationMembersClient>(behavior: MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };
        private readonly Mock<IConnection> connection = new Mock<IConnection>(behavior: MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };

        private GitHubClientMockFactory()
        {
            this.GitHubClientMock = new Mock<IGitHubClient>(behavior: MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };

            repositoriesClient.SetupGet(c => c.Content).Returns(repoContentClient.Object);
            GitHubClientMock.SetupGet(c => c.Repository).Returns(repositoriesClient.Object);
            GitHubClientMock.SetupGet(c => c.PullRequest).Returns(pullRequestsClient.Object);
            GitHubClientMock.SetupGet(c => c.Issue.Events).Returns(issueEventsClient.Object);
            GitHubClientMock.SetupGet(c => c.Organization.Member).Returns(organizationMembersClient.Object);
            GitHubClientMock.SetupGet(c => c.Connection).Returns(connection.Object);
        }

        public Mock<IGitHubClient> GitHubClientMock { get; }

        public static GitHubClientMockFactory CreateDefault()
            => new GitHubClientMockFactory().WithDefaultAhkMonitorConfigYamlContent().WithDefaultNeptunTxtContent();

        public static GitHubClientMockFactory CreateCustom()
            => new GitHubClientMockFactory();

        public IGitHubClientFactory CreateFactory()
        {
            var factoryMock = new Mock<IGitHubClientFactory>();
            factoryMock.Setup(f => f.CreateGitHubClient(It.IsAny<long>(), NullLogger.Instance)).ReturnsAsync(GitHubClientMock.Object);
            return factoryMock.Object;
        }

        public GitHubClientMockFactory WithDefaultAhkMonitorConfigYamlContent()
            => WithAhkMonitorConfigYamlContent(c => c.ReturnsAsync(new[] { GitHubMockData.CreateAhkMonitorYamlFileContent() }));

        public GitHubClientMockFactory WithAhkMonitorConfigYamlContent(
            Action<Moq.Language.Flow.ISetup<IRepositoryContentsClient, Task<IReadOnlyList<RepositoryContent>>>> configure)
        {
            configure(repoContentClient.Setup(c => c.GetAllContentsByRef(It.IsAny<long>(), ".github/ahk-monitor.yml", It.IsAny<string>())));
            return this;
        }

        public GitHubClientMockFactory WithDefaultNeptunTxtContent()
            => WithNeptunTxtContent(content: null);

        public GitHubClientMockFactory WithNeptunTxtContent(string content)
        {
            repoContentClient.Setup(c => c.GetAllContentsByRef(It.IsAny<long>(), "neptun.txt", It.IsAny<string>())).ReturnsAsync(new[] { GitHubMockData.CreateNeptunTxtFileContent(content) });
            return this;
        }
        public GitHubClientMockFactory WithNeptunTxtContent(
            Action<Moq.Language.Flow.ISetup<IRepositoryContentsClient, Task<IReadOnlyList<RepositoryContent>>>> configure)
        {
            configure(repoContentClient.Setup(c => c.GetAllContentsByRef(It.IsAny<long>(), "neptun.txt", It.IsAny<string>())));
            return this;
        }

        public GitHubClientMockFactory WithPullRequestGetAll(
            Action<Moq.Language.Flow.ISetup<IPullRequestsClient, Task<IReadOnlyList<PullRequest>>>> configure)
        {
            configure(pullRequestsClient.Setup(c => c.GetAllForRepository(It.IsAny<long>(), It.IsAny<PullRequestRequest>())));
            return this;
        }

        public GitHubClientMockFactory WithPullRequestGet(long repositoryId, int number, PullRequest value)
        {
            pullRequestsClient.Setup(c => c.Get(repositoryId, number)).ReturnsAsync(value);
            return this;
        }

        public GitHubClientMockFactory WithIssueEventGetAll(
            Action<Moq.Language.Flow.ISetup<IIssuesEventsClient, Task<IReadOnlyList<IssueEvent>>>> configure)
        {
            configure(issueEventsClient.Setup(c => c.GetAllForIssue(It.IsAny<long>(), It.IsAny<int>())));
            return this;
        }

        public GitHubClientMockFactory WithOrganizationMemberGet(string userName, bool result)
        {
            organizationMembersClient.Setup(c => c.CheckMember(It.IsAny<string>(), userName)).ReturnsAsync(result);
            return this;
        }

        public GitHubClientMockFactory WithWorkflowRunsCount(string owner, string repo, string actor, int count)
        {
            connection.Setup(c =>
                c.Get<GitHubClientWorkflowRunsExtensions.ListWorkflowRunsResponse>(
                    new Uri($"repos/{owner}/{repo}/actions/runs", UriKind.Relative),
                    It.Is<IDictionary<string, string>>(d => d.ContainsKey("actor") && d["actor"] == actor),
                    It.IsAny<string>()))
                .ReturnsAsync(
                    new Octokit.Internal.ApiResponse<GitHubClientWorkflowRunsExtensions.ListWorkflowRunsResponse>(
                        new Mock<IResponse>().Object,
                        new GitHubClientWorkflowRunsExtensions.ListWorkflowRunsResponse { TotalCount = count }));
            return this;
        }
    }
}
