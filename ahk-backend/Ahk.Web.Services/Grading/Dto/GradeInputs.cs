namespace Ahk.Web.Services.Grading.Dto;

/// <summary>Teacher grade/override via the "/ahk ok 5 3.5 0" chatops command (was SetGradeEvent).</summary>
public sealed class SetGradeInput
{
    public string Neptun { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public int PrNumber { get; set; }

    public string? PrUrl { get; set; }

    public string? Actor { get; set; }

    public string? Origin { get; set; }

    /// <summary>Positional point values; index maps to exercise order.</summary>
    public IReadOnlyList<double> Results { get; set; } = Array.Empty<double>();
}

/// <summary>Teacher approval that keeps the automated points as-is (was ConfirmAutoGradeEvent).</summary>
public sealed class ConfirmAutoGradeInput
{
    public string Neptun { get; set; } = string.Empty;

    public string Repository { get; set; } = string.Empty;

    public int PrNumber { get; set; }

    public string? PrUrl { get; set; }

    public string? Actor { get; set; }

    public string? Origin { get; set; }
}

/// <summary>Automated evaluation result posted by publish-results-pr (was AhkProcessResult).</summary>
public sealed class EvaluationResultInput
{
    public string NeptunCode { get; set; } = string.Empty;

    public string GitHubRepoName { get; set; } = string.Empty;

    public string? GitHubBranch { get; set; }

    public string? GitHubCommitHash { get; set; }

    public int? GitHubPullRequestNum { get; set; }

    public string? Origin { get; set; }

    public IReadOnlyList<EvaluationTaskResult> Result { get; set; } = Array.Empty<EvaluationTaskResult>();
}

/// <summary>One task line from result.txt (was AhkTaskResult). Points are summed per exercise before storage.</summary>
public sealed class EvaluationTaskResult
{
    public string? ExerciseName { get; set; }

    public string TaskName { get; set; } = string.Empty;

    public double Points { get; set; }

    public string? Comment { get; set; }
}
