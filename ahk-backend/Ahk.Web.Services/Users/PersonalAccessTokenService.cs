using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Services.Users;

public interface IPersonalAccessTokenService
{
    /// <summary>
    /// The active token with this value, or null. Stamps <see cref="PersonalAccessToken.LastUsedAt"/> as a
    /// side effect, so the owner can tell a live token from a forgotten one.
    /// </summary>
    Task<PersonalAccessToken?> AuthenticateAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>A user's own tokens, newest first, values included — the owner may copy one again.</summary>
    Task<IReadOnlyList<PersonalAccessToken>> ListForUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Issues a token for a user. The returned entity carries the value; it is not shown elsewhere.</summary>
    Task<PersonalAccessToken> CreateAsync(int userId, string? description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes one of <paramref name="userId"/>'s own tokens. Scoped to the owner on purpose: the id comes
    /// from the client, and one user must not be able to revoke another's by guessing it.
    /// </summary>
    Task<bool> RevokeAsync(int userId, int tokenId, CancellationToken cancellationToken = default);

    /// <summary>Revokes any token, whoever owns it. For the site-admin surface only.</summary>
    Task<bool> RevokeAnyAsync(int tokenId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Owns the personal access tokens: the lookup on the authentication path and the issue/revoke operations
/// behind the account screen. Shaped after <see cref="Courses.WebhookTokenService"/>, including generating the
/// value with <see cref="TokenGenerator"/>.
///
/// <para>⚠️ Deliberately <em>not</em> cached, where the CI callback tokens are cached for an hour. A revoked
/// personal credential has to stop working now — someone revokes one because they think it leaked, not to
/// tidy up. The endpoints it guards project a whole course anyway, so one indexed lookup is noise beside them.
/// </para>
/// </summary>
public sealed class PersonalAccessTokenService : IPersonalAccessTokenService
{
    /// <summary>Marks the value as one of ours in a log or a secret scanner, and never as a CI token.</summary>
    public const string Prefix = "ahkp_";

    /// <summary>
    /// How stale <see cref="PersonalAccessToken.LastUsedAt"/> may get before it is written again. A script
    /// polling in a loop would otherwise cost a write per request to say what the previous one already said.
    /// </summary>
    private static readonly TimeSpan LastUsedResolution = TimeSpan.FromMinutes(1);

    private readonly ApplicationDbContext db;
    private readonly TimeProvider timeProvider;

    public PersonalAccessTokenService(ApplicationDbContext db, TimeProvider timeProvider)
    {
        this.db = db;
        this.timeProvider = timeProvider;
    }

    public async Task<PersonalAccessToken?> AuthenticateAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var entity = await db.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.RevokedAt == null, cancellationToken);

        if (entity is null)
            return null;

        var now = timeProvider.GetUtcNow();
        if (entity.LastUsedAt is null || now - entity.LastUsedAt >= LastUsedResolution)
        {
            entity.LastUsedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        return entity;
    }

    public async Task<IReadOnlyList<PersonalAccessToken>> ListForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await db.PersonalAccessTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<PersonalAccessToken> CreateAsync(int userId, string? description, CancellationToken cancellationToken = default)
    {
        var entity = new PersonalAccessToken
        {
            UserId = userId,
            Token = Prefix + TokenGenerator.UrlSafe(32),
            Description = description,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        db.PersonalAccessTokens.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public Task<bool> RevokeAsync(int userId, int tokenId, CancellationToken cancellationToken = default) =>
        RevokeAsync(t => t.Id == tokenId && t.UserId == userId, cancellationToken);

    public Task<bool> RevokeAnyAsync(int tokenId, CancellationToken cancellationToken = default) =>
        RevokeAsync(t => t.Id == tokenId, cancellationToken);

    private async Task<bool> RevokeAsync(
        System.Linq.Expressions.Expression<Func<PersonalAccessToken, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var entity = await db.PersonalAccessTokens.FirstOrDefaultAsync(predicate, cancellationToken);
        if (entity is null)
            return false;

        if (entity.RevokedAt is null)
        {
            entity.RevokedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
