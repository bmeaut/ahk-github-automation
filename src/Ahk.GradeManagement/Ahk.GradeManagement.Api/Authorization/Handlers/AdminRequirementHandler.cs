using GradeManagement.Server.Authorization.Policies;

using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Server.Authorization.Handlers;

public class AdminRequirementHandler : AuthorizationHandler<AdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (AdminRoleChecker.CheckAdminRole(context, requirement))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
