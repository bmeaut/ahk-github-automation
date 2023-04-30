using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.SetGrade
{
    public class SetGradeService : ISetGradeService
    {
        private readonly IGradeRepository repo;

        public SetGradeService(IGradeRepository repo) => this.repo = repo;

        public async Task SetGrade(SetGradeEvent data)
        {
            var previousResults = await this.repo.GetLastResultOf(neptun: Normalize.Neptun(data.Neptun), gitHubRepoName: Normalize.RepoName(data.Repository), gitHubPrNumber: data.PrNumber);

            await this.repo.AddGrade(new Grade()
            {
                Id = previousResults.Id,
                Student = previousResults.Student,
                StudentId = previousResults.StudentId,
                GithubRepoName = previousResults.GithubRepoName,
                GithubPrNumber = previousResults.GithubPrNumber,
                GithubPrUrl = previousResults.GithubPrUrl,
                Date = previousResults.Date,
                Origin = previousResults.Origin,
                Points = getPoints(data.Results, previousResults?.Points),
                Confirmed = true,
                Assignment = previousResults.Assignment,
                AssignmentId = previousResults.AssignmentId                
            });
        }

        public async Task ConfirmAutoGrade(ConfirmAutoGradeEvent data)
        {
            var previousResults = await this.repo.GetLastResultOf(neptun: Normalize.Neptun(data.Neptun), gitHubRepoName: Normalize.RepoName(data.Repository), gitHubPrNumber: data.PrNumber);
            await this.repo.AddGrade(new Grade()
            {
                Id = previousResults.Id,
                Student = previousResults.Student,
                StudentId = previousResults.StudentId,
                GithubPrNumber = previousResults.GithubPrNumber,
                GithubRepoName = previousResults.GithubRepoName,
                GithubPrUrl = previousResults.GithubPrUrl,
                Date = System.DateTime.UtcNow,
                Origin = previousResults.Origin,
                Points = previousResults?.Points,
                Confirmed = true,
                Assignment = previousResults.Assignment,
                AssignmentId = previousResults.AssignmentId
            });
        }

        private static List<Point> getPoints(double[] values, ICollection<Point> previousPoints)
        {
            var value = new List<Point>(capacity: values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                var exercise = new Exercise() { Name = $"ex{i}" };
                if (previousPoints != null && previousPoints.Count > i)
                    exercise = previousPoints.ElementAt(i).Exercise;
                var p = new Point()
                {
                    PointEarned = (int)values[i],
                    Exercise = exercise,
                };

                value.Add(p);
            }

            return value;
        }
    }
}
