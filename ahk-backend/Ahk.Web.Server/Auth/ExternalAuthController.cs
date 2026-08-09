using System.Security.Claims;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Server.Auth;

/// <summary>
/// Generic OIDC external login. The SPA navigates the browser to <c>challenge</c>, which redirects to the OIDC
/// provider; the provider posts back to the OIDC handler (SignInScheme = external cookie) which returns to
/// <c>callback</c>. There the external identity is linked/created and the Identity application cookie is issued,
/// then the browser is redirected back into the SPA.
/// </summary>
[ApiController]
[Route("api/auth/external")]
public sealed class ExternalAuthController : ControllerBase
{
    public const string Scheme = "oidc";

    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly OidcOptions oidcOptions;
    private readonly ILogger<ExternalAuthController> logger;

    public ExternalAuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IOptions<OidcOptions> oidcOptions,
        ILogger<ExternalAuthController> logger)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.oidcOptions = oidcOptions.Value;
        this.logger = logger;
    }

    [HttpGet("challenge")]
    public IActionResult Challenge([FromQuery] string? returnUrl)
    {
        if (!oidcOptions.IsEnabled)
            return BadRequest(new { error = "OIDC is not configured." });

        var redirectUrl = Url.Action(nameof(Callback), values: new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(Scheme, redirectUrl);
        return Challenge(properties, Scheme);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? returnUrl)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
            return Redirect(SafeReturnUrl(returnUrl, error: "external_login_failed"));

        var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
        if (signInResult.Succeeded)
        {
            // Known identity: refresh the directory data, which may have changed since the last login.
            var existing = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existing is not null && ExternalClaimsMapper.SyncFromClaims(existing, info.Principal))
                await userManager.UpdateAsync(existing);

            return Redirect(SafeReturnUrl(returnUrl));
        }

        // First time this external identity is seen. Identify the person by Neptun code — the domain key —
        // not by email/username, so an account an admin pre-provisioned with just a Neptun code is linked
        // rather than duplicated.
        var neptunClaim = info.Principal.FindFirstValue(BmeClaimTypes.NeptunCode);
        var neptun = string.IsNullOrWhiteSpace(neptunClaim) ? null : Normalize.Neptun(neptunClaim);

        var user = neptun is null
            ? null
            : await userManager.Users.FirstOrDefaultAsync(u => u.NeptunCode == neptun);

        if (user is null)
        {
            // No account carries this Neptun code (or the IdP sent none): provision a fresh local user.
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            user = new ApplicationUser { UserName = email ?? BuildUserName(info), EmailConfirmed = true };
            ExternalClaimsMapper.SyncFromClaims(user, info.Principal);

            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                logger.LogError(
                    "Provisioning a user for external login {Provider}/{ProviderKey} failed: {Errors}",
                    info.LoginProvider,
                    info.ProviderKey,
                    string.Join("; ", created.Errors.Select(e => $"{e.Code}: {e.Description}")));

                return Redirect(SafeReturnUrl(returnUrl, error: "user_creation_failed"));
            }
        }
        else
        {
            // Existing account matched by Neptun code (e.g. admin-created): attach this login to it. The
            // username is never written by the claim sync, so it is preserved; email is refreshed.
            if (ExternalClaimsMapper.SyncFromClaims(user, info.Principal))
                await userManager.UpdateAsync(user);
        }

        await userManager.AddLoginAsync(user, info);
        await signInManager.SignInAsync(user, isPersistent: true);

        return Redirect(SafeReturnUrl(returnUrl));
    }

    /// <summary>
    /// Landing point after a provider-initiated sign-out, matching the registered
    /// post_logout_redirect_uris (https://ahk.aut.bme.hu/signout-callback-oidc). Unused while the BME IdP
    /// advertises no end-session endpoint, but registered and wired so enabling it needs no code change.
    /// </summary>
    [HttpGet("/signout-callback-oidc")]
    [AllowAnonymous]
    public IActionResult SignedOut([FromQuery] string? returnUrl)
        => Redirect(SafeReturnUrl(returnUrl));

    /// <summary>
    /// Fallback username when the provider returns no email. Identity's AllowedUserNameCharacters excludes ':',
    /// so the provider/subject pair is sanitized rather than concatenated raw.
    /// </summary>
    private static string BuildUserName(ExternalLoginInfo info)
    {
        var raw = $"{info.LoginProvider}-{info.ProviderKey}";
        var safe = new string(raw.Select(c => char.IsLetterOrDigit(c) || c is '-' or '.' or '_' or '@' or '+' ? c : '-').ToArray());
        return safe;
    }

    // Only allow relative return paths to avoid open-redirects; default to the SPA root.
    private string SafeReturnUrl(string? returnUrl, string? error = null)
    {
        var target = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
        if (error is null)
            return target;

        return target + (target.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "error=" + Uri.EscapeDataString(error);
    }
}
