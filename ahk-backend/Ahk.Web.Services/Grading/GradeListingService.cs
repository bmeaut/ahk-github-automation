using Ahk.Web.Data;
using Ahk.Web.Services.Grading.Dto;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.Grading;

public interface IGradeListingService
{
    /// <summary>
    /// The course's final grades. Archived submissions are left out unless <paramref name="includeArchived"/>
    /// asks for them — the submissions screen does when it is showing archived rows, so a row it shows still
    /// has its points beside it.
    /// </summary>
    Task<IReadOnlyCollection<FinalStudentGrade>> ListAsync(int courseId, bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<string> ExportCsvAsync(int courseId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Final-grade listing and export. Port of <c>grade-management/.../ListGrades/GradeListing.cs</c>: take the
/// confirmed results, group by (neptun, submission), and keep the most recent one — the append-only history
/// means "current grade" is always the latest row.
/// </summary>
public sealed class GradeListingService : IGradeListingService
{
    private readonly ApplicationDbContext db;

    public GradeListingService(ApplicationDbContext db) => this.db = db;

    public async Task<IReadOnlyCollection<FinalStudentGrade>> ListAsync(int courseId, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var confirmed = await db.GradeRecords
            .IgnoreQueryFilters()
            .AsNoTracking()
            // Archived submissions leave the list, and with it the CSV export below — archiving means the
            // work is no longer part of the course's live picture. The grade rows themselves are untouched.
            .Where(g => g.CourseId == courseId && g.Confirmed)
            .Where(g => includeArchived || g.Submission!.ArchivedAt == null)
            .Include(g => g.Points)
            .Include(g => g.Submission)
            .ToListAsync(cancellationToken);

        return confirmed
            .GroupBy(g => new { g.Neptun, Repo = g.Submission!.GitHubRepoName })
            .Select(group =>
            {
                var latest = group.OrderByDescending(g => g.Date).First();
                return new FinalStudentGrade
                {
                    Neptun = group.Key.Neptun,
                    Repo = group.Key.Repo,
                    PrUrl = latest.PrUrl,
                    Points = latest.Points
                        .OrderBy(p => p.Order)
                        .GroupBy(p => p.Name)
                        .ToDictionary(p => p.Key, p => p.Last().Point),
                };
            })
            .ToList();
    }

    /// <summary>The export is always the live picture: an archived submission is not part of the course's report.</summary>
    public async Task<string> ExportCsvAsync(int courseId, CancellationToken cancellationToken = default)
        => CsvExporter.GetCsv(await ListAsync(courseId, includeArchived: false, cancellationToken));
}
