using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Services;

namespace Ahk.GradeManagement.ListGrades
{
    public class GradeListing : IGradeListing
    {
        private readonly IGradeService service;

        public GradeListing(IGradeService service)
            => this.service = service;

        public async Task<IReadOnlyCollection<FinalStudentGrade>> List(string repoPrefix)
        {
            var items = await this.service.ListConfirmedWithRepositoryPrefixAsync(Normalize.RepoName(repoPrefix));
            var finalResults = new List<FinalStudentGrade>();
            foreach (var student in items.GroupBy(r => Normalize.Neptun(r.Student.Neptun)))
            {
                var lastResult = student.OrderByDescending(s => s.Date).First();
                finalResults.Add(new FinalStudentGrade(
                    assignmentName: lastResult.Assignment.Name,
                    neptun: student.Key,
                    repo: lastResult.GithubRepoName,
                    prUrl: lastResult.GithubPrUrl.ToString(),
                    points: lastResult.Points == null ? new Dictionary<string, double>() : lastResult.Points.ToDictionary(keySelector: p => p.Exercise.Name, elementSelector: p => (double)p.PointEarned)));
            }

            return finalResults;
        }
    }
}
