using Octokit;

namespace Ahk.GitHub.Monitor.EventHandlers
{
    public interface ICommentPayload<T>
        where T : ActivityPayload
    {
        public T Payload { get; }

        public Repository Repository { get; }
        public int PullRequestNumber { get; }
        public string CommentingUser { get; }
        string CommentHtmlUrl { get; }
        string CommentBody { get; }
    }
}
