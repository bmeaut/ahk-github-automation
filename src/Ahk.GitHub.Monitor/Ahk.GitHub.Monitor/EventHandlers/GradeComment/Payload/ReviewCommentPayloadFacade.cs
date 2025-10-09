using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;

internal class ReviewCommentPayloadFacade(PullRequestReviewEventPayload payload)
    : ICommentPayload<PullRequestReviewEventPayload>
{
    public PullRequestReviewEventPayload Payload { get; } = payload;

    public Repository Repository => Payload.Repository;
    public int PullRequestNumber => Payload.PullRequest.Number;
    public string PullRequestUrl => Payload.PullRequest.Url;
    public string CommentingUser => Payload.Review.User.Login;
    public string CommentHtmlUrl => Payload.Review.HtmlUrl;
    public string CommentBody => Payload.Review.Body;
}
