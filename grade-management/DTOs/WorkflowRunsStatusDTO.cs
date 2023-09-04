namespace DTOs
{
    public class WorkflowRunsStatusDTO
    {
        public WorkflowRunsStatusDTO(int count, string lastStatus)
        {
            this.Count = count;
            this.LastStatus = lastStatus;
        }

        public int Count { get; }
        public string LastStatus { get; }
    }
}
