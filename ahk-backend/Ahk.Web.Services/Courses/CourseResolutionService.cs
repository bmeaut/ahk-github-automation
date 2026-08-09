using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.Courses;

public interface ICourseResolutionService
{
    Task<Course?> ResolveBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<Course?> ResolveByRepositoryAsync(string gitHubRepoName, CancellationToken cancellationToken = default);

    Task<Course?> ResolveByWebhookTokenAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Maps an incoming request to a course. Browser requests use the {course} route slug; machine-to-machine
/// requests have no such segment, so they resolve from the payload's repository (organization + repo-name
/// prefix) or from the CI callback token.
///
/// The repo-name prefix rule is the explicit form of the original system's implicit convention, where a course
/// *was* a repository-name prefix (ListConfirmedWithRepositoryPrefix / ListEventsForRepositories).
/// </summary>
public sealed class CourseResolutionService : ICourseResolutionService
{
    private readonly ApplicationDbContext db;

    public CourseResolutionService(ApplicationDbContext db) => this.db = db;

    public Task<Course?> ResolveBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

    public async Task<Course?> ResolveByRepositoryAsync(string gitHubRepoName, CancellationToken cancellationToken = default)
    {
        var repo = Normalize.RepoName(gitHubRepoName);
        if (string.IsNullOrEmpty(repo))
            return null;

        // "owner/name" — the owner is the GitHub organization.
        var slashIndex = repo.IndexOf('/', StringComparison.Ordinal);
        var org = slashIndex > 0 ? repo[..slashIndex] : null;
        var nameOnly = slashIndex > 0 ? repo[(slashIndex + 1)..] : repo;

        var candidates = await db.Courses.AsNoTracking()
            .Where(c => c.GitHubOrganization != null && c.GitHubOrganization == org)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return null;

        if (candidates.Count == 1)
            return candidates[0];

        // Several courses share the organization: disambiguate on the repo-name prefix, longest match wins.
        return candidates
            .Where(c => !string.IsNullOrEmpty(c.RepoNamePrefix) && nameOnly.StartsWith(c.RepoNamePrefix!, StringComparison.Ordinal))
            .OrderByDescending(c => c.RepoNamePrefix!.Length)
            .FirstOrDefault();
    }

    public async Task<Course?> ResolveByWebhookTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var match = await db.CourseWebhookTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == token && t.RevokedAt == null, cancellationToken);

        return match is null ? null : await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == match.CourseId, cancellationToken);
    }
}
