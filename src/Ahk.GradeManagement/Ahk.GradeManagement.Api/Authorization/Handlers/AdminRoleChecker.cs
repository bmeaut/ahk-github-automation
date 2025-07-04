using Ahk.GradeManagement.Shared.Enums;

using System.Security.Claims;

namespace Ahk.GradeManagement.Api.Authorization.Handlers;

public static class AdminRoleChecker
{
    public static bool IsInAdminRole(this ClaimsPrincipal user)
    {
        var roleClaim = user.FindFirst(CustomClaimTypes.UserRole);
        if (roleClaim == null || roleClaim.Value != UserType.Admin.ToString())
            return false;

        return true;
    }
}
