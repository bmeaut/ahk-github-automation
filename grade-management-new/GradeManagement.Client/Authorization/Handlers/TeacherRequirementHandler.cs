using GradeManagement.Client.Authorization.Policies;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace GradeManagement.Client.Authorization.Handlers;

public class TeacherRequirementHandler : AuthorizationHandler<TeacherRequirement>
{
    private readonly List<string> Whitelist = new List<string> { "gallikzoltan@edu.bme.hu" };

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TeacherRequirement requirement)
    {
        var emailAddress = context.User.FindFirst("email")?.Value;
        if (emailAddress is not null && (emailAddress.EndsWith("@vik.bme.hu") == true ||
                                         Whitelist.Contains(emailAddress)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
