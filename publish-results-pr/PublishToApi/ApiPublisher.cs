using System.Text.Json;

using PublishResult.AppArgs;
using PublishResult.Processing;

namespace PublishResult.PublishToApi;

public static class ApiPublisher
{
    private static readonly JsonSerializerOptions writeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void PublishToApi(AppArgs.AppArgs appArgs, AhkProcessResult result)
    {
        var githubRepositoryUrl = $"https://github.com/{appArgs.GitHubRepoFullName}";
        var pullrequestUrl = $"https://github.com/{appArgs.GitHubRepoOwner}/{appArgs.GitHubRepoName}/pull/{appArgs.GitHubPullRequestNum}";
        var neptun = result.NeptunCode;
        var scores = new List<EventScore>();

        var exercises = JsonSerializer.Deserialize<Exercise[]>(result.Result);
        var createdDate = DateTimeOffset.Now;

        if (exercises is not null && exercises.Length != 0)
        {
            foreach (var exercise in exercises)
            {
                scores.Add(new EventScore()
                {
                    Value = exercise.GivenPoint,
                    CreatedDate = createdDate,
                    ScoreType = exercise.ExerciseName,
                });
            }
        }

        var resultToApi = new ResultForApi()
        {
            GithubRepositoryUrl = githubRepositoryUrl,
            PullRequestUrl = pullrequestUrl,
            StudentNeptun = neptun,
            Scores = scores,
        };

        var json = JsonSerializer.Serialize(resultToApi, writeOptions);

        // TODO: send json to backend
    }
}
