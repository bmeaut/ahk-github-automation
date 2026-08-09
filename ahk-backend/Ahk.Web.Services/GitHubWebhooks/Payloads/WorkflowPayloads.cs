using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks.Payloads;

/// <summary>
/// The <c>workflow_run</c> payload, as much of it as the run-count rule needs.
///
/// Octokit has no type for this event. <c>github-monitor</c> declared its equivalents inside
/// <c>namespace Octokit</c> so they looked native; that is a landmine for the next Octokit upgrade (the day
/// Octokit ships its own <c>WorkflowRunEventPayload</c>, the project stops compiling for reasons nobody will
/// connect to this file), so they live in our own namespace here. Octokit's <see cref="SimpleJsonSerializer"/>
/// binds by convention, not by namespace, so nothing else changes.
/// </summary>
public class WorkflowRunEventPayload : ActivityPayload
{
    public string? Action { get; set; }
}

/// <summary>The <c>workflow_run</c> payload including the run's conclusion, for the status event log.</summary>
public class WorkflowEventPayload : ActivityPayload
{
    public string? Action { get; set; }

    public WorkflowRun? WorkflowRun { get; set; }
}

/// <summary>An Actions workflow run, as much of it as the status projection needs.</summary>
public class WorkflowRun
{
    public string? Conclusion { get; set; }
}
