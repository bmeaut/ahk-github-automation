using PublishResult.AppArgs;
using PublishResult.Processing;

using System.Text.Json;

namespace PublishResult.PublishToPr;

public static class CommentFormatter
{
    public static string CreateComment(AppArgs.AppArgs appArgs, AhkProcessResult result)
    {
        var comment = "";

        comment += AddImages(appArgs, result);
        comment += "\n\n";
        comment += $"**Neptun**: {result.NeptunCode}";
        comment += "\n\n";
        comment += AddTasks(result);
        comment += "\n\n";
        comment += AddSummary(result);

        return comment;
    }

    private static string AddImages(AppArgs.AppArgs appArgs, AhkProcessResult result)
    {
        var str = "";

        if (result.ImageFiles?.Length > 0)
        {
            foreach (var image in result.ImageFiles)
            {
                str += "**" + image + "**" + "\n";
                str += $"![](https://github.com/{appArgs.GitHubRepoOwner}/{appArgs.GitHubRepoName}/blob/{appArgs.GitCommitHash}/{image}?raw=true)\n\n";
            }
        }

        return str;
    }

    private static string AddTasks(AhkProcessResult result)
    {
        var str = "";
        var exercises = JsonSerializer.Deserialize<Exercise[]>(result.Result);
        if (exercises is not null && exercises.Length != 0)
        {
            foreach (var exercise in exercises)
            {
                str += exercise.ToString() + "\n";
            }
        }

        return str;
    }

    private static string AddSummary(AhkProcessResult result)
    {
        var maxPoint = 0;
        var givenPoint = 0;
        var maxImsc = 0;
        var givenImsc = 0;

        var exercises = JsonSerializer.Deserialize<Exercise[]>(result.Result);

        if (exercises is not null && exercises.Length != 0)
        {
            foreach (var exercise in exercises)
            {
                if (exercise.ExerciseName.StartsWith("imsc"))
                {
                    maxImsc += exercise.MaxPoint;
                    givenImsc += exercise.GivenPoint;
                }
                else
                {
                    maxPoint += exercise.MaxPoint;
                    givenPoint += exercise.GivenPoint;
                }
            }
        }

        return $"**Osszesites/Summary**:\n{givenPoint} out of {maxPoint}\nimsc: {givenImsc} out of {maxImsc}";
    }
}
