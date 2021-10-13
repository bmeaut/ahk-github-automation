using Octokit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    internal class IssueCommentPayloadFacade : ICommentPayload<IssueCommentPayload>
    {
        public IssueCommentPayloadFacade(IssueCommentPayload payload)
            => this.Payload = payload;

        public IssueCommentPayload Payload { get; }
        
        public Repository Repository => Payload.Repository;
        public int PullRequestNumber => Payload.Issue.Number;
        public string CommentingUser => Payload.Comment.User.Login;
        public string CommentHtmlUrl => Payload.Comment.HtmlUrl;
        public string CommentBody => Payload.Comment.Body;
    }
}
