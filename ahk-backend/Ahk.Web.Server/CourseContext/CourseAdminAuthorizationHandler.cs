using System.Globalization;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.CourseContext;

/// <summary>
/// Grants the <c>CourseAdmin</c> policy to a user who holds <see cref="CourseRole.Admin"/> in the course named
/// by the <c>{id}</c> route value, and to every site admin (<see cref="Roles.Admin"/>).
///
/// <para>The course comes from the route rather than <see cref="ICurrentCourseProvider"/> because these are
/// host/admin routes with no <c>{course}</c> segment; see <see cref="CourseAdminRequirement"/>.</para>
/// </summary>
public sealed class CourseAdminAuthorizationHandler : AuthorizationHandler<CourseAdminRequirement>
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> userManager;

    public CourseAdminAuthorizationHandler(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.db = db;
        this.userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CourseAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (context.User.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        if (CourseIdOf(context) is not int courseId)
            return;

        if (!int.TryParse(userManager.GetUserId(context.User), out var userId))
            return;

        // IgnoreQueryFilters is not needed: CourseMembership is not course-scoped, and no current course is set
        // on these routes anyway.
        var isCourseAdmin = await db.CourseMemberships.AsNoTracking()
            .AnyAsync(m => m.CourseId == courseId && m.UserId == userId && m.Role == CourseRole.Admin);

        if (isCourseAdmin)
            context.Succeed(requirement);
    }

    private int? CourseIdOf(AuthorizationHandlerContext context)
    {
        var routeValues = httpContextAccessor.HttpContext?.Request.RouteValues;
        if (routeValues is null || !routeValues.TryGetValue(CourseAdminRequirement.CourseIdRouteKey, out var raw))
            return null;

        return int.TryParse(raw as string ?? raw?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
    }
}
