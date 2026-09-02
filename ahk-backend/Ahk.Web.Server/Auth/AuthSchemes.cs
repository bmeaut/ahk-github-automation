namespace Ahk.Web.Server.Auth;

/// <summary>Scheme combinations named once, so the endpoints that share one cannot drift apart.</summary>
public static class AuthSchemes
{
    /// <summary>
    /// The Identity application cookie, spelled out because <c>IdentityConstants.ApplicationScheme</c> is a
    /// static readonly field and an <c>[Authorize]</c> argument has to be a compile-time constant.
    /// <c>PersonalAccessTokenTests</c> asserts the two still say the same thing.
    /// </summary>
    public const string ApplicationCookie = "Identity.Application";

    /// <summary>
    /// The interactive session cookie or a personal access token. Only the course read endpoints
    /// (<c>statuses</c>, <c>grades</c>) accept this pair; everywhere else the default cookie scheme applies,
    /// which is what keeps a token out of the admin API and away from minting further tokens.
    /// </summary>
    public const string CookieOrPersonalToken =
        ApplicationCookie + "," + PersonalAccessTokenAuthenticationHandler.SchemeName;
}
