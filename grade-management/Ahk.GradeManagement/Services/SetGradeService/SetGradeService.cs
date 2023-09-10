using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Services;
using Ahk.GradeManagement.SetGrade;

namespace Ahk.GradeManagement.Services.SetGradeService
{
    public class SetGradeService : ISetGradeService
    {
        private readonly IGradeService service;

        public SetGradeService(IGradeService service) => this.service = service;

        public async Task SetGradeAsync(SetGradeEvent data)
        {
            var previousResults = await service.GetLastResultOfAsync(neptun: Normalize.Neptun(data.Neptun), gitHubRepoName: Normalize.RepoName(data.Repository), gitHubPrNumber: data.PrNumber);

            await service.AddGradeAsync(new Grade()
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
                IsConfirmed = true,
                Assignment = previousResults.Assignment,
                AssignmentId = previousResults.AssignmentId
            });
        }

        public async Task ConfirmAutoGradeAsync(ConfirmAutoGradeEvent data)
        {
            var previousResults = await service.GetLastResultOfAsync(neptun: Normalize.Neptun(data.Neptun), gitHubRepoName: Normalize.RepoName(data.Repository), gitHubPrNumber: data.PrNumber);
            await service.AddGradeAsync(new Grade()
            {
                Id = previousResults.Id,
                Student = previousResults.Student,
                StudentId = previousResults.StudentId,
                GithubPrNumber = previousResults.GithubPrNumber,
                GithubRepoName = previousResults.GithubRepoName,
                GithubPrUrl = previousResults.GithubPrUrl,
                Date = System.DateTimeOffset.UtcNow,
                Origin = previousResults.Origin,
                Points = previousResults?.Points,
                IsConfirmed = true,
                Assignment = previousResults.Assignment,
                AssignmentId = previousResults.AssignmentId
            });
        }

        private static List<Point> getPoints(double[] values, ICollection<Point> previousPoints)
        {
            var value = new List<Point>(capacity: values.Length);
            for (var i = 0; i < values.Length; i++)
            {
                var exercise = new Exercise() { Name = $"ex{i}" };
                if (previousPoints != null && previousPoints.Count > i)
                    exercise = previousPoints.ElementAt(i).Exercise;
                var p = new Point()
                {
                    PointEarned = values[i],
                    Exercise = exercise,
                };

                value.Add(p);
            }

            return value;
        }
    }
}
