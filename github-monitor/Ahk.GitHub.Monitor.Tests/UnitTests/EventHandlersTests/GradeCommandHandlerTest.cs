using Ahk.GitHub.Monitor.EventHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests
{
    [TestClass]
    public class GradeCommandHandlerTest
    {
        [TestMethod]
        public async Task CommentNoIssueInPayloadIgnored()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var payload = SampleData.CommentEdit
                .Replace("\"issue\": {", "\"aaaaa\": {");

            var eh = new GradeCommandHandler(gitHubMock.CreateFactory(), GradeStoreMockFactory.Default, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(payload);

            Assert.IsTrue(result.Result.Contains("no issue information"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Get(It.IsAny<long>(), It.IsAny<int>()),
                Times.Never());
        }

        [TestMethod]
        public async Task CommentActionIsNotCreated()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();

            var eh = new GradeCommandHandler(gitHubMock.CreateFactory(), GradeStoreMockFactory.Default, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(SampleData.CommentDelete);

            Assert.IsTrue(result.Result.Contains("not of interest"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Get(It.IsAny<long>(), It.IsAny<int>()),
                Times.Never());
        }

        [DataTestMethod]
        [DataRow("aaaa")]
        [DataRow("/ahk hello")]
        [DataRow("/ahk okkkkkkk")]
        [DataRow("/ahkkkkk ok")]
        [DataRow("a/ahk ok")]
        public async Task CommandNotRecognized(string commentText)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault();
            var gradeStoreMock = GradeStoreMockFactory.Create();

            var eh = new GradeCommandHandler(gitHubMock.CreateFactory(), gradeStoreMock.Object, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(getPayloadWithComment(commentText));

            Assert.IsTrue(result.Result.Contains("not recognized as command"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Get(It.IsAny<long>(), It.IsAny<int>()),
                Times.Never());
            gradeStoreMock.Verify(c =>
                c.StoreGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<double>>()),
                Times.Never());
        }

        [TestMethod]
        public async Task ActorIsNotOrgMemberCommandNotAllowed()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithOrganizationMemberGet("abcabc", false);
            var gradeStoreMock = GradeStoreMockFactory.Create();

            var eh = new GradeCommandHandler(gitHubMock.CreateFactory(), gradeStoreMock.Object, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(getPayloadWithComment(@"/ahk ok"));

            Assert.IsTrue(result.Result.Contains("not allowed for user"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Merge(336882879, 24, It.IsAny<Octokit.MergePullRequest>()),
                Times.Never());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.Organization.Member.CheckMember("org1", "abcabc"),
                Times.Once());
            gradeStoreMock.Verify(c =>
                c.StoreGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<double>>()),
                Times.Never());
        }

        [TestMethod]
        public async Task PRAlreadyClosedGradesAccepted()
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithOrganizationMemberGet("abcabc", true)
                                .WithPullRequestGet(336882879, 24, GitHubMockData.CreatePullRequest(24, Octokit.ItemState.Closed, mergeable: false));
            var gradeStoreMock = GradeStoreMockFactory.Create();

            var eh = new GradeCommandHandler(gitHubMock.CreateFactory(), gradeStoreMock.Object, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(getPayloadWithComment(@"/ahk ok"));

            Assert.IsTrue(result.Result.Contains("grade done"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Merge(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<Octokit.MergePullRequest>()),
                Times.Never());
            gradeStoreMock.Verify(c =>
                c.StoreGrade(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyCollection<double>>()),
                Times.Never());
        }

        [DataTestMethod]
        [DataRow("/ahk ok", false)]
        [DataRow("/ahk ok\rsomething", false)]
        [DataRow("/ahk ok\r\nsomething", false)]
        [DataRow("something\r\n\r\n/ahk ok", false)]
        [DataRow("/ahk ok 1", true)]
        [DataRow("/ahk ok 2\rsomething", true)]
        [DataRow("/ahk ok 3\r\nsomething", true)]
        [DataRow("something\r\n\r\n/ahk ok 4", true)]
        [DataRow("/ahk ok 1 2", true)]
        [DataRow("/ahk ok 2 3.5\rsomething", true)]
        [DataRow("/ahk ok 3.4 5\r\nsomething", true)]
        [DataRow("something\r\n\r\n/ahk ok 4 5.6", true)]
        public async Task PRNeedsMergeGradesAccepted(string commentText, bool gradesExpected)
        {
            var gitHubMock = GitHubClientMockFactory.CreateDefault()
                                .WithOrganizationMemberGet("abcabc", true)
                                .WithPullRequestGet(336882879, 24, GitHubMockData.CreatePullRequest(24, Octokit.ItemState.Open, mergeable: true))
                                .WithNeptunTxtContent("NEPT12");
            var gradeStoreMock = GradeStoreMockFactory.Create();

            var eh = new GradeCommandHandler(gitHubMock.CreateFactory(), gradeStoreMock.Object, MemoryCacheMockFactory.Instance);
            var result = await eh.Execute(getPayloadWithComment(commentText));

            Assert.IsTrue(result.Result.Contains("grade done"));
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Review.Create(336882879, 24, It.IsAny<Octokit.PullRequestReviewCreate>()),
                Times.Once());
            gitHubMock.GitHubClientMock.Verify(c =>
                c.PullRequest.Merge(336882879, 24, It.IsAny<Octokit.MergePullRequest>()),
                Times.Once());
            gradeStoreMock.Verify(c =>
                c.StoreGrade("NEPT12", "org1/repo1", 24, "https://www.github.com/org1/repo1/pull/24", It.IsAny<string>(), @"https://github.com/org1/repo1/pull/1#issuecomment-821112111", It.IsAny<IReadOnlyCollection<double>>()),
                gradesExpected ? Times.Once() : Times.Never());
        }

        private static string getPayloadWithComment(string value, string user = "abcabc")
            => SampleData.CommentCommand.Replace("xxx-body-placeholder-xxx", value).Replace("xxx-comment-creator-user-xxx", user);
    }
}
