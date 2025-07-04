using GradeManagement.Bll.Services;
using GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authentication;

using System.Security.Claims;

namespace GradeManagement.Server.Authorization;

public class UserClaimsTransformation(UserService userService, SubjectTeacherService subjectTeacherService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is ClaimsIdentity identity && identity.IsAuthenticated)
        {
            var userEmail = identity.FindFirst(ClaimTypes.Email)?.Value ?? identity.FindFirst("email")?.Value;
            var user = await userService.GetOrCreateUserByEmailAsync(userEmail);
            var subjectRoles = await subjectTeacherService.GetAllSubjectsWithRolesForTeacherAsync(user.Id);

            identity.AddClaim(new Claim(CustomClaimTypes.UserRole, user.Type.ToString()));
            foreach (var subjectRole in subjectRoles)
            {
                identity.AddClaim(new Claim(CustomClaimTypes.SubjectAccess, subjectRole.Key.ToString()));
                identity.AddClaim(new Claim($"{CustomClaimTypes.AccessLevel}_{subjectRole.Key}", subjectRole.Value.ToString()));
            }
        }

        return principal;
    }
}
