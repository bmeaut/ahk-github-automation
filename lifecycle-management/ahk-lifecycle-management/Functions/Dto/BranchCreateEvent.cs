namespace Ahk.Lifecycle.Management
{
    public class BranchCreateEvent
    {
        public BranchCreateEvent(string repository, string username, string branch)
        {
            this.Repository = repository;
            this.Username = username;
            this.Branch = branch;
        }

        public string Repository { get; }
        public string Username { get; }
        public string Branch { get; }
    }
}
