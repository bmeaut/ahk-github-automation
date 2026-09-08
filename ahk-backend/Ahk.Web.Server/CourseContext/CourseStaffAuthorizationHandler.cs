using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.CourseContext;

/// <summary>
/// Grants the <c>CourseStaff</c> policy to every site admin (<see cref="Roles.Admin"/>) and to anyone holding a
/// membership in any course, whatever its <see cref="CourseRole"/>. See <see cref="CourseStaffRequirement"/>.
/// </summary>
public sealed class CourseStaffAuthorizationHandler : AuthorizationHandler<CourseStaffRequirement>
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> userManager;

    public CourseStaffAuthorizationHandler(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        this.db = db;
        this.userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CourseStaffRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (context.User.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        if (!int.TryParse(userManager.GetUserId(context.User), out var userId))
            return;

        // CourseMembership is not course-scoped, and these routes resolve no current course anyway.
        if (await db.CourseMemberships.AsNoTracking().AnyAsync(m => m.UserId == userId))
            context.Succeed(requirement);
    }
}
