using Ahk.GradeManagement.Api.Authorization.Policies;
using Ahk.GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

using System.Globalization;

namespace Ahk.GradeManagement.Api.Authorization.Handlers;

public class TeacherOnSubjectRequirementHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<TeacherOnSubjectRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TeacherOnSubjectRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (context.User.IsInAdminRole())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Subject-Id-Value", out var subjectIdHeader)
            && long.TryParse(subjectIdHeader, out var subjectId))
        {
            var subjectAccessClaims = context.User.FindAll(CustomClaimTypes.SubjectAccess).Select(c => c.Value).ToList();
            var roleOnSubjectClaim = context.User.FindFirst($"{CustomClaimTypes.AccessLevel}_{subjectId}");

            if (subjectAccessClaims.Contains(subjectId.ToString(CultureInfo.InvariantCulture))
                && roleOnSubjectClaim != null
                && roleOnSubjectClaim.Value == UserRoleOnSubject.Teacher.ToString())
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
