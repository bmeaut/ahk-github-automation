namespace Ahk.Web.Data.Entities;

/// <summary>
/// Token/secret pair authenticating a course's CI callbacks (publish-results-pr → evaluation-result webhook).
/// Relational form of the <c>webhooktokens</c> container's <c>WebhookToken</c>.
///
/// <see cref="Token"/> is globally unique because the CI callback carries no {course} path segment — the token
/// itself is how that request resolves to a course. <see cref="Secret"/> is the HMAC-SHA256 key, verified with
/// the scheme ported from <c>grade-management/.../Helpers/HmacSha256Validator.cs</c>.
/// </summary>
public class CourseWebhookToken : ICourseScoped
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    /// <summary>Public identifier sent in the X-Ahk-Token header.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>HMAC signing key. Stored as a plain column by decision.</summary>
    public string Secret { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When set, the token is no longer accepted.</summary>
    public DateTimeOffset? RevokedAt { get; set; }
}
