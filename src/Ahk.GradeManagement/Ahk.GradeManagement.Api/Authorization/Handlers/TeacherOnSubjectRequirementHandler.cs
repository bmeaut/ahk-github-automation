using GradeManagement.Bll.Services;
using GradeManagement.Data;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace GradeManagement.Server.Authorization.Handlers;

public class TeacherOnSubjectRequirementHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<TeacherOnSubjectRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TeacherOnSubjectRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (AdminRoleChecker.CheckAdminRole(context, requirement))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Subject-Id-Value", out var subjectIdHeader))
        {
            if (long.TryParse(subjectIdHeader, out var subjectId))
            {
                var subjectAccessClaims =
                    context.User.FindAll(CustomClaimTypes.SubjectAccess).Select(c => c.Value).ToList();
                var roleOnSubjectClaim = context.User.FindFirst($"{CustomClaimTypes.AccessLevel}_{subjectId}");
                if (subjectAccessClaims.Contains(subjectId.ToString()) && roleOnSubjectClaim != null &&
                    roleOnSubjectClaim.Value == UserRoleOnSubject.Teacher.ToString())
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        context.Fail();
        return Task.CompletedTask;
    }
}
