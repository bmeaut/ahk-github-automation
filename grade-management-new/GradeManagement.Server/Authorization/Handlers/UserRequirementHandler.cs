using GradeManagement.Bll.Services;
using GradeManagement.Data;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace GradeManagement.Server.Authorization.Handlers;

public class UserRequirementHandler(UserService userService) : AuthorizationHandler<UserRequirement>
{

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, UserRequirement requirement)
    {
        var emailAddress = context.User.FindFirst(ClaimTypes.Email)?.Value ?? context.User.FindFirst("email")?.Value;
        var user = await userService.GetOrCreateUserByEmailAsync(emailAddress ?? throw new InvalidOperationException("Email address was null"));

        if (user.Type is UserType.User or UserType.Teacher or UserType.Admin)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
