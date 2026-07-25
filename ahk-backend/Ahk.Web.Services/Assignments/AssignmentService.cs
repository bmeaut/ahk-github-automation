using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.GitHub;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.Assignments;

/// <summary>What an instructor supplies when creating or editing an assignment.</summary>
public sealed class AssignmentInput
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Either "owner/name" or a bare repository name, which is taken to be in the course's organization.</summary>
    public string TemplateRepoName { get; set; } = string.Empty;
}

/// <summary>
/// The result of checking an assignment's template repository on GitHub. Advisory: an assignment may be drafted
/// before its template exists, so a problem here is reported, never enforced.
/// </summary>
public sealed record TemplateCheck(bool Reachable, bool IsTemplate, string? HtmlUrl, string? Problem);

public interface IAssignmentService
{
    Task<IReadOnlyList<Assignment>> ListAsync(int courseId, bool includeArchived, CancellationToken cancellationToken = default);

    Task<Assignment?> GetAsync(int courseId, int assignmentId, CancellationToken cancellationToken = default);

    Task<Assignment> CreateAsync(int courseId, AssignmentInput input, CancellationToken cancellationToken = default);

    Task<Assignment?> UpdateAsync(int courseId, int assignmentId, AssignmentInput input, CancellationToken cancellationToken = default);

    /// <summary>Archives or restores. Archiving also closes the invite link to students who have not accepted yet.</summary>
    Task<Assignment?> SetArchivedAsync(int courseId, int assignmentId, bool archived, CancellationToken cancellationToken = default);

    /// <summary>Issues a fresh invite token, which invalidates every copy of the old link.</summary>
    Task<Assignment?> RegenerateInviteTokenAsync(int courseId, int assignmentId, CancellationToken cancellationToken = default);

    /// <summary>Deletes an assignment. Refuses once students have accepted it — archive those instead.</summary>
    Task<bool> DeleteAsync(int courseId, int assignmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssignmentAcceptance>> ListAcceptancesAsync(int courseId, int assignmentId, CancellationToken cancellationToken = default);

    /// <summary>Acceptances per assignment for a whole course, so the listing needs one query rather than one per row.</summary>
    Task<IReadOnlyDictionary<int, int>> CountAcceptancesAsync(int courseId, CancellationToken cancellationToken = default);

    /// <summary>Verifies the template repository exists and is marked as a template. Never throws.</summary>
    Task<TemplateCheck> CheckTemplateAsync(int courseId, string templateRepoName, CancellationToken cancellationToken = default);

    /// <summary>Splits a stored "owner/name" into its parts.</summary>
    static (string Owner, string Name) SplitRepoName(string fullName)
    {
        ArgumentNullException.ThrowIfNull(fullName);

        var separator = fullName.IndexOf('/', StringComparison.Ordinal);
        return separator < 0
            ? (string.Empty, fullName)
            : (fullName[..separator], fullName[(separator + 1)..]);
    }
}

/// <summary>
/// Assignment administration for instructors. Like every other service here it takes an explicit
/// <c>courseId</c> and reads with <c>IgnoreQueryFilters()</c>: the ambient course filter follows
/// <see cref="ICurrentCourseProvider"/>, which is only set on requests that carry a {course} route segment.
/// </summary>
public sealed class AssignmentService : IAssignmentService
{
    private readonly ApplicationDbContext db;
    private readonly IGitHubRepositoryService gitHub;
    private readonly ICourseGitHubAppTokenProvider tokens;

    public AssignmentService(ApplicationDbContext db, IGitHubRepositoryService gitHub, ICourseGitHubAppTokenProvider tokens)
    {
        this.db = db;
        this.gitHub = gitHub;
        this.tokens = tokens;
    }

    public async Task<IReadOnlyList<Assignment>> ListAsync(int courseId, bool includeArchived, CancellationToken cancellationToken = default)
    {
        var query = db.Assignments.IgnoreQueryFilters().AsNoTracking().Where(a => a.CourseId == courseId);

        if (!includeArchived)
            query = query.Where(a => a.ArchivedAt == null);

        return await query
            .OrderBy(a => a.ArchivedAt == null ? 0 : 1)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Assignment?> GetAsync(int courseId, int assignmentId, CancellationToken cancellationToken = default) =>
        await db.Assignments.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.CourseId == courseId, cancellationToken);

