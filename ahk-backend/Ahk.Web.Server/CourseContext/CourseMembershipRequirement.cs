using Microsoft.AspNetCore.Authorization;

namespace Ahk.Web.Server.CourseContext;

/// <summary>Requires the current user to be a member of the resolved current course (site admins bypass).</summary>
public sealed class CourseMembershipRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "CourseMember";
}
