using GradeManagement.Shared.Enums;

using System.Security.Claims;

namespace GradeManagement.Bll.Services;

public static class AuthorizationHelper
{
    public static string GetCurrentUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
    }

    public static string? GetCurrentUserRole(ClaimsPrincipal user)
    {
        return user.FindFirst(CustomClaimTypes.UserRole)?.Value;
    }
}
