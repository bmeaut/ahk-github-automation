using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Server.Authorization.Policies;

public class DemonstratorOnSubjectRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "DemonstratorOnSubject";
}
