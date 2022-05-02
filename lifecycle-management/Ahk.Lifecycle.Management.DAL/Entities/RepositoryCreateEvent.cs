namespace Ahk.Lifecycle.Management.DAL.Entities
{
    public class RepositoryCreateEvent : LifecycleEvent
    {
        public RepositoryCreateEvent(string id, string repository, string username, DateTime timestamp) : base(id, repository, username, timestamp)
        {
        }

        public override string Type { get; } = "RepositoryCreateEvent";
    }
}
