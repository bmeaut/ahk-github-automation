using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.CourseContext;

/// <summary>
/// Grants the <c>CurrentCourseAdmin</c> policy to a <see cref="CourseRole.Admin"/> of the resolved current
/// course, and to every site admin (<see cref="Roles.Admin"/>).
///
/// <para>Same shape as <see cref="CourseMembershipAuthorizationHandler"/>, one clause stricter: the membership
/// has to be an admin one. The course comes from <see cref="ICurrentCourseProvider"/>, which
/// <see cref="CourseResolutionMiddleware"/> set from the <c>{course}</c> slug — never from a route id. See
/// <see cref="CurrentCourseAdminRequirement"/> for why that distinction matters.</para>
/// </summary>
public sealed class CurrentCourseAdminAuthorizationHandler : AuthorizationHandler<CurrentCourseAdminRequirement>
{
    private readonly ICurrentCourseProvider currentCourse;
    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> userManager;

    public CurrentCourseAdminAuthorizationHandler(ICurrentCourseProvider currentCourse, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        this.currentCourse = currentCourse;
        this.db = db;
        this.userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CurrentCourseAdminRequirement requirement)
    {
        if (currentCourse.CurrentCourseId is not int courseId)
            return;

        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (context.User.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        if (!int.TryParse(userManager.GetUserId(context.User), out var userId))
            return;

        var isCourseAdmin = await db.CourseMemberships.AsNoTracking()
            .AnyAsync(m => m.CourseId == courseId && m.UserId == userId && m.Role == CourseRole.Admin);

        if (isCourseAdmin)
            context.Succeed(requirement);
    }
}
