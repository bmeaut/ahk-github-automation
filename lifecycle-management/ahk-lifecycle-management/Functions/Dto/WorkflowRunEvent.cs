namespace Ahk.Lifecycle.Management
{
    public class WorkflowRunEvent
    {
        public WorkflowRunEvent(string repository, string username, string conclusion)
        {
            this.Repository = repository;
            this.Username = username;
            this.Conclusion = conclusion;
        }

        public string Repository { get; }
        public string Username { get; }

        // neutral, success, skipped, cancelled, timed_out, action_required, failure
        public string Conclusion { get; }
    }
}
