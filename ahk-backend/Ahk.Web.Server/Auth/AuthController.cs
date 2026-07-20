using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.Auth;

/// <summary>Local username/password authentication and the current-user endpoint for the SPA.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ApplicationDbContext db;

    public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.db = db;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(request.UserName, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized();

        var user = await userManager.FindByNameAsync(request.UserName);
        return Ok(await BuildCurrentUserAsync(user!));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
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
        var courses = await db.CourseMemberships
            .AsNoTracking()
            .Where(m => m.UserId == user.Id)
            .Select(m => new CourseMembershipDto
            {
                Slug = m.Course!.Slug,
                Name = m.Course.Name,
                Role = m.Role.ToString(),
            })
            .ToListAsync();

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
