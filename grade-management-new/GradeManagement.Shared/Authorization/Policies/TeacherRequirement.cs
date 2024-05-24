using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Shared.Authorization.Policies;

public class TeacherRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Teacher";
}
