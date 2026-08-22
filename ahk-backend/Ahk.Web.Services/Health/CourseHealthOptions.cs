namespace Ahk.Web.Services.Health;

/// <summary>
/// Tuning for the cached course health verdicts, bound from the <c>Health</c> configuration section.
/// </summary>
public sealed class CourseHealthOptions
{
    public const string SectionName = "Health";

    /// <summary>
    /// How long a cached verdict on <c>Course</c> is considered current. A stale verdict is still shown — the
    /// course register never waits for a check — it just also queues a background refresh.
    ///
    /// <para>A day is chosen because what the checks look at (credentials, App installation, CI tokens) changes
    /// when a human changes it, and that human sees a fresh result immediately: the course editor re-checks
    /// after every save, and /admin/health always runs live.</para>
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Whether the background refresh worker runs. Off in tests, which would otherwise let a page-triggered
    /// refresh reach out to GitHub in the background of an unrelated assertion.
    /// </summary>
    public bool RefreshWorkerEnabled { get; set; } = true;
}
