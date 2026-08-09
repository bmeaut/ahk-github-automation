using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.Health;

/// <summary>
/// Checks that the course can still receive evaluation results: publish-results-pr signs its callback with a
/// <see cref="CourseWebhookToken"/>, and without a live one every evaluation is rejected.
/// </summary>
public sealed class CiCallbackTokenHealthCheck : ICourseHealthCheck
{
    private readonly ApplicationDbContext db;

    public CiCallbackTokenHealthCheck(ApplicationDbContext db) => this.db = db;

    public string Id => "ci-callback-token";

    public string Title => "CI callback token";

    public int Order => 30;

    public async Task<HealthCheckResult> RunAsync(Course course, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: health checks run in the host/admin context, where no current course is set and
        // the course filter would otherwise match nothing.
        var tokens = await db.CourseWebhookTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.CourseId == course.Id)
            .Select(t => new { t.RevokedAt })
            .ToListAsync(cancellationToken);

        var active = tokens.Count(t => t.RevokedAt is null);
        if (active > 0)
        {
            return HealthCheckResult.Healthy(
                this,
                active == 1 ? "One active token accepts evaluation results." : $"{active} active tokens accept evaluation results.");
        }

        return tokens.Count == 0
            ? HealthCheckResult.NotConfigured(
                this,
                "No callback token exists, so evaluation results from GitHub Actions will be rejected.",
                "Create a token under CI callback tokens and set it on the course's evaluator workflow.")
            : HealthCheckResult.Failed(
                this,
                "Every callback token for this course has been revoked, so evaluation results are being rejected.",
                "Create a replacement token and update the course's evaluator workflow.");
    }
}
