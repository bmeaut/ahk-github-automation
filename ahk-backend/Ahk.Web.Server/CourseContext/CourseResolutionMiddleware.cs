using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.CourseContext;

/// <summary>
/// Resolves the {course} route segment (present on <c>/api/{course}/...</c> routes) to a <see cref="Course"/>,
/// stores it in <c>HttpContext.Items["Course"]</c>, and sets the request's <see cref="CurrentCourseProvider"/>
/// so the DbContext course filter applies. Returns 404 when the slug does not match a course. Routes without a
/// {course} segment (auth, admin, integrations) pass through untouched.
///
/// Runs after routing (needs route values) and after authentication. Course *membership* is enforced separately
/// by <see cref="CourseMembershipAuthorizationHandler"/> via the <c>CourseMember</c> policy.
/// </summary>
public sealed class CourseResolutionMiddleware
{
    public const string CourseRouteKey = "course";
    public const string CourseItemKey = "Course";

    private readonly RequestDelegate next;

    public CourseResolutionMiddleware(RequestDelegate next) => this.next = next;

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, CurrentCourseProvider currentCourse)
    {
        if (context.GetRouteValue(CourseRouteKey) is string slug && !string.IsNullOrEmpty(slug))
        {
            var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug);
            if (course is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            currentCourse.Set(course.Id);
            context.Items[CourseItemKey] = course;
        }

        await this.next(context);
    }
}
