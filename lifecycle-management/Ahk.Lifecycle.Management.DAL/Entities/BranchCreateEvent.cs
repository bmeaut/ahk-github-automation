namespace Ahk.Lifecycle.Management.DAL.Entities
{
    public class BranchCreateEvent : LifecycleEvent
    {
        public BranchCreateEvent(string id, string repository, string username, DateTime timestamp, string branch) : base(id, repository, username, timestamp)
        {
            this.Branch = branch;
        }

        public override string Type { get; } = "BranchCreateEvent";
        public string Branch { get; }
    }
}
