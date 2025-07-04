using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Api.Authorization.Policies;

public class TeacherRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Teacher";
}
