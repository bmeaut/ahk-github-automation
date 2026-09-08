using Microsoft.AspNetCore.Authorization;

namespace Ahk.Web.Server.CourseContext;

/// <summary>
/// Requires the current user to staff <em>at least one</em> course — any <c>CourseMembership</c> row, either
/// role — or to be a site admin. In other words: not a student.
///
/// <para>Unlike the other three course policies this one names no course at all, because the endpoint it
/// guards has none: personal access tokens belong to a user, not to a course. It is the "is this person
/// staff?" question, and the only thing it gates today is the token surface — a student reads their own
/// repositories through the site, and giving them a credential to script that is support burden without a
/// use case.</para>
/// </summary>
public sealed class CourseStaffRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "CourseStaff";
}
