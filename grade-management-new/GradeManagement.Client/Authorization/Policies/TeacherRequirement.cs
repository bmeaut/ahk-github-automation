using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Client.Authorization.Policies;

public class TeacherRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Teacher";
}
