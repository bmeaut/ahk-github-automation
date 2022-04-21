namespace Octokit
{
    public class WorkflowEventPayload : ActivityPayload
    {
        public string Action { get; set; }
        public WorkflowRun WorkflowRun { get; set; }
    }
}
