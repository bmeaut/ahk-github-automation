namespace Ahk.Web.Server.Auth.Dto;

/// <summary>Result of signing out of the portal.</summary>
public sealed class LogoutResponse
{
    /// <summary>
    /// Provider end-session URL the SPA should navigate to for a full single sign-out, or null when the
    /// provider has none configured (the current case for the BME IdP) and sign-out is local-only.
    /// </summary>
    public string? EndSessionUrl { get; set; }
}
