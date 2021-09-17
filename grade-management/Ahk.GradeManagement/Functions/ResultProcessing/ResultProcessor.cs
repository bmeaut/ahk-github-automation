using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.ResultProcessing.Dto;
using Ahk.GradeManagement.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.ResultProcessing
{
    public class ResultProcessor : IResultProcessor
    {
        private readonly AhkDb db;
        private readonly ITokenManagementService tokensService;

        public ResultProcessor(AhkDb db, ITokenManagementService tokensService)
        {
            this.db = db;
            this.tokensService = tokensService;
        }

        public Task<string> GetSecretForToken(string token) => tokensService.GetSecretForToken(token);

        public async Task ProcessResult(AhkProcessResult value)
        {
            await this.db.EnsureCreated();
            this.db.Results.Add(StudentResult.Create(
                neptun: value.NeptunCode,
                repository: value.GitHubRepoName,
                prNum: value.GitHubPullRequestNum,
                prUrl: value.GitHubPullRequestNum.HasValue ? $"https://github.com/{value.GitHubRepoName}/pull/{value.GitHubPullRequestNum}" : null,
                actor: "grade-management-api",
                origin: formatOrigin(value),
                points: GetTotalPoints(value.Result)));
            await this.db.SaveChangesAsync();
        }

        internal static System.Collections.Generic.List<ExerciseWithPoint> GetTotalPoints(AhkTaskResult[] value)
        {
            if (value is null)
                return null;

            return value.GroupBy(r => string.IsNullOrEmpty(r.ExerciseName) ? string.Empty : r.ExerciseName)
                        .Select(g => new ExerciseWithPoint() { Name = g.Key, Point = g.Sum(r => r.Points) })
                        .OrderBy(x => x.Name)
                        .ToList();
        }

        private static string formatOrigin(AhkProcessResult value)
            => string.IsNullOrEmpty(value.Origin) ? $"https://github.com/{value.GitHubRepoName}/commit/{value.GitHubCommitHash}" : value.Origin;
    }
}
