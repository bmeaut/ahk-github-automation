using Ahk.GradeManagement.Api.Authorization.Policies;
using Ahk.GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

namespace Ahk.GradeManagement.Api.Authorization.Handlers;

public class TeacherRequirementHandler : AuthorizationHandler<TeacherRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TeacherRequirement requirement)
    {
        if (context.User.IsInAdminRole())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var roleClaim = context.User.FindFirst(CustomClaimTypes.UserRole);
        if (roleClaim == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (roleClaim.Value == UserType.Teacher.ToString())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
