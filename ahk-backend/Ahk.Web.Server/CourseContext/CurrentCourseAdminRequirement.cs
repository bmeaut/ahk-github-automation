using Microsoft.AspNetCore.Authorization;

namespace Ahk.Web.Server.CourseContext;

/// <summary>
/// Requires <c>CourseRole.Admin</c> in the course the request is scoped to (site admins bypass) — the
/// course-scoped counterpart of <see cref="CourseAdminRequirement"/>.
///
/// <para>⚠️ The two are not interchangeable, and picking the wrong one is a security bug rather than a
/// compile error:</para>
/// <list type="bullet">
/// <item><description><see cref="CourseAdminRequirement"/> is for host/admin routes
/// (<c>api/admin/courses/{id}</c>), which have no <c>{course}</c> segment; it reads the course from the
/// <c>{id}</c> route value.</description></item>
/// <item><description>This one is for <c>api/{course}/…</c> routes, where <c>{id}</c> means whatever the
/// action operates on — a submission, an assignment — and reading it as a course id would let an admin of
/// course 42 act on entity 42 in someone else's course.</description></item>
/// </list>
/// </summary>
public sealed class CurrentCourseAdminRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "CurrentCourseAdmin";
}
