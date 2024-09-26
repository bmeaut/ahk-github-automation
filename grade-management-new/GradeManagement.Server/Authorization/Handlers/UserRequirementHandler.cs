using GradeManagement.Bll.Services;
using GradeManagement.Data;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace GradeManagement.Server.Authorization.Handlers;

public class UserRequirementHandler : AuthorizationHandler<UserRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, UserRequirement requirement)
    {
        if (AdminRoleChecker.CheckAdminRole(context, requirement))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var roleClaim = context.User.FindFirst(CustomClaimTypes.SubjectAccess);
        if (roleClaim == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (roleClaim.Value == UserType.Teacher.ToString() || roleClaim.Value == UserType.User.ToString())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
