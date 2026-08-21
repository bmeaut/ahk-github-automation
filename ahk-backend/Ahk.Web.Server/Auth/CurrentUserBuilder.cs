using System.Security.Claims;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.Auth;

/// <summary>
/// Builds the session shape the SPA hydrates from — identity, site roles, and every course the user may open.
/// Shared by <see cref="AuthController"/> (login, me) and <see cref="ImpersonationController"/> so all four
/// entry points answer identically; the site-admin "every course" branch in particular is subtle enough that
/// a second copy would drift.
/// </summary>
public sealed class CurrentUserBuilder
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> userManager;

    public CurrentUserBuilder(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        this.db = db;
        this.userManager = userManager;
    }

    /// <summary>
    /// Builds the response for <paramref name="user"/>. <paramref name="impersonatorUserName"/> names the site
    /// admin looking through this account, when there is one, so the SPA can show a way back; callers read it
    /// from the signed cookie with <see cref="ImpersonatorNameOf"/> or supply it directly when they have just
    /// issued the cookie themselves.
    /// </summary>
    public async Task<CurrentUserResponse> BuildAsync(ApplicationUser user, string? impersonatorUserName = null)
    {
        var roles = await userManager.GetRolesAsync(user);

        var memberships = await db.CourseMemberships
            .AsNoTracking()
            .Where(m => m.UserId == user.Id)
            .OrderBy(m => m.Course!.Name)
            .ThenBy(m => m.Course!.Slug)
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
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Slug)
                .Select(c => new CourseMembershipDto
                {
                    Slug = c.Slug,
                    Name = c.Name,
                    Role = CourseRole.Admin.ToString(),
                    ViaSiteAdmin = true,
                })
                .ToListAsync();

            courses = memberships.Concat(rest)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Slug, StringComparer.Ordinal)
                .ToList();
        }

        return new CurrentUserResponse
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            DisplayName = user.DisplayName,
            NeptunCode = user.NeptunCode,
            GitHubUsername = user.GitHubUsername,
            Roles = roles.ToList(),
            Courses = courses,
            ImpersonatorUserName = impersonatorUserName,
        };
    }

    /// <summary>The impersonating admin's user name carried by <paramref name="principal"/>, or null.</summary>
    public static string? ImpersonatorNameOf(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ImpersonationClaims.ImpersonatorName);
}
