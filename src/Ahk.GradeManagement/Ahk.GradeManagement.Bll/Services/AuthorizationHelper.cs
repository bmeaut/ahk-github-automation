using Ahk.GradeManagement.Shared.Enums;

using System.Security.Claims;

namespace Ahk.GradeManagement.Bll.Services;

public static class AuthorizationHelper
{
    public static string GetCurrentUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? user.FindFirst(ClaimTypes.Upn)?.Value;
    }

    public static string? GetCurrentUserRole(ClaimsPrincipal user)
    {
        return user.FindFirst(CustomClaimTypes.UserRole)?.Value;
    }
}
