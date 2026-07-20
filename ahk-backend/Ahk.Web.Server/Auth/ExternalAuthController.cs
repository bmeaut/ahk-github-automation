using System.Security.Claims;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

    public ExternalAuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IOptions<OidcOptions> oidcOptions)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.oidcOptions = oidcOptions.Value;
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
        if (!signInResult.Succeeded)
        {
            // First time this external identity is seen: provision a local user and link the login.
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email ?? info.ProviderKey;

            var user = (email is not null ? await userManager.FindByEmailAsync(email) : null)
                       ?? new ApplicationUser { UserName = email ?? $"{info.LoginProvider}:{info.ProviderKey}", Email = email, DisplayName = name, EmailConfirmed = true };

            if (user.Id.Length == 0 || await userManager.FindByIdAsync(user.Id) is null)
            {
                var created = await userManager.CreateAsync(user);
                if (!created.Succeeded)
                    return Redirect(SafeReturnUrl(returnUrl, error: "user_creation_failed"));
            }

            await userManager.AddLoginAsync(user, info);
            await signInManager.SignInAsync(user, isPersistent: true);
        }

        return Redirect(SafeReturnUrl(returnUrl));
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
