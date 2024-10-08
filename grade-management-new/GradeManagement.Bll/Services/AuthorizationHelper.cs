using System.Security.Claims;

namespace GradeManagement.Bll.Services;

public static class AuthorizationHelper
{
    public static string GetCurrentUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
    }
}
