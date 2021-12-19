using System.Linq;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.ResultProcessing.Dto;
using Ahk.GradeManagement.Services;

namespace Ahk.GradeManagement.ResultProcessing
{
    public class ResultProcessor : IResultProcessor
    {
        private readonly IResultsRepository repo;
        private readonly ITokenManagementService tokensService;

        public ResultProcessor(IResultsRepository repo, ITokenManagementService tokensService)
        {
            this.repo = repo;
            this.tokensService = tokensService;
        }

        public Task<string> GetSecretForToken(string token) => tokensService.GetSecretForToken(token);

        public Task ProcessResult(AhkProcessResult value, System.DateTime dateTime)
            => this.repo.AddResult(new StudentResult(
                    id: null,
                    neptun: value.NeptunCode,
                    gitHubRepoName: value.GitHubRepoName,
                    gitHubPrNumber: value.GitHubPullRequestNum,
                    gitHubPrUrl: value.GitHubPullRequestNum.HasValue ? $"https://github.com/{value.GitHubRepoName}/pull/{value.GitHubPullRequestNum}" : null,
                    date: dateTime,
                    actor: "grade-management-api",
                    origin: formatOrigin(value),
                    points: GetTotalPoints(value.Result),
                    confirmed: false));

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
