using Ahk.GradeManagement.Api.Authorization.Policies;

using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Api.Authorization.Handlers;

public class AdminRequirementHandler : AuthorizationHandler<AdminRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (context.User.IsInAdminRole())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
