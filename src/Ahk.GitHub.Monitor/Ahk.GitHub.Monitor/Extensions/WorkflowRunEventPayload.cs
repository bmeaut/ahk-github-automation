using Octokit;

namespace Ahk.GitHub.Monitor.Extensions;

public class WorkflowRunEventPayload : ActivityPayload
{
    public string Action { get; set; }
}
