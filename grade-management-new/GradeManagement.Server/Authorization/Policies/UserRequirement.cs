using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Server.Authorization.Policies;

public class UserRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "User";
}
