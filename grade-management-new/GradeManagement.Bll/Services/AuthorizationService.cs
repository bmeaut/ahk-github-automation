using AutSoft.Linq.Queryable;

using GradeManagement.Data;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Enums;
using GradeManagement.Shared.Exceptions;

using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

using User = GradeManagement.Data.Models.User;

namespace GradeManagement.Server.Authorization;

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

    public async Task userIsAtLeastADemonstratorOnSubject(ClaimsPrincipal user, long subjectId)
    {
        var currentUser = await GetCurrentUserAsync(user);
        if (currentUser.Type != UserType.Admin &&
            !await gradeManagementDbContext.SubjectTeacher.AnyAsync(s =>
                s.SubjectId == subjectId && s.UserId == currentUser.Id &&
                (s.Role == UserRoleOnSubject.Demonstrator || s.Role == UserRoleOnSubject.Teacher)))
        {
            throw new UnauthorizedException("You are not at least demonstrator on this subject.");
        }
    }
}
