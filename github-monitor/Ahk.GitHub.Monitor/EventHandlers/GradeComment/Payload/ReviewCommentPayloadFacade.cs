using Octokit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    internal class ReviewCommentPayloadFacade : ICommentPayload<PullRequestReviewEventPayload>
    {
        public ReviewCommentPayloadFacade(PullRequestReviewEventPayload payload)
            => this.Payload = payload;

        public PullRequestReviewEventPayload Payload { get; }
        
        public Repository Repository => Payload.Repository;
        public int PullRequestNumber => Payload.PullRequest.Number;
        public string CommentingUser => Payload.Review.User.Login;
        public string CommentHtmlUrl => Payload.Review.HtmlUrl;
        public string CommentBody => Payload.Review.Body;
    }
}
