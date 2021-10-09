using Ahk.GradeManagement.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.ListGrades
{
    public class GradeListing : IGradeListing
    {
        private readonly IResultsRepository repo;

        public GradeListing(IResultsRepository repo)
            => this.repo = repo;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Repo name is normalized to lowercase.")]
        public async Task<IReadOnlyCollection<FinalStudentGrade>> List(string repoPrefix)
        {
            var items = await this.repo.ListConfirmedWithRepositoryPrefix(repoPrefix.ToLowerInvariant());
            var finalResults = new List<FinalStudentGrade>();
            foreach (var student in items.GroupBy(r => r.Neptun.ToUpperInvariant()))
            {
                var lastResult = student.OrderByDescending(s => s.Date).First();
                finalResults.Add(new FinalStudentGrade(
                    neptun: student.Key,
                    repo: lastResult.GitHubRepoName,
                    prUrl: lastResult.GitHubPrUrl,
                    points: lastResult.Points.ToDictionary(keySelector: p => p.Name, elementSelector: p => p.Point)));
            }

            return finalResults;
        }
    }
}
