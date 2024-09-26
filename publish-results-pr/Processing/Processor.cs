using System.Text.Json;

using PublishResult.AppArgs;

namespace PublishResult.Processing;

public readonly struct AhkProcessResult
{
    public string GitHubRepoName { get; init; }
    public string GitHubBranch { get; init; }
    public int GitHubPullRequestNum { get; init; }
    public string GitHubCommitHash { get; init; }
    public string NeptunCode { get; init; }
    public string?[]? ImageFiles { get; init; }
    public string Result { get; init; }
    public string Origin { get; init; }
}

public static class Processor
{
    public static AhkProcessResult Process(AppArgs.AppArgs appArgs, string workDir)
    {
        var neptun = NeptunParser.ParseNeptun(appArgs.NeptunFileName);

        Console.WriteLine($"Neptun: {neptun}");

        string?[]? imageFiles = null;
        if (appArgs.ImageExtension == "")
            Console.WriteLine("Image files gathering not requested");
        else
        {
            imageFiles = ImageFilesFinder.GetImageFiles(workDir, appArgs.ImageExtension);
            if (imageFiles != null && imageFiles.Length > 0)
            {
                foreach (var imageFile in imageFiles)
                {
                    Console.WriteLine($"Found image file: {imageFile}");
                }
            }
            else
                Console.WriteLine("Found 0 image files");
        }

        string result = null!;
        if (appArgs.ResultFile == "")
            Console.WriteLine("Result file processing not requested");
        else
        {
            // JSON
            result = File.ReadAllText(appArgs.ResultFile);
            var exercises = JsonSerializer.Deserialize<Exercise[]>(result);
            if (exercises != null)
                foreach (var exercise in exercises)
                {
                    Console.WriteLine(exercise);
                }
        }

        return new AhkProcessResult
        {
            GitHubRepoName = appArgs.GitHubRepoFullName,
            GitHubBranch = appArgs.GitHubBranch,
            GitHubPullRequestNum = appArgs.GitHubPullRequestNum,
            GitHubCommitHash = appArgs.GitCommitHash,
            NeptunCode = neptun!,
            ImageFiles = imageFiles,
            Result = result,
            Origin = $"https://github.com/{appArgs.GitHubRepoFullName}/commit/{appArgs.GitCommitHash} https://github.com/{appArgs.GitHubRepoFullName}/actions/runs/{appArgs.GitHubActionRunId}",
        };
    }
}
