using System;
using System.Text;
using Octokit;

namespace Ahk.GitHub.Monitor.Tests.UnitTests.EventHandlersTests.Helpers;

internal static class GitHubMockData
{
    private const string AhkMonitorYamlContent = @"enabled: true";
    private const string NeptunTxtContent = @"ABC123";

    public static PullRequest CreatePullRequest(int number, ItemState state, int userId = 111, bool? mergeable = null)
        => new(
            number,
            string.Empty,
            string.Empty,
            $"https://www.github.com/org1/repo1/pull/{number}",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            number,
            state,
            string.Empty,
            string.Empty,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            null,
            new GitReference("121sdad23", "", "", "branch", "aaaaa1111", null, null),
            null,
            CreateUser(userId),
            null,
            null,
            false,
            mergeable,
            null,
            null,
            string.Empty,
            0,
            0,
            0,
            0,
            0,
            null,
            false,
            null,
            null,
            null,
            null,
            null);

    public static RepositoryContent CreateRepositoryFileContent(string filePath, string content)
        => new(
            System.IO.Path.GetFileName(filePath),
            filePath,
            "dummy", 123, ContentType.File,
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
            string.Empty, string.Empty);

    public static RepositoryContent CreateAhkMonitorYamlFileContent(string content = null)
        => CreateRepositoryFileContent(".github/ahk-monitor.yml", content ?? AhkMonitorYamlContent);

    public static RepositoryContent CreateNeptunTxtFileContent(string content = null)
        => CreateRepositoryFileContent("neptun.txt", content ?? NeptunTxtContent);

    public static IssueEvent CreateIssueEvent(EventInfoState @event, int actorId)
        => new(
            1234,
            string.Empty,
            string.Empty,
            CreateUser(actorId),
            null,
            null,
            @event,
            null,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            dismissedReview: null,
            milestone: null,
            lockReason: LockReason.Resolved);

    public static User CreateUser(int actorId)
        => new(
            string.Empty, string.Empty, string.Empty, 0, string.Empty,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, string.Empty,
            0, 0, null, string.Empty, 0,
            actorId, string.Empty, string.Empty, string.Empty, string.Empty,
            0, null, 0, 0, 0, string.Empty,
            null, false, string.Empty, null);
}
