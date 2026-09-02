using Microsoft.AspNetCore.Authorization;

namespace Ahk.Web.Server.CourseContext;

/// <summary>
/// Requires the current user to hold <c>CourseRole.Admin</c> in the course being acted on (site admins bypass).
///
/// <para>Unlike <see cref="CourseMembershipRequirement"/> this cannot read <c>ICurrentCourseProvider</c>: the
/// host/admin routes it guards have no <c>{course}</c> segment, so no current course is ever resolved. The
/// course is taken from the <c>{id:int}</c> route value instead — every action guarded by this policy must name
/// its route parameter <c>id</c>.</para>
/// </summary>
public sealed class CourseAdminRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "CourseAdmin";

    /// <summary>Route value naming the course an action operates on.</summary>
    public const string CourseIdRouteKey = "id";
}
