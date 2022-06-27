namespace Ahk.Review.Ui.Models
{
    public class SubmissionInfo : RepositoryStatus
    {
        public SubmissionInfo(RepositoryStatus r, IReadOnlyDictionary<string, double>? points)
            : base(r.Repository, r.Neptun, r.Branches, r.PullRequests, r.WorkflowRuns)
        {
            this.Grade = getGradeAsString(points);
            this.RepositoryUrl = $"https://github.com/{r.Repository}";
        }

        public string RepositoryUrl { get; }
        public string Grade { get; }

        private static string getGradeAsString(IReadOnlyDictionary<string, double>? points)
        {
            if (points is null)
                return string.Empty;

            return string.Join(" ", points.Values);
        }
    }
}
