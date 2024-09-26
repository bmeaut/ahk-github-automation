using System.Text.Json;

namespace PublishResult.AppArgs;

public readonly struct AppArgs
{
    public string GitHubRepoFullName { get; init; }
    public string GitHubRepoOwner { get; init; }
    public string GitHubRepoName { get; init; }
    public string GitHubBranch { get; init; }
    public string GitCommitHash { get; init; }
    public string GitHubActionRunId { get; init; }
    public int GitHubPullRequestNum { get; init; }
    public string GitHubToken { get; init; }
    public string NeptunFileName { get; init; }
    public string ImageExtension { get; init; }
    public string ResultFile { get; init; }
    public string AhkAppUrl { get; init; }
    public string AhkAppSecret { get; init; }
    public string AhkAppToken { get; init; }
}

public static class ArgReader
{
    public static AppArgs GetArgs()
    {
        _ = Environment.GetEnvironmentVariable("GITHUB_ACTIONS")
            ?? throw new ArgumentException("GITHUB_ACTIONS not set (not running inside GitHub Actions)");
        var ghRepoFullName = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
        if (ghRepoFullName is null)
            throw new ArgumentNullException(nameof(ghRepoFullName));

        var ghRepoSplit = ghRepoFullName.Split("/");
        if (ghRepoSplit.Length != 2)
            throw new ArgumentException("GITHUB_REPOSITORY is not in expected format owner/name");
        var ghRepoOwner = ghRepoSplit[0];
        var ghRepoName = ghRepoSplit[1];

        var ghBranch = Environment.GetEnvironmentVariable("GITHUB_REF");
        if (ghBranch is null)
            throw new ArgumentNullException(nameof(ghBranch));

        var ghCommitHash = Environment.GetEnvironmentVariable("GITHUB_SHA");
        if (ghCommitHash is null)
            throw new ArgumentNullException(nameof(ghCommitHash));

        var ghActionRunId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
        ghActionRunId ??= "";

        var ghToken = Environment.GetEnvironmentVariable("INPUT_GITHUB_TOKEN");
        if (ghToken is null)
            throw new ArgumentNullException(nameof(ghToken));

        var neptunFileName = Environment.GetEnvironmentVariable("INPUT_AHK_NEPTUNFILENAME");
        neptunFileName ??= "neptun.txt";

        var imageExt = Environment.GetEnvironmentVariable("INPUT_AHK_IMAGEEXT");
        imageExt ??= ".png";

        var resultsFile = Environment.GetEnvironmentVariable("INPUT_AHK_RESULTFILE");
        resultsFile ??= "result.json";

        var ahkAppUrl = Environment.GetEnvironmentVariable("INPUT_AHK_APPURL");
        ahkAppUrl ??= "https://ahk-grade-management.azurewebsites.net/api/evaluation-result";

        var ahkAppToken = Environment.GetEnvironmentVariable("INPUT_AHK_APPTOKEN");
        ahkAppToken ??= "";

        var ahkAppSecret = Environment.GetEnvironmentVariable("INPUT_AHK_APPSECRET");
        ahkAppSecret ??= "";

        var ghEventPayloadFilePath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
        if (ghEventPayloadFilePath is null)
            throw new ArgumentNullException(ghEventPayloadFilePath);
        var ghPrNum = GetPrNumFromPayload(ghEventPayloadFilePath);

        return new AppArgs()
        {
            GitHubRepoFullName = ghRepoFullName,
            GitHubRepoOwner = ghRepoOwner,
            GitHubRepoName = ghRepoName,
            GitHubBranch = ghBranch,
            GitCommitHash = ghCommitHash,
            GitHubActionRunId = ghActionRunId,
            GitHubPullRequestNum = ghPrNum,
            GitHubToken = ghToken,
            NeptunFileName = neptunFileName,
            ImageExtension = imageExt,
            ResultFile = resultsFile,
            AhkAppUrl = ahkAppUrl,
            AhkAppToken = ahkAppToken,
            AhkAppSecret = ahkAppSecret,
        };
    }

    private static int GetPrNumFromPayload(string ghEventPayloadFilePath)
    {
        var jsonText = File.ReadAllText(ghEventPayloadFilePath)
            ?? throw new ArgumentNullException($"file at {ghEventPayloadFilePath} does not exist");

        var json = JsonSerializer.Deserialize<JsonElement>(jsonText);

        try
        {
            var pullRequest = json.TryGetProperty("pull_request", out var numberValue);
            var number = numberValue.TryGetProperty("number", out var numberValue2);

            if (numberValue2.ToString().Length <= 0)
                throw new Exception("not running within a pull request event context");

            return Int32.Parse(numberValue2.ToString());
        }
        catch (Exception)
        {
            throw new Exception("not running within a pull request event context");
        }
    }
}
