namespace Ahk.Lifecycle.Management.DAL.Entities
{
    public class SetGradeEvent : LifecycleEvent
    {
        public SetGradeEvent(string id, string repository, string username, DateTime timestamp) : base(id, repository, username, timestamp)
        {
        }

        public override string Type { get; } = "SetGradeEvent";
    }
}
