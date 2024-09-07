using GradeManagement.Shared.Authorization.Policies;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace GradeManagement.Shared.Authorization.Handlers;

public class TeacherRequirementHandler : AuthorizationHandler<TeacherRequirement>
{
    private readonly List<string> Whitelist = new List<string> { "gallikzoltan@edu.bme.hu" };

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TeacherRequirement requirement)
    {
        //Here the email should be in Tenant or invited in by one meeting
        var emailAddress = context.User.FindFirst(ClaimTypes.Email)?.Value ?? context.User.FindFirst("email")?.Value;
        if (emailAddress is not null && (emailAddress.EndsWith("@vik.bme.hu") == true ||
                                         Whitelist.Contains(emailAddress)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
