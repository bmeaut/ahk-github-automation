using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Server.Authorization.Policies;

public class TeacherRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Teacher";
}
