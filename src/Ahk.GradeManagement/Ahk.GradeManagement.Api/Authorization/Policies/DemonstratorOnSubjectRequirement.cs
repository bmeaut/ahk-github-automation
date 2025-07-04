using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Api.Authorization.Policies;

public class DemonstratorOnSubjectRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "DemonstratorOnSubject";
}
