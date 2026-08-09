namespace Ahk.Web.Server.Configuration;

/// <summary>
/// OpenID Connect provider settings (configuration section <c>Authentication:Oidc</c>). Written against BME's
/// Shibboleth IdP (<c>https://idp.bme.hu</c>) but kept generic.
///
/// When <see cref="Authority"/>/<see cref="ClientId"/> are empty the external login handler is not registered
/// and OIDC is disabled, so the app runs with local username/password only.
/// </summary>
public sealed class OidcOptions
{
    public const string SectionName = "Authentication:Oidc";

    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    /// <summary>Never commit this — use `dotnet user-secrets` locally and Authentication__Oidc__ClientSecret in production.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Exact scopes to request. Must stay within what the client is registered for — requesting an
    /// unregistered scope (e.g. <c>profile</c>, which BME did not register for us) is a common rejection cause.
    /// <c>offline_access</c> is registered but omitted by default: we mint our own Identity cookie and never
    /// use refresh tokens.
    /// </summary>
    public string[] Scopes { get; set; } = new[] { "openid", "email", "userinfo" };

    /// <summary>
    /// Off by default: the BME IdP does not advertise <c>code_challenge_methods_supported</c>, and we are a
    /// confidential client (client_secret_post), so PKCE is defence-in-depth rather than required.
    /// </summary>
    public bool UsePkce { get; set; }

    /// <summary>
    /// <c>query</c> by default. The ASP.NET default of <c>form_post</c> makes the callback a cross-site POST,
    /// which drops the correlation cookie under SameSite=Lax and fails with "Correlation failed".
    /// </summary>
    public string ResponseMode { get; set; } = "query";

    /// <summary>We exchange the external identity for our own cookie, so the provider's tokens are not kept.</summary>
    public bool SaveTokens { get; set; }

    /// <summary>
    /// Absolute redirect_uri override. Needed in development because the Angular proxy rewrites the Host, so the
    /// computed value would point at the backend port instead of the browser's origin.
    /// </summary>
    public string? RedirectUri { get; set; }

    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>
    /// RP-initiated logout endpoint. The BME IdP does not advertise <c>end_session_endpoint</c>, so this is empty
    /// and logout is local-only; setting it later enables full sign-out with no code change.
    /// </summary>
    public string? EndSessionEndpoint { get; set; }

    /// <summary>Development only: serve an in-app mock OpenID provider at /mock-oidc (see MockOidc/).</summary>
    public bool UseMockProvider { get; set; }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(Authority) && !string.IsNullOrWhiteSpace(ClientId);
}
