namespace Ahk.Web.Data.Entities;

/// <summary>
/// A credential a user mints for themselves so a script can call the course read API as them, instead of
/// driving the login form. Sent as <c>Authorization: Bearer {Token}</c>; the request is then authenticated as
/// <see cref="User"/>, with their roles and course memberships, so the same authorization policies apply.
///
/// <para>Not <see cref="ICourseScoped"/>: it belongs to a person, not a course. A query filter here would hide
/// every row on the requests that need it most, which have no course context at all.</para>
///
/// <para><see cref="Token"/> is stored as a plain column, like <see cref="CourseWebhookToken.Secret"/>, so its
/// owner can copy it again later. Unlike that one it acts as a person: whoever can read the row can act as
/// them, which is why only the owner's own endpoint ever returns the value.</para>
/// </summary>
public class PersonalAccessToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public ApplicationUser? User { get; set; }

    /// <summary>The value the caller sends. Globally unique — it is the whole lookup key.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>What the owner said it is for, so a list of them can be told apart.</summary>
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the token last authenticated a request, so a forgotten one is visible as such. Stamped lazily —
    /// see <c>PersonalAccessTokenService.AuthenticateAsync</c>.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>When set, the token is no longer accepted.</summary>
    public DateTimeOffset? RevokedAt { get; set; }
}
