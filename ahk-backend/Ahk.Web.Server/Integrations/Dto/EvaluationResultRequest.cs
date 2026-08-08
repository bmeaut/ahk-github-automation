using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ahk.Web.Server.Integrations.Dto;

/// <summary>
/// The evaluation result posted by <c>publish-results-pr</c>. A verbatim port of grade-management's
/// <c>AhkProcessResult</c>, property names and validation attributes included: this is a wire contract with a
/// Go client running inside student repositories, and those repositories are updated on their own schedule.
///
/// <para>⚠️ Unknown members are tolerated on purpose. The Go client also sends <c>imageFiles</c>, which has no
/// counterpart here and never had one. Do not turn on <c>JsonUnmappedMemberHandling.Disallow</c>.</para>
/// </summary>
public sealed class EvaluationResultRequest
{
    [JsonPropertyName("gitHubRepoName")]
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string GitHubRepoName { get; set; } = string.Empty;

    [JsonPropertyName("gitHubBranch")]
    public string? GitHubBranch { get; set; }

    [JsonPropertyName("gitHubCommitHash")]
    public string? GitHubCommitHash { get; set; }

    /// <summary>Absent rather than zero when there is no pull request — the Go client marks it <c>omitempty</c>.</summary>
    [JsonPropertyName("gitHubPullRequestNum")]
    public int? GitHubPullRequestNum { get; set; }

    [JsonPropertyName("neptunCode")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string NeptunCode { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public EvaluationTaskResultRequest[]? Result { get; set; }

    [JsonPropertyName("origin")]
    public string? Origin { get; set; }
}

/// <summary>
/// One task line from <c>result.txt</c>.
///
/// <para>⚠️ The <c>[Required]</c> on <see cref="TaskName"/> is not actually enforced, because
/// <c>Validator.TryValidateObject</c> does not recurse into collection elements. That was true in
/// grade-management too, and it is left alone deliberately: a stricter server here would start failing student
/// builds that pass today.</para>
/// </summary>
public sealed class EvaluationTaskResultRequest
{
    [JsonPropertyName("exerciseName")]
    public string? ExerciseName { get; set; }

    [JsonPropertyName("taskName")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string TaskName { get; set; } = string.Empty;

    [JsonPropertyName("points")]
    [Required]
    public double Points { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}
