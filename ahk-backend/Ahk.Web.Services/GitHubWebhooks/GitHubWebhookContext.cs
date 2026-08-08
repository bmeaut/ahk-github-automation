using Octokit;

namespace Ahk.Web.Services.GitHubWebhooks;

/// <summary>
/// Everything one webhook delivery carries into the handlers. All per-delivery state lives here rather than on
/// the handler, which is what lets handlers be ordinary scoped DI registrations instead of the reflectively
/// activated, mutable objects <c>github-monitor</c> needed.
///
/// The body has already been signature-verified by the time a context exists; handlers may trust it.
/// </summary>
public sealed class GitHubWebhookContext
{
    /// <summary>
    /// The course the delivery's repository belongs to. Always passed explicitly into services — nothing on the
    /// webhook path may rely on the ambient <c>ICurrentCourseProvider</c>, because the EF course filter matches
    /// nothing when no course is set.
    /// </summary>
    public required int CourseId { get; init; }

    /// <summary>The <c>X-GitHub-Event</c> header.</summary>
    public required string GitHubEventName { get; init; }

    /// <summary>The <c>X-GitHub-Delivery</c> header; empty when absent. Makes status-event writes idempotent.</summary>
    public required string DeliveryId { get; init; }

    /// <summary>The raw, signature-verified request body.</summary>
    public required string RequestBody { get; init; }

    /// <summary>Authenticated as the course's GitHub App installation.</summary>
    public required IGitHubClient GitHubClient { get; init; }

    /// <summary>
    /// From <c>CourseGitHubConfig.WorkflowRunThreshold</c>. Was a compile-time constant of 5 in
    /// <c>github-monitor</c>, since each deployment served exactly one course.
    /// </summary>
    public required int WorkflowRunThreshold { get; init; }
}
