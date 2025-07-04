using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Api.Authorization.Policies;

public class AdminRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Admin";
}
