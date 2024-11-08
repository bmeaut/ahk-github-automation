using GradeManagement.Client.Network;

using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Client.Policies;

public class UserTypeAuthorizationHandler(UserClient userClient) : AuthorizationHandler<UserTypeRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserTypeRequirement requirement)
    {
        var user = await userClient.GetCurrentUserAsync();

        if (requirement.Type.Contains(user.Type))
        {
            context.Succeed(requirement);
        }
    }
}
