using DTOs;

namespace Ahk.Review.Ui
{ 

    public class SubmissionInfo
    {
        public SubmissionInfo(string AssignmentName, string Repository, string Neptun,
                              IReadOnlyCollection<string> Branches, IReadOnlyCollection<PullRequestStatusDTO> PullRequests,
                              WorkflowRunsStatusDTO WorkflowRuns, IReadOnlyDictionary<string, double>? points, IReadOnlyCollection<StatusEventBaseDTO> Events)
        {
            this.AssignmentName = AssignmentName;
            this.Repository = Repository;
            this.Neptun = Neptun;
            this.Branches = Branches;
            this.PullRequests = PullRequests;
            this.WorkflowRuns = WorkflowRuns;
            this.Grade = getGradeAsString(points);
            this.RepositoryUrl = $"https://github.com/{Repository}";
            this.Events = Events;
        }

        public string AssignmentName { get; }
        public string Repository { get; }
        public string Neptun { get; }
        public IReadOnlyCollection<string> Branches { get; }
        public IReadOnlyCollection<PullRequestStatusDTO> PullRequests { get; }
        public WorkflowRunsStatusDTO WorkflowRuns { get; }
        public string RepositoryUrl { get; }
        public string Grade { get; }
        public IReadOnlyCollection<StatusEventBaseDTO> Events { get; }

        private static string getGradeAsString(IReadOnlyDictionary<string, double>? points)
        {
            if (points is null)
                return string.Empty;

            return string.Join(" ", points.Values);
        }
    }
}
