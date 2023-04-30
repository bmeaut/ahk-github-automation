using System;
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
        private readonly IGradeRepository repo;
        private readonly ITokenManagementService tokensService;

        public ResultProcessor(IGradeRepository repo, ITokenManagementService tokensService)
        {
            this.repo = repo;
            this.tokensService = tokensService;
        }

        public Task<string> GetSecretForToken(string token) => tokensService.GetSecretForToken(token);

        public Task ProcessResult(AhkProcessResult value, System.DateTime dateTime)
            => this.repo.AddGrade(new Grade()
               {
                    Student = repo.Context.Students.Where(s => s.Neptun == value.NeptunCode).FirstOrDefault(),
                    GithubRepoName = value.GitHubRepoName,
                    GithubPrNumber = (int)value.GitHubPullRequestNum,
                    GithubPrUrl = new Uri(value.GitHubPullRequestNum.HasValue ? $"https://github.com/{value.GitHubRepoName}/pull/{value.GitHubPullRequestNum}" : null),
                    Date = dateTime,
                    //Actor = "grade-management-api",
                    Origin = new Uri(formatOrigin(value)),
                    Points = GetTotalPoints(value.Result),
                    Confirmed = false
                });

        internal static System.Collections.Generic.List<Point> GetTotalPoints(AhkTaskResult[] value)
        {
            if (value is null)
                return null;

            return value.GroupBy(r => string.IsNullOrEmpty(r.ExerciseName) ? string.Empty : r.ExerciseName)
                        .Select(g => new Point() { Exercise = new Exercise() { Name = g.Key }, PointEarned = (int)g.Sum(r => r.Points) })
                        .OrderBy(x => x.Exercise.Name)
                        .ToList();
        }

        private static string formatOrigin(AhkProcessResult value)
            => string.IsNullOrEmpty(value.Origin) ? $"https://github.com/{value.GitHubRepoName}/commit/{value.GitHubCommitHash}" : value.Origin;
    }
}
