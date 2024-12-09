using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;

namespace Ahk.GradeManagement.ListGrades
{
    public class GradeListing : IGradeListing
    {
        private readonly IResultsRepository repo;

        public GradeListing(IResultsRepository repo)
            => this.repo = repo;

        public async Task<IReadOnlyCollection<FinalStudentGrade>> List(string repoPrefix)
        {
            var items = await this.repo.ListConfirmedWithRepositoryPrefix(Normalize.RepoName(repoPrefix));
            var finalResults = new List<FinalStudentGrade>();
            foreach (var student in items.GroupBy(r => new { Neptun = Normalize.Neptun(r.Neptun), r.GitHubRepoName }))
            {
                var lastResult = student.OrderByDescending(s => s.Date).First();
                finalResults.Add(new FinalStudentGrade(
                    neptun: student.Key.Neptun,
                    repo: lastResult.Key.GitHubRepoName,
                    prUrl: lastResult.GitHubPrUrl,
                    points: lastResult.Points == null ? new Dictionary<string, double>() : lastResult.Points.ToDictionary(keySelector: p => p.Name, elementSelector: p => p.Point)));
            }

            return finalResults;
        }
    }
}
