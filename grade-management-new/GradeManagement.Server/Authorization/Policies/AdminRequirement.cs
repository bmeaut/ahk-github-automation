using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Server.Authorization.Policies;

public class AdminRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Admin";
}
