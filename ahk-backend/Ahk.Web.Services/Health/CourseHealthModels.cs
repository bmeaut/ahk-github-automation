namespace Ahk.Web.Services.Health;

/// <summary>Outcome of a single health check. Ordered by severity so a report can take the worst.</summary>
public enum HealthStatus
{
    /// <summary>The check had nothing to verify (an optional setting is not in use).</summary>
    NotConfigured = 0,

    /// <summary>The check passed.</summary>
    Healthy = 1,

    /// <summary>The check passed, but something needs attention before it becomes a failure.</summary>
    Warning = 2,

    /// <summary>The check failed — this part of the course's integration will not work.</summary>
    Failed = 3,
}

/// <summary>Result of one <see cref="ICourseHealthCheck"/> run against one course.</summary>
public sealed class HealthCheckResult
{
    /// <summary>Stable identifier of the check that produced this result (<see cref="ICourseHealthCheck.Id"/>).</summary>
    public string CheckId { get; init; } = string.Empty;

    /// <summary>Human-readable name of the check, e.g. "GitHub access token".</summary>
    public string Title { get; init; } = string.Empty;

    public HealthStatus Status { get; init; }

    /// <summary>One sentence stating what was found. Shown verbatim in the admin UI.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Optional next step when the check did not pass.</summary>
    public string? Remediation { get; init; }

    public int DurationMs { get; init; }

    public static HealthCheckResult Healthy(ICourseHealthCheck check, string message) =>
        Create(check, HealthStatus.Healthy, message, null);

    public static HealthCheckResult Warning(ICourseHealthCheck check, string message, string? remediation = null) =>
        Create(check, HealthStatus.Warning, message, remediation);

    public static HealthCheckResult Failed(ICourseHealthCheck check, string message, string? remediation = null) =>
        Create(check, HealthStatus.Failed, message, remediation);

    public static HealthCheckResult NotConfigured(ICourseHealthCheck check, string message, string? remediation = null) =>
        Create(check, HealthStatus.NotConfigured, message, remediation);

    private static HealthCheckResult Create(ICourseHealthCheck check, HealthStatus status, string message, string? remediation) => new()
    {
        CheckId = check.Id,
        Title = check.Title,
        Status = status,
        Message = message,
        Remediation = remediation,
    };
}

/// <summary>All check results for one course, plus the aggregate status the course list shows.</summary>
public sealed class CourseHealthReport
{
    public int CourseId { get; init; }

    public string CourseSlug { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<HealthCheckResult> Checks { get; init; } = Array.Empty<HealthCheckResult>();

    /// <summary>
    /// The worst status across the checks. <see cref="HealthStatus.NotConfigured"/> results do not drag the
    /// aggregate down on their own — a course with nothing wired up yet is incomplete, not broken — but a
    /// report made up entirely of them reports <see cref="HealthStatus.NotConfigured"/>.
    /// </summary>
    public HealthStatus Status
    {
        get
        {
            if (Checks.Count == 0)
                return HealthStatus.NotConfigured;

            var worst = Checks.Max(c => c.Status);
            return worst;
        }
    }
}
