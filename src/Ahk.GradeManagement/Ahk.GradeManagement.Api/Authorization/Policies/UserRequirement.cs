using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Api.Authorization.Policies;

public class UserRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "User";
}
