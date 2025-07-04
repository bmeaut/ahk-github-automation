using GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

namespace GradeManagement.Server.Authorization.Handlers;

public static class AdminRoleChecker
{
    public static bool CheckAdminRole(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(CustomClaimTypes.UserRole);
        if (roleClaim == null || roleClaim.Value != UserType.Admin.ToString())
        {
            return false;
        }

        return true;
    }
}
