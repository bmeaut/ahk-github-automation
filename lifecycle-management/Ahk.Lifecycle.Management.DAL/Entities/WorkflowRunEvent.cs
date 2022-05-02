namespace Ahk.Lifecycle.Management.DAL.Entities
{
    public class WorkflowRunEvent : LifecycleEvent
    {
        public WorkflowRunEvent(string id, string repository, string username, DateTime timestamp, string conclusion) : base(id, repository, username, timestamp)
        {
            this.Conclusion = conclusion;
        }

        public override string Type { get; } = "WorkflowRunEvent";
        public string Conclusion { get; }
    }
}
