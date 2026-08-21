using System.Globalization;
using System.Security.Claims;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.Web.Server.Auth;

/// <summary>
/// Lets a site admin work through another account ("sign in as this user") and get back afterwards, so a
/// support question can be answered by looking at what the person actually sees instead of resetting their
/// password.
///
/// <para>The security model is the cookie: starting an impersonation needs <see cref="Roles.Admin"/>, and the
/// marker it writes (<see cref="ImpersonationClaims"/>) lives inside the data-protected application cookie.
/// Returning trusts nothing from the request — it reads the admin's id out of that cookie and re-checks the
/// role before restoring the session. Nothing here special-cases authorization anywhere else in the app: an
/// impersonated session is an ordinary sign-in as the target and holds exactly the target's rights.</para>
/// </summary>
[ApiController]
[Route("api/auth/impersonate")]
public sealed class ImpersonationController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly CurrentUserBuilder currentUser;
    private readonly ILogger<ImpersonationController> logger;

    public ImpersonationController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        CurrentUserBuilder currentUser,
        ILogger<ImpersonationController> logger)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    /// <summary>Signs the calling admin in as <paramref name="userId"/>, remembering who to return to.</summary>
    [HttpPost("{userId:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserResponse>> Start(int userId)
    {
        // No chaining. An impersonated session that happens to land on another admin must not be able to open
        // a further one — one hop keeps "who is really acting" answerable from a single claim.
        if (User.FindFirstValue(ImpersonationClaims.ImpersonatorId) is not null)
            return BadRequest(new { error = "Return to your own account before impersonating someone else." });

        var admin = await userManager.GetUserAsync(User);
        if (admin is null)
            return Unauthorized();

        if (admin.Id == userId)
            return BadRequest(new { error = "You are already signed in as yourself." });

        var target = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
        if (target is null)
            return NotFound();

        // A plain sign-in as the target, plus the marker. isPersistent: false — the impersonation must not
        // outlive the browser session even if the admin signed in with "remember me".
        await signInManager.SignInWithClaimsAsync(target, isPersistent: false, new[]
        {
            new Claim(ImpersonationClaims.ImpersonatorId, admin.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ImpersonationClaims.ImpersonatorName, admin.UserName ?? string.Empty),
        });

        logger.LogWarning(
            "Impersonation started: admin {AdminId} ({AdminUserName}) is now acting as user {TargetId} ({TargetUserName}) from {RemoteIp}.",
            admin.Id,
            admin.UserName,
            target.Id,
            target.UserName,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        // The new cookie only reaches the caller with this response, so User is still the admin here — name the
        // impersonator from the record rather than from the principal.
        return Ok(await currentUser.BuildAsync(target, admin.UserName));
    }

    /// <summary>
    /// Ends an impersonation and restores the admin's own session. Deliberately only <c>[Authorize]</c>: the
    /// caller is the impersonated user, who may hold no roles at all. The authority is the claim in their
    /// signed cookie, never anything they send.
    /// </summary>
    [HttpPost("stop")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Stop()
    {
        var impersonatorId = User.FindFirstValue(ImpersonationClaims.ImpersonatorId);
        if (impersonatorId is null)
            return BadRequest(new { error = "This session is not an impersonation." });

        var impersonatedId = userManager.GetUserId(User);
        var admin = await userManager.FindByIdAsync(impersonatorId);

        // The admin may have been deleted or demoted while the impersonation was open. Restoring the session
        // then would hand out an admin cookie no one is entitled to any more, so sign out instead.
        if (admin is null || !await userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            await signInManager.SignOutAsync();
            logger.LogWarning(
                "Impersonation ended by sign-out: the original admin {AdminId} no longer exists or is no longer a site admin.",
                impersonatorId);

            return Unauthorized(new { error = "Your administrator account is no longer available. Sign in again." });
        }

        await signInManager.SignInAsync(admin, isPersistent: false);

        logger.LogWarning(
            "Impersonation ended: admin {AdminId} ({AdminUserName}) returned from acting as user {TargetId} from {RemoteIp}.",
            admin.Id,
            admin.UserName,
            impersonatedId,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(await currentUser.BuildAsync(admin));
    }
}
