using GradeManagement.Bll.Services;
using GradeManagement.Data;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

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
