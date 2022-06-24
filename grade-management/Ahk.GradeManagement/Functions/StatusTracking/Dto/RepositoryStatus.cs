using System.Collections.Generic;

namespace Ahk.GradeManagement.StatusTracking
{
    public class RepositoryStatus
    {
        public RepositoryStatus(string repository, IReadOnlyCollection<string> branches, IReadOnlyCollection<PullRequestStatus> pullRequests, WorkflowRunsStatus workflowRuns)
        {
            this.Repository = repository;
            this.Branches = branches;
            this.PullRequests = pullRequests;
            this.WorkflowRuns = workflowRuns;
        }

        public string Repository { get; }
        public IReadOnlyCollection<string> Branches { get; }
        public IReadOnlyCollection<PullRequestStatus> PullRequests { get; }
        public WorkflowRunsStatus WorkflowRuns { get; }
    }
}