    public async Task<Assignment> CreateAsync(int courseId, AssignmentInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var assignment = new Assignment
        {
            CourseId = courseId,
            Name = input.Name.Trim(),
            Description = Trimmed(input.Description),
            TemplateRepoName = await QualifyRepoNameAsync(courseId, input.TemplateRepoName, cancellationToken),
            InviteToken = TokenGenerator.UrlSafe(18),
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<Assignment?> UpdateAsync(int courseId, int assignmentId, AssignmentInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var assignment = await TrackedAsync(courseId, assignmentId, cancellationToken);
        if (assignment is null)
            return null;

        assignment.Name = input.Name.Trim();
        assignment.Description = Trimmed(input.Description);
        assignment.TemplateRepoName = await QualifyRepoNameAsync(courseId, input.TemplateRepoName, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<Assignment?> SetArchivedAsync(int courseId, int assignmentId, bool archived, CancellationToken cancellationToken = default)
    {
        var assignment = await TrackedAsync(courseId, assignmentId, cancellationToken);
        if (assignment is null)
            return null;

        assignment.ArchivedAt = archived ? assignment.ArchivedAt ?? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<Assignment?> RegenerateInviteTokenAsync(int courseId, int assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await TrackedAsync(courseId, assignmentId, cancellationToken);
        if (assignment is null)
            return null;

        assignment.InviteToken = TokenGenerator.UrlSafe(18);
        await db.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task<bool> DeleteAsync(int courseId, int assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await TrackedAsync(courseId, assignmentId, cancellationToken);
        if (assignment is null)
            return false;

        // Repositories exist on GitHub for every acceptance; deleting the record would orphan them and lose the
        // audit trail of who got what. Archiving is the operation the instructor actually wants.
        var accepted = await db.AssignmentAcceptances.IgnoreQueryFilters()
            .AnyAsync(a => a.AssignmentId == assignmentId, cancellationToken);

        if (accepted)
            return false;

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AssignmentAcceptance>> ListAcceptancesAsync(int courseId, int assignmentId, CancellationToken cancellationToken = default) =>
        await db.AssignmentAcceptances.IgnoreQueryFilters().AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.CourseId == courseId && a.AssignmentId == assignmentId)
            .OrderByDescending(a => a.AcceptedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, int>> CountAcceptancesAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var counts = await db.AssignmentAcceptances.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .GroupBy(a => a.AssignmentId)
            .Select(g => new { AssignmentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.AssignmentId, c => c.Count);
    }

    public async Task<TemplateCheck> CheckTemplateAsync(int courseId, string templateRepoName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateRepoName))
            return new TemplateCheck(false, false, null, "No template repository is set.");

        var token = await tokens.GetForCourseAsync(courseId, bypassCache: false, cancellationToken);
        if (token is null)
            return new TemplateCheck(false, false, null, "This course has no GitHub App configured, so the template cannot be checked.");

        var qualified = await QualifyRepoNameAsync(courseId, templateRepoName, cancellationToken);
        var (owner, name) = IAssignmentService.SplitRepoName(qualified);

        try
        {
            var repository = await gitHub.GetRepositoryAsync(owner, name, token.Token, cancellationToken);
            if (repository is null)
                return new TemplateCheck(false, false, null, $"GitHub has no repository called '{qualified}', or the App cannot see it.");

            return repository.IsTemplate
                ? new TemplateCheck(true, true, repository.HtmlUrl, null)
                : new TemplateCheck(true, false, repository.HtmlUrl, $"'{qualified}' exists but is not marked as a template repository. Turn on \"Template repository\" in its settings.");
        }
        catch (GitHubOperationException ex)
        {
            return new TemplateCheck(false, false, null, ex.GitHubMessage ?? ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return new TemplateCheck(false, false, null, $"GitHub could not be reached: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new TemplateCheck(false, false, null, "GitHub did not respond in time.");
        }
    }

    private async Task<Assignment?> TrackedAsync(int courseId, int assignmentId, CancellationToken cancellationToken) =>
        await db.Assignments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.CourseId == courseId, cancellationToken);

    /// <summary>
    /// Accepts a bare repository name and completes it with the course's organization, so an instructor does
    /// not have to retype "ahk-viaubc01/" on every assignment. Always stored normalized, like every other
    /// repository name in the model.
    /// </summary>
    private async Task<string> QualifyRepoNameAsync(int courseId, string templateRepoName, CancellationToken cancellationToken)
    {
        var value = (templateRepoName ?? string.Empty).Trim().Trim('/');
        if (value.Contains('/', StringComparison.Ordinal))
            return Normalize.RepoName(value);

        var organization = await db.Courses.AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => c.GitHubOrganization)
            .FirstOrDefaultAsync(cancellationToken);

        return Normalize.RepoName(string.IsNullOrWhiteSpace(organization) ? value : $"{organization}/{value}");
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
