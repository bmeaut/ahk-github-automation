using System.Globalization;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Admin.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.Admin;

/// <summary>
/// Host/admin context: who has an account, what site role they hold, and which courses they are assigned to.
/// Site roles come from ASP.NET Identity (<see cref="Roles"/>); course assignments are
/// <see cref="CourseMembership"/> rows, which is what the <c>CourseMember</c> policy reads.
///
/// Two self-inflicted lockouts are refused: an admin cannot drop their own Admin role, and cannot delete
/// their own account.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class UsersAdminController : ControllerBase
{
    private const int MaxPageSize = 200;

    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> userManager;

    public UsersAdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        this.db = db;
        this.userManager = userManager;
    }

    /// <summary>
    /// Lists users, newest account last. <paramref name="search"/> matches user name, display name, e-mail or
    /// Neptun code; <paramref name="courseId"/> narrows to the members of one course.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserListResponse>> List(
        [FromQuery] string? search,
        [FromQuery] int? courseId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(term)) ||
                (u.DisplayName != null && u.DisplayName.Contains(term)) ||
                (u.Email != null && u.Email.Contains(term)) ||
                (u.NeptunCode != null && u.NeptunCode.Contains(term)));
        }

        if (courseId is int id)
            query = query.Where(u => u.CourseMemberships.Any(m => m.CourseId == id));

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, MaxPageSize))
            .Select(u => new
            {
                User = u,
                Courses = u.CourseMemberships
                    .Select(m => new UserCourseDto { CourseId = m.CourseId, Slug = m.Course!.Slug, Name = m.Course.Name, Role = m.Role })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        // Roles come from Identity's join tables; one query for the page beats one per user.
        var userIds = users.Select(u => u.User.Id).ToList();
        var rolesByUser = await db.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name! })
            .ToListAsync(cancellationToken);

        var items = users.Select(u => ToDto(
            u.User,
            rolesByUser.Where(r => r.UserId == u.User.Id).Select(r => r.RoleName).OrderBy(r => r, StringComparer.Ordinal).ToList(),
            u.Courses.OrderBy(c => c.Slug, StringComparer.Ordinal).ToList()))
            .ToList();

        return Ok(new UserListResponse { Items = items, Total = total });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Get(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(await LoadDtoAsync(user, cancellationToken));
    }

    /// <summary>Creates a local (password) account. Directory users are created on their first OIDC sign-in.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var neptun = NormalizeNeptun(request.NeptunCode);
        if (neptun is not null && await IsNeptunTakenAsync(neptun, excludeUserId: null, cancellationToken))
            return BadRequest(new { error = $"The Neptun code {neptun} is already assigned to another user." });

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            NeptunCode = neptun,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return CreatedAtAction(nameof(Get), new { id = user.Id }, await LoadDtoAsync(user, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
        if (user is null)
            return NotFound();

        var neptun = NormalizeNeptun(request.NeptunCode);
        if (neptun is not null && await IsNeptunTakenAsync(neptun, excludeUserId: user.Id, cancellationToken))
            return BadRequest(new { error = $"The Neptun code {neptun} is already assigned to another user." });

        user.DisplayName = Trimmed(request.DisplayName);
        user.NeptunCode = neptun;

        var email = Trimmed(request.Email);
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await userManager.SetEmailAsync(user, email);
            if (!emailResult.Succeeded)
                return BadRequest(new { errors = emailResult.Errors.Select(e => e.Description) });
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(await LoadDtoAsync(user, cancellationToken));
    }

    /// <summary>Replaces the user's site roles with exactly the set given.</summary>
    [HttpPut("{id:int}/roles")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateRoles(int id, [FromBody] UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
        if (user is null)
            return NotFound();

        var requested = request.Roles.Select(r => r.Trim()).Where(r => r.Length > 0).Distinct(StringComparer.Ordinal).ToList();

        var unknown = requested.Where(r => !Roles.All.Contains(r, StringComparer.Ordinal)).ToList();
        if (unknown.Count > 0)
            return BadRequest(new { error = $"Unknown role: {string.Join(", ", unknown)}." });

        if (IsSelf(user) && !requested.Contains(Roles.Admin, StringComparer.Ordinal))
            return BadRequest(new { error = "You cannot remove your own Admin role. Ask another administrator to do it." });

        var current = await userManager.GetRolesAsync(user);

        var toRemove = current.Except(requested, StringComparer.Ordinal).ToList();
        if (toRemove.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
                return BadRequest(new { errors = removeResult.Errors.Select(e => e.Description) });
        }

        var toAdd = requested.Except(current, StringComparer.Ordinal).ToList();
        if (toAdd.Count > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
                return BadRequest(new { errors = addResult.Errors.Select(e => e.Description) });
        }

        return Ok(await LoadDtoAsync(user, cancellationToken));
    }

    /// <summary>Assigns the user to a course, or changes the role they hold in it.</summary>
    [HttpPut("{id:int}/courses")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpsertCourse(int id, [FromBody] UpsertUserCourseRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return NotFound();

        if (!await db.Courses.AnyAsync(c => c.Id == request.CourseId, cancellationToken))
            return NotFound(new { error = "No such course." });

        var membership = await db.CourseMemberships
            .FirstOrDefaultAsync(m => m.UserId == id && m.CourseId == request.CourseId, cancellationToken);

        if (membership is null)
            db.CourseMemberships.Add(new CourseMembership { UserId = id, CourseId = request.CourseId, Role = request.Role });
        else
            membership.Role = request.Role;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(await LoadDtoAsync(user, cancellationToken));
    }

    [HttpDelete("{id:int}/courses/{courseId:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> RemoveCourse(int id, int courseId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
            return NotFound();

        var membership = await db.CourseMemberships
            .FirstOrDefaultAsync(m => m.UserId == id && m.CourseId == courseId, cancellationToken);

        if (membership is not null)
        {
            db.CourseMemberships.Remove(membership);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(await LoadDtoAsync(user, cancellationToken));
    }

    /// <summary>Sets a new password for a local account, e.g. after a support request.</summary>
    [HttpPost("{id:int}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPassword(int id, [FromBody] SetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
        if (user is null)
            return NotFound();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

        return result.Succeeded
            ? NoContent()
            : BadRequest(new { errors = result.Errors.Select(e => e.Description) });
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await userManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
        if (user is null)
            return NotFound();

        if (IsSelf(user))
            return BadRequest(new { error = "You cannot delete your own account." });

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded
            ? NoContent()
            : BadRequest(new { errors = result.Errors.Select(e => e.Description) });
    }

    private bool IsSelf(ApplicationUser user) =>
        string.Equals(userManager.GetUserId(User), user.Id.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

    private async Task<UserDto> LoadDtoAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var courses = await db.CourseMemberships
            .AsNoTracking()
            .Where(m => m.UserId == user.Id)
            .OrderBy(m => m.Course!.Slug)
            .Select(m => new UserCourseDto { CourseId = m.CourseId, Slug = m.Course!.Slug, Name = m.Course.Name, Role = m.Role })
            .ToListAsync(cancellationToken);

        return ToDto(user, roles.OrderBy(r => r, StringComparer.Ordinal).ToList(), courses);
    }

    private static UserDto ToDto(ApplicationUser user, IReadOnlyList<string> roles, IReadOnlyList<UserCourseDto> courses) => new()
    {
        Id = user.Id,
        UserName = user.UserName ?? string.Empty,
        Email = user.Email,
        DisplayName = user.DisplayName,
        NeptunCode = user.NeptunCode,
        Affiliation = user.Affiliation,
        Roles = roles,
        Courses = courses,
        IsExternal = user.PasswordHash is null,
        IsLockedOut = user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow,
    };

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Canonical Neptun code, or null for a blank one — never "", so the filtered unique index treats
    /// "no code" as absent rather than a shared value.</summary>
    private static string? NormalizeNeptun(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Normalize.Neptun(value);

    /// <summary>True when another user already holds this (already-normalized) Neptun code.</summary>
    private Task<bool> IsNeptunTakenAsync(string neptun, int? excludeUserId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.NeptunCode == neptun && (excludeUserId == null || u.Id != excludeUserId), cancellationToken);
}
