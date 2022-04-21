namespace Ahk.Lifecycle.Management
{
    public class PullRequestEvent
    {
        public PullRequestEvent(string repository, string username, string action, string assignee, string neptun)
        {
            this.Repository = repository;
            this.Username = username;
            this.Action = action;
            this.Assignee = assignee;
            this.Neptun = neptun;
        }

        public string Repository { get; }
        public string Username { get; }
        public string Action { get; }
        public string Assignee { get; }
        public string Neptun { get; }
    }
}
