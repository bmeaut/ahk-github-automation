using AutSoft.Linq.Queryable;

using GradeManagement.Data;
using GradeManagement.Shared.Enums;
using GradeManagement.Shared.Exceptions;

using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

using User = GradeManagement.Data.Models.User;

namespace GradeManagement.Bll.Services;

public class AuthorizationService(GradeManagementDbContext gradeManagementDbContext)
{
    public string GetCurrentUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
    }

    public async Task<User> GetCurrentUserAsync(ClaimsPrincipal user)
    {
        var email = GetCurrentUserEmail(user);
        return await gradeManagementDbContext.User.SingleEntityAsync(s => s.BmeEmail == email, 0);
    }
}
