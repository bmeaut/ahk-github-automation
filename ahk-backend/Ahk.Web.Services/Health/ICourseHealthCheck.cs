using Ahk.Web.Data.Entities;

namespace Ahk.Web.Services.Health;

/// <summary>
/// One verifiable property of a course's integration. Implementations are registered in DI and run by
/// <see cref="ICourseHealthService"/>; adding a check to the admin dashboard means adding one class and one
/// registration line — nothing else changes.
/// </summary>
public interface ICourseHealthCheck
{
    /// <summary>Stable machine identifier, e.g. "github-access-token". Used as the result key.</summary>
    string Id { get; }

    /// <summary>Short name shown as the check's label in the admin UI.</summary>
    string Title { get; }

    /// <summary>Order within a course's report; lower runs and displays first.</summary>
    int Order { get; }

    /// <summary>
    /// Runs the check. The <paramref name="course"/> is loaded with its <see cref="Course.GitHubConfig"/>.
    /// Implementations must not throw: return a <see cref="HealthStatus.Failed"/> result instead.
    /// </summary>
    Task<HealthCheckResult> RunAsync(Course course, CancellationToken cancellationToken = default);
}
