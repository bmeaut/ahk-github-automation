namespace Ahk.Web.Server.Configuration;

/// <summary>
/// Generic OpenID Connect provider settings (bound from configuration section <c>Authentication:Oidc</c>).
/// When <see cref="Authority"/> is empty the external login handler is not registered and OIDC is disabled,
/// so the app runs with local username/password only.
/// </summary>
public sealed class OidcOptions
{
    public const string SectionName = "Authentication:Oidc";

    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    /// <summary>Scopes to request in addition to <c>openid</c>/<c>profile</c>. Defaults to email.</summary>
    public string[] Scopes { get; set; } = new[] { "email" };

    public bool IsEnabled => !string.IsNullOrWhiteSpace(Authority) && !string.IsNullOrWhiteSpace(ClientId);
}
