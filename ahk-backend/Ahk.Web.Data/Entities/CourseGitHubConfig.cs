namespace Ahk.Web.Data.Entities;

/// <summary>
/// Per-course GitHub integration configuration — what used to be the per-deployment <c>AHK_*</c> environment
/// variables of github-monitor (<c>GitHubMonitorConfig.cs</c>). Kept in its own table (1:1 with
/// <see cref="Course"/>) so the per-request course lookup never loads the GitHub App private key.
/// </summary>
public class CourseGitHubConfig
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>GitHub App id (was AHK_GitHubAppId).</summary>
    public string? GitHubAppId { get; set; }

    /// <summary>GitHub App private key (was AHK_GitHubAppPrivateKey). Stored as a plain column by decision.</summary>
    public string? GitHubAppPrivateKey { get; set; }

    /// <summary>
    /// Personal / fine-grained access token used for REST calls that do not need a per-installation token —
    /// today only the connectivity health check. Stored as a plain column, like the other credentials.
    /// </summary>
    public string? GitHubAccessToken { get; set; }

    /// <summary>Secret used to validate the X-Hub-Signature-256 webhook signature (was AHK_GitHubWebhookSecret).</summary>
    public string? GitHubWebhookSecret { get; set; }

    /// <summary>Maximum allowed Actions workflow runs per repository; was the const WorkflowRunThreshold = 5.</summary>
    public int WorkflowRunThreshold { get; set; } = 5;

    /// <summary>When false, incoming webhooks for this course are ignored.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Last time an administrator changed these settings; shown in the admin UI.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
