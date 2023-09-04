using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class SubmissionInfoDTO
    {
        public string Repository { get; }
        public string Neptun { get; }
        public IReadOnlyCollection<string> Branches { get; }
        public IReadOnlyCollection<PullRequestStatusDTO> PullRequests { get; }
        public WorkflowRunsStatusDTO WorkflowRuns { get; }
        public string RepositoryUrl { get; }
        public string Grade { get; }
    }
}
