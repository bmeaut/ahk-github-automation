namespace Ahk.GradeManagement.Data.Models
{
    public class WorkflowRunsStatus
    {
        public WorkflowRunsStatus(int count, string lastStatus)
        {
            this.Count = count;
            this.LastStatus = lastStatus;
        }

        public int Count { get; }
        public string LastStatus { get; }
    }
}
