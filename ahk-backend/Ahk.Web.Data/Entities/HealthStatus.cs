namespace Ahk.Web.Data.Entities;

/// <summary>
/// Outcome of a single health check. Ordered by severity so a report can take the worst.
///
/// This lives in the data layer rather than next to the checks themselves because <see cref="Course"/>
/// caches the aggregate value — see <see cref="Course.HealthStatus"/>.
/// </summary>
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
