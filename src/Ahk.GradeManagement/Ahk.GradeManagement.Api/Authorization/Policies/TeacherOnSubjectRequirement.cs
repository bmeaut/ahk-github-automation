using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Api.Authorization.Policies;

public class TeacherOnSubjectRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "TeacherOnSubject";
}
