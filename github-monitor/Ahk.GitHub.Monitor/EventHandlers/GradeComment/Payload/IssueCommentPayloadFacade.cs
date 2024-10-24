using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;

internal class IssueCommentPayloadFacade(IssueCommentPayload payload) : ICommentPayload<IssueCommentPayload>
{
    public IssueCommentPayload Payload { get; } = payload;

    public Repository Repository => this.Payload.Repository;
    public int PullRequestNumber => this.Payload.Issue.Number;
    public string PullRequestUrl => this.Payload.Issue.Url;
    public string CommentingUser => this.Payload.Comment.User.Login;
    public string CommentHtmlUrl => this.Payload.Comment.HtmlUrl;
    public string CommentBody => this.Payload.Comment.Body;
}
