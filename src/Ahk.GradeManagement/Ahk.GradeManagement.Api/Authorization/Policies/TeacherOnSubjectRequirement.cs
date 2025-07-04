using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Server.Authorization.Policies;

public class TeacherOnSubjectRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "TeacherOnSubject";
}
