namespace Ahk.Lifecycle.Management.DAL.Entities
{
    public class PullRequestEvent : LifecycleEvent
    {
        public PullRequestEvent(string id, string repository, string username, DateTime timestamp, string action, IReadOnlyCollection<string> assignees, string neptun) : base(id, repository, username, timestamp)
        {
            this.Action = action;
            this.Assignees = assignees;
            this.Neptun = neptun;
        }

        public override string Type { get; } = "PullRequestEvent";
        public string Action { get; }
        public IReadOnlyCollection<string> Assignees { get; }
        public string Neptun { get; }
    }
}
