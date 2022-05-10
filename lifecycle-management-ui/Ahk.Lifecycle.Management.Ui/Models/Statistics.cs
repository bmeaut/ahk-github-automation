namespace Ahk.Lifecycle.Management.Ui.Models
{
    public class Statistics
    {
        public Statistics(int count, string repository, IReadOnlyCollection<LifecycleEvent> events)
        {
            this.Count = count;
            this.Repository = repository;
            this.Events = events;
        }

        public int Count { get; }
        public string Repository { get; }
        public IReadOnlyCollection<LifecycleEvent> Events { get; }
    }
}
