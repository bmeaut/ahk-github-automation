using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload
{
    internal class ReviewCommentPayloadFacade(PullRequestReviewEventPayload payload)
        : ICommentPayload<PullRequestReviewEventPayload>
    {
        public PullRequestReviewEventPayload Payload { get; } = payload;

        public Repository Repository => this.Payload.Repository;
        public int PullRequestNumber => this.Payload.PullRequest.Number;
        public string PullRequestUrl => this.Payload.PullRequest.Url;
        public string CommentingUser => this.Payload.Review.User.Login;
        public string CommentHtmlUrl => this.Payload.Review.HtmlUrl;
        public string CommentBody => this.Payload.Review.Body;
    }
}
