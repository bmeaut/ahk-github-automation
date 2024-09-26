using Octokit;
using PublishResult.Processing;

namespace PublishResult.PublishToPr;

public static class PrPublisher
{
    public static async Task PublishToPrAsync(AppArgs.AppArgs appArgs, AhkProcessResult result)
    {
        var github = new GitHubClient(new ProductHeaderValue("MyApp"));
        var token = appArgs.GitHubToken;
        github.Credentials = new Credentials(token);

        var owner = appArgs.GitHubRepoOwner;
        var repo = appArgs.GitHubRepoName;
        var prNumber = appArgs.GitHubPullRequestNum;

        var newComment = CommentFormatter.CreateComment(appArgs, result);

        var comment = await github.Issue.Comment.Create(owner, repo, prNumber, newComment);

        Console.WriteLine($"Sending comment to {owner}/{repo} PR {prNumber}");
        Console.WriteLine($"Created comment with id {comment?.Id} at {comment?.HtmlUrl}");
    }
}
