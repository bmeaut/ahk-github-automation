using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.SetGrade
{
    public class SetGradeService : ISetGradeService
    {
        private readonly IResultsRepository repo;

        public SetGradeService(IResultsRepository repo)
            => this.repo = repo;

        public async Task SetGrade(SetGradeEvent data)
        {
            var previousResults = await this.repo.GetLastResultOf(neptun: Normalize.Neptun(data.Neptun), gitHubRepoName: Normalize.RepoName(data.Repository), gitHubPrNumber: data.PrNumber);
            await this.repo.AddResult(new StudentResult(
                id: null,
                neptun: data.Neptun,
                gitHubRepoName: data.Repository,
                gitHubPrNumber: data.PrNumber,
                gitHubPrUrl: data.PrUrl,
                date: System.DateTime.UtcNow,
                actor: data.Actor,
                origin: data.Origin,
                points: getPoints(data.Results, previousResults?.Points),
                confirmed: true));
        }

        public async Task ConfirmAutoGrade(ConfirmAutoGradeEvent data)
        {
            var previousResults = await this.repo.GetLastResultOf(neptun: Normalize.Neptun(data.Neptun), gitHubRepoName: Normalize.RepoName(data.Repository), gitHubPrNumber: data.PrNumber);
            await this.repo.AddResult(new StudentResult(
                id: null,
                neptun: data.Neptun,
                gitHubRepoName: data.Repository,
                gitHubPrNumber: data.PrNumber,
                gitHubPrUrl: data.PrUrl,
                date: System.DateTime.UtcNow,
                actor: data.Actor,
                origin: data.Origin,
                points: previousResults?.Points,
                confirmed: true));
        }

        private static List<ExerciseWithPoint> getPoints(double[] values, ICollection<ExerciseWithPoint> previousPoints)
        {
            var value = new List<ExerciseWithPoint>(capacity: values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                var name = $"ex{i}";
                if (previousPoints != null && previousPoints.Count > i)
                    name = previousPoints.ElementAt(i).Name;

                value.Add(new ExerciseWithPoint() { Name = name, Point = values[i] });
            }

            return value;
        }
    }
}
