using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Models
{
    public class SubmissionInfo
    {
        public SubmissionInfo(string repository, string neptun, IReadOnlyCollection<string> branches, IReadOnlyCollection<PullRequestStatus> pullRequests, WorkflowRunsStatus workflowRuns)
        {
            this.Repository = repository;
            this.Neptun = neptun;
            this.Branches = branches;
            this.PullRequests = pullRequests;
            this.WorkflowRuns = workflowRuns;
        }

        public string Repository { get; }
        public string Neptun { get; }
        public IReadOnlyCollection<string> Branches { get; }
        public IReadOnlyCollection<PullRequestStatus> PullRequests { get; }
        public WorkflowRunsStatus WorkflowRuns { get; }
    }
}
