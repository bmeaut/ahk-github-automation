using Ahk.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.Auth;

/// <summary>
/// The two steps of linking a GitHub account that the user's own profile and the admin console share: reading
/// a login out of what was typed, and refusing one that already backs another portal account.
/// </summary>
internal static class GitHubLogin
{
    /// <summary>
    /// The login in <paramref name="input"/>, trimmed. A pasted profile URL is the obvious mistake to be forgiving
    /// about, so only its last path segment is kept. Empty when nothing usable was given.
    /// </summary>
    public static string Parse(string? input)
    {
        var login = input?.Trim().TrimEnd('/') ?? string.Empty;
        var lastSlash = login.LastIndexOf('/');
        return lastSlash >= 0 ? login[(lastSlash + 1)..] : login;
    }

    /// <summary>
    /// True when a user other than <paramref name="userId"/> holds this GitHub account. One GitHub account belongs
    /// to one person, so it may back only one portal account. Matched on the numeric id as well as the login: the
    /// id is what survives a rename, so it catches the case where someone re-types a login another account claimed
    /// before that account was renamed. The filtered unique indexes are the real guarantee; this check exists to
    /// answer with a sentence rather than a 500.
    /// </summary>
    public static Task<bool> IsTakenAsync(ApplicationDbContext db, int userId, string login, long gitHubUserId, CancellationToken cancellationToken) =>
        db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id != userId && (u.GitHubUsername == login || u.GitHubUserId == gitHubUserId), cancellationToken);
}
