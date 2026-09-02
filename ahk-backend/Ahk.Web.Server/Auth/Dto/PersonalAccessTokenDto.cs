using System.ComponentModel.DataAnnotations;

namespace Ahk.Web.Server.Auth.Dto;

/// <summary>
/// One of the caller's own access tokens, value included: the point of storing it in the clear is that its
/// owner can copy it again. Only the owner's endpoint returns this shape — the admin listing withholds it.
/// </summary>
public sealed class PersonalAccessTokenDto
{
    public int Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}

/// <summary>
/// One user's token as an administrator sees it: enough to decide whether to cut it off, and no more. An
/// admin who genuinely has to act as someone has impersonation for that.
/// </summary>
public sealed class UserAccessTokenDto
{
    public int Id { get; set; }

    /// <summary>Last four characters of the value, e.g. "…f3Ab" — enough to match against what the owner sees.</summary>
    public string? TokenHint { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class CreatePersonalAccessTokenRequest
{
    [MaxLength(512)]
    public string? Description { get; set; }
}
