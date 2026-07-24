using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth.Dto;
using Ahk.Web.Server.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ahk.Web.Server.Auth;

/// <summary>Local username/password authentication and the current-user endpoint for the SPA.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ApplicationDbContext db;
    private readonly OidcOptions oidcOptions;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IOptions<OidcOptions> oidcOptions)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.db = db;
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
        return Ok(await BuildCurrentUserAsync(user!));
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

    [HttpPost("register")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CurrentUserResponse>> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.UserName, Email = request.Email, DisplayName = request.DisplayName };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await signInManager.SignInAsync(user, isPersistent: false);
        return Ok(await BuildCurrentUserAsync(user));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        return Ok(await BuildCurrentUserAsync(user));
    }

    private async Task<CurrentUserResponse> BuildCurrentUserAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        var memberships = await db.CourseMemberships
            .AsNoTracking()
            .Where(m => m.UserId == user.Id)
            .Select(m => new CourseMembershipDto
            {
                Slug = m.Course!.Slug,
                Name = m.Course.Name,
                Role = m.Role.ToString(),
            })
            .ToListAsync();

        // A site admin may open any course (CourseMembershipAuthorizationHandler says so), so the switcher has
        // to list them all — otherwise the instructor screens are unreachable for courses they do not staff.
        // Explicit memberships win, keeping the role the admin actually holds in their own courses.
        var courses = memberships;
        if (roles.Contains(Roles.Admin, StringComparer.Ordinal))
        {
            var assigned = memberships.Select(m => m.Slug).ToHashSet(StringComparer.Ordinal);
            var rest = await db.Courses
                .AsNoTracking()
                .Where(c => !assigned.Contains(c.Slug))
                .OrderBy(c => c.Slug)
                .Select(c => new CourseMembershipDto
                {
                    Slug = c.Slug,
                    Name = c.Name,
                    Role = CourseRole.Admin.ToString(),
                    ViaSiteAdmin = true,
                })
                .ToListAsync();

            courses = memberships.Concat(rest).OrderBy(c => c.Slug, StringComparer.Ordinal).ToList();
        }

        return new CurrentUserResponse
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = roles.ToList(),
            Courses = courses,
        };
    }
}
