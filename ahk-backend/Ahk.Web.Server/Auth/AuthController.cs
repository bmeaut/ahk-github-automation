using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth.Dto;
using Ahk.Web.Server.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Server.Auth;

/// <summary>Local username/password authentication and the current-user endpoint for the SPA.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly CurrentUserBuilder currentUser;
    private readonly OidcOptions oidcOptions;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        CurrentUserBuilder currentUser,
        IOptions<OidcOptions> oidcOptions)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.currentUser = currentUser;
        this.oidcOptions = oidcOptions.Value;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginFailureResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(request.UserName, request.Password, request.RememberMe, lockoutOnFailure: true);

        // Lockout and a wrong password share the 401, but they need different words: one is "try again",
        // the other is "wait, or ask an administrator".
        if (result.IsLockedOut)
        {
            return Unauthorized(new LoginFailureResponse
            {
                Reason = "LockedOut",
                Error = "This account is temporarily locked after too many failed attempts. Try again in a few minutes.",
            });
        }

        if (result.IsNotAllowed)
        {
            return Unauthorized(new LoginFailureResponse
            {
                Reason = "NotAllowed",
                Error = "This account is not allowed to sign in. Ask an administrator to check it.",
            });
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new LoginFailureResponse
            {
                Reason = "InvalidCredentials",
                Error = "That username and password do not match an account.",
            });
        }

        var user = await userManager.FindByNameAsync(request.UserName);
        return Ok(await currentUser.BuildAsync(user!));
    }

    /// <summary>
    /// Clears the portal session. Returns the provider's end-session URL when one is configured, which the SPA
    /// then navigates to; the BME IdP does not advertise <c>end_session_endpoint</c>, so today this is null and
    /// sign-out is local-only (the SSO session at the IdP survives).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LogoutResponse>> Logout()
    {
        await signInManager.SignOutAsync();

        var endSession = oidcOptions.EndSessionEndpoint;
        if (string.IsNullOrWhiteSpace(endSession))
            return Ok(new LogoutResponse { EndSessionUrl = null });

        var postLogout = oidcOptions.PostLogoutRedirectUri;
        var url = string.IsNullOrWhiteSpace(postLogout)
            ? endSession
            : $"{endSession}{(endSession.Contains('?', StringComparison.Ordinal) ? "&" : "?")}post_logout_redirect_uri={Uri.EscapeDataString(postLogout)}";

        return Ok(new LogoutResponse { EndSessionUrl = url });
    }

    // There is deliberately no self-service registration. Accounts arrive one of two ways: a BME account on
    // its first OIDC sign-in (ExternalAuthController), or an administrator creating a local one
    // (UsersAdminController.Create). An anonymous endpoint that mints a full account with no approval and no
    // verified identity contradicts that model, so it was removed rather than gated.

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        // The cookie carries the impersonation marker when an admin is looking through this account, which is
        // the one thing the session shape needs beyond the user record itself.
        return Ok(await currentUser.BuildAsync(user, CurrentUserBuilder.ImpersonatorNameOf(User)));
    }
}
