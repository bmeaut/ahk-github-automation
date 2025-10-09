using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers.GradeComment.Payload;

public interface ICommentPayload<T>
    where T : ActivityPayload
{
    public T Payload { get; }

    public Repository Repository { get; }
    public int PullRequestNumber { get; }
    public string PullRequestUrl { get; }
    public string CommentingUser { get; }
    public string CommentHtmlUrl { get; }
    public string CommentBody { get; }
}
