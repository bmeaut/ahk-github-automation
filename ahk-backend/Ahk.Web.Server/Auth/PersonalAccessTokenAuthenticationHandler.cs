using System.Security.Claims;
using System.Text.Encodings.Web;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Server.Auth;

/// <summary>
/// Authenticates <c>Authorization: Bearer {token}</c> against a user's personal access tokens, so a script can
/// call the course read API as its owner.
///
/// <para>The principal is built by the same <see cref="IUserClaimsPrincipalFactory{TUser}"/> the cookie path
/// uses. That is what makes "authenticates as the user" exact rather than approximate: the roles and the user
/// id claim are identical, so <c>CourseMembershipAuthorizationHandler</c> — including its site-admin
/// short-circuit — behaves the same either way.</para>
///
/// <para>A request with no bearer header returns <see cref="AuthenticateResult.NoResult"/> rather than a
/// failure: the cookie scheme is tried alongside this one on the same request and must still win.</para>
/// </summary>
public sealed class PersonalAccessTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "PersonalAccessToken";

    private const string BearerPrefix = "Bearer ";

    private readonly IPersonalAccessTokenService tokens;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory;

    public PersonalAccessTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IPersonalAccessTokenService tokens,
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory)
        : base(options, logger, encoder)
    {
        this.tokens = tokens;
        this.userManager = userManager;
        this.claimsFactory = claimsFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!TryReadToken(out var value))
            return AuthenticateResult.NoResult();

        var token = await tokens.AuthenticateAsync(value, Context.RequestAborted);
        if (token is null)
        {
            // Masked like the CI callback logs it: enough to recognize which token was tried, not to use it.
            Logger.LogInformation("Personal access token {Token} is unknown or revoked.", Mask(value));
            return AuthenticateResult.Fail("The access token is not valid.");
        }

        var user = await userManager.FindByIdAsync(token.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (user is null)
            return AuthenticateResult.Fail("The access token is not valid.");

        // A locked-out account cannot sign in interactively; a token it minted earlier must not be a way round.
        if (await userManager.IsLockedOutAsync(user))
        {
            Logger.LogInformation("Personal access token {Token} belongs to a locked-out account.", Mask(value));
            return AuthenticateResult.Fail("The access token is not valid.");
        }

        var principal = await claimsFactory.CreateAsync(user);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private bool TryReadToken(out string token)
    {
        token = string.Empty;

        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        token = header[BearerPrefix.Length..].Trim();
        return token.Length > 0;
    }

    /// <summary>Last four characters only — enough to identify a token in a log, not to replay it.</summary>
    private static string Mask(string token) =>
        token.Length <= 4 ? "…" : "…" + token[^4..];
}
