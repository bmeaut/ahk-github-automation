namespace Ahk.GradeManagement.StatusTracking
{
    public class PullRequestStatus
    {
        public PullRequestStatus(int number, string status, string assignee)
        {
            this.Number = number;
            this.Status = status;
            this.Assignee = assignee;
        }

        public int Number { get; }
        public string Status { get; }
        public string Assignee { get; }
    }
}
