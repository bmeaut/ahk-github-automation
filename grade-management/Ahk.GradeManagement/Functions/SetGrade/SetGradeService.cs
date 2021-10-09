using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.SetGrade
{
    public class SetGradeService : ISetGradeService
    {
        private readonly IResultsRepository repo;

        public SetGradeService(IResultsRepository repo)
            => this.repo = repo;

        public Task SetGrade(SetGradeEvent data)
            => this.repo.AddResult(new StudentResult(
                id: null,
                neptun: data.Neptun,
                gitHubRepoName: data.Repository,
                gitHubPrNumber: data.PrNumber,
                gitHubPrUrl: data.PrUrl,
                date: System.DateTime.UtcNow,
                actor: data.Actor,
                origin: data.Origin,
                points: getPoints(data.Results),
                confirmed: true));

        public async Task ConfirmAutoGrade(ConfirmAutoGradeEvent data)
        {
            var previousResults = await this.repo.GetLastResultOf(neptun: data.Neptun, gitHubRepoName: data.Repository, gitHubPrNumber: data.PrNumber);
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

        private static List<ExerciseWithPoint> getPoints(double[] values)
        {
            var value = new List<ExerciseWithPoint>(capacity: values.Length);
            for (int i = 0; i < values.Length; i++)
                value.Add(new ExerciseWithPoint() { Name = $"ex{i}", Point = values[i] });
            return value;
        }
    }
}
