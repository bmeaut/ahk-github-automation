using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Ahk.Web.Services.Courses;

public interface IWebhookTokenService
{
    Task<string?> GetSecretForTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Lists a course's tokens, newest first. Secrets are not returned — they are shown once, at creation.</summary>
    Task<IReadOnlyList<CourseWebhookToken>> ListForCourseAsync(int courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a new token/secret pair for a course. The returned entity carries the plaintext secret; it is the
    /// only time the caller can show it, so the admin UI surfaces it immediately.
    /// </summary>
    Task<CourseWebhookToken> CreateAsync(int courseId, string? description, CancellationToken cancellationToken = default);

    /// <summary>Revokes a token so callbacks signed with it are rejected. Returns false when it does not exist.</summary>
    Task<bool> RevokeAsync(int courseId, int tokenId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Owns the CI callback tokens: the hot-path secret lookup used by the evaluation-result webhook and the
/// administrative issue/revoke operations. Port of
/// <c>grade-management/.../TokenManagement/TokenManagementService.cs</c>, including its one-hour memory cache —
/// this runs on every evaluation-result callback, so the cache matters.
///
/// Issue and revoke live here rather than in the controller precisely because of that cache: a revoked token
/// would otherwise keep authenticating callbacks for up to an hour.
/// </summary>
public sealed class WebhookTokenService : IWebhookTokenService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly ApplicationDbContext db;
    private readonly IMemoryCache cache;

    public WebhookTokenService(ApplicationDbContext db, IMemoryCache cache)
    {
        this.db = db;
        this.cache = cache;
    }

    public async Task<string?> GetSecretForTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(token);
        if (cache.TryGetValue<string?>(key, out var cached))
            return cached;

        var secret = await db.CourseWebhookTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.Token == token && t.RevokedAt == null)
            .Select(t => t.Secret)
            .FirstOrDefaultAsync(cancellationToken);

        cache.Set(key, secret, CacheDuration);
        return secret;
    }

    public async Task<IReadOnlyList<CourseWebhookToken>> ListForCourseAsync(int courseId, CancellationToken cancellationToken = default) =>
        await db.CourseWebhookTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.CourseId == courseId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<CourseWebhookToken> CreateAsync(int courseId, string? description, CancellationToken cancellationToken = default)
    {
        var entity = new CourseWebhookToken
        {
            CourseId = courseId,
            Token = TokenGenerator.UrlSafe(24),
            Secret = TokenGenerator.UrlSafe(32),
            Description = description,
        };

        db.CourseWebhookTokens.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> RevokeAsync(int courseId, int tokenId, CancellationToken cancellationToken = default)
    {
        var entity = await db.CourseWebhookTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.CourseId == courseId, cancellationToken);

        if (entity is null)
            return false;

        if (entity.RevokedAt is null)
        {
            entity.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        // Drop the cached secret, otherwise the revoked token keeps authenticating callbacks until it expires.
        cache.Remove(CacheKey(entity.Token));
        return true;
    }

    private static string CacheKey(string token) => $"secrettotoken{token}";
}
