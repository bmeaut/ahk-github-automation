using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;

internal class IssueCommentPayloadFacade(IssueCommentPayload payload) : ICommentPayload<IssueCommentPayload>
{
    public IssueCommentPayload Payload { get; } = payload;

    public Repository Repository => Payload.Repository;
    public int PullRequestNumber => Payload.Issue.Number;
    public string PullRequestUrl => Payload.Issue.Url;
    public string CommentingUser => Payload.Comment.User.Login;
    public string CommentHtmlUrl => Payload.Comment.HtmlUrl;
    public string CommentBody => Payload.Comment.Body;
}
