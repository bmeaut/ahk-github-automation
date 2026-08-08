using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers.GradeComment;

/// <summary>
/// The parts of a comment the grading command needs, regardless of whether it arrived as an issue comment or
/// as a pull request review. Lets one base class serve both events.
/// </summary>
public interface ICommentPayload<out T>
    where T : ActivityPayload
{
    T Payload { get; }

    Repository Repository { get; }

    int PullRequestNumber { get; }

    string CommentingUser { get; }

    string CommentHtmlUrl { get; }

    string CommentBody { get; }
}

internal sealed class IssueCommentPayloadFacade : ICommentPayload<IssueCommentPayload>
{
    public IssueCommentPayloadFacade(IssueCommentPayload payload) => this.Payload = payload;

    public IssueCommentPayload Payload { get; }

    public Repository Repository => Payload.Repository;

    public int PullRequestNumber => Payload.Issue.Number;

    public string CommentingUser => Payload.Comment.User.Login;

    public string CommentHtmlUrl => Payload.Comment.HtmlUrl;

    public string CommentBody => Payload.Comment.Body;
}

internal sealed class ReviewCommentPayloadFacade : ICommentPayload<PullRequestReviewEventPayload>
{
    public ReviewCommentPayloadFacade(PullRequestReviewEventPayload payload) => this.Payload = payload;

    public PullRequestReviewEventPayload Payload { get; }

    public Repository Repository => Payload.Repository;

    public int PullRequestNumber => Payload.PullRequest.Number;

    public string CommentingUser => Payload.Review.User.Login;

    public string CommentHtmlUrl => Payload.Review.HtmlUrl;

    public string CommentBody => Payload.Review.Body;
}
