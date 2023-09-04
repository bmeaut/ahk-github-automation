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
        private readonly IGradeService service;
        private readonly ITokenManagementService tokensService;

        public ResultProcessor(IGradeService service, ITokenManagementService tokensService)
        {
            this.service = service;
            this.tokensService = tokensService;
        }

        public Task<string> GetSecretForTokenAsync(string token) => tokensService.GetSecretForTokenAsync(token);

        public Task ProcessResultAsync(AhkProcessResult value, System.DateTime dateTime)
            => this.service.AddGradeAsync(new Grade()
            {
                Student = service.FindStudentAsync(value.NeptunCode),
                GithubRepoName = value.GitHubRepoName,
                GithubPrNumber = (int)value.GitHubPullRequestNum,
                GithubPrUrl = new Uri(value.GitHubPullRequestNum.HasValue ? $"https://github.com/{value.GitHubRepoName}/pull/{value.GitHubPullRequestNum}" : null),
                Date = dateTime,
                //Actor = "grade-management-api",
                Origin = formatOrigin(value),
                Points = GetTotalPoints(value.Result),
                IsConfirmed = false,
                Assignment = service.FindAssignment(value.NeptunCode)
            });

        internal static System.Collections.Generic.List<Point> GetTotalPoints(AhkTaskResult[] value)
        {
            if (value is null)
                return null;

            return value.GroupBy(r => string.IsNullOrEmpty(r.ExerciseName) ? string.Empty : r.ExerciseName)
                        .Select(g => new Point() { Exercise = new Exercise() { Name = g.Key }, PointEarned = g.Sum(r => r.Points) })
                        .OrderBy(x => x.Exercise.Name)
                        .ToList();
        }

        private static string formatOrigin(AhkProcessResult value)
            => string.IsNullOrEmpty(value.Origin) ? $"https://github.com/{value.GitHubRepoName}/commit/{value.GitHubCommitHash}" : value.Origin;
    }
}
