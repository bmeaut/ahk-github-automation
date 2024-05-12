using GradeManagement.Server.Authorization.Policies;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace GradeManagement.Server.Authorization.Handlers;

public class TeacherRequirementHandler : AuthorizationHandler<TeacherRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TeacherRequirement requirement)
    {
        var emailAddress = context.User.FindFirstValue(ClaimTypes.Email);
        if (emailAddress?.EndsWith("@vik.bme.hu") == true)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
