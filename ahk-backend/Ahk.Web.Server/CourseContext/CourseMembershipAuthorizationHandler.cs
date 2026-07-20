using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.CourseContext;

/// <summary>
/// Grants the <c>CourseMember</c> policy when the authenticated user has a <see cref="CourseMembership"/> in the
/// resolved current course. Site admins (<see cref="Roles.Admin"/>) are allowed into any course.
/// </summary>
public sealed class CourseMembershipAuthorizationHandler : AuthorizationHandler<CourseMembershipRequirement>
{
    private readonly ICurrentCourseProvider currentCourse;
    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> userManager;

    public CourseMembershipAuthorizationHandler(ICurrentCourseProvider currentCourse, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        this.currentCourse = currentCourse;
        this.db = db;
        this.userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CourseMembershipRequirement requirement)
    {
        if (currentCourse.CurrentCourseId is not Guid courseId)
            return;

        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (context.User.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = userManager.GetUserId(context.User);
        if (userId is null)
            return;

        var isMember = await db.CourseMemberships.AsNoTracking()
            .AnyAsync(m => m.CourseId == courseId && m.UserId == userId);

        if (isMember)
            context.Succeed(requirement);
    }
}
