using Ahk.GradeManagement.Api.Authorization.Policies;
using Ahk.GradeManagement.Backend.Common.RequestContext;
using Ahk.GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

using System.Globalization;

namespace Ahk.GradeManagement.Api.Authorization.Handlers;

public class TeacherOnSubjectRequirementHandler(IHttpContextAccessor httpContextAccessor, IRequestContext requestContext)
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

        if (requestContext.CurrentUser?.CurrentSubjectId is not null)
        {
            var subjectAccessClaims = context.User.FindAll(CustomClaimTypes.SubjectAccess).Select(c => c.Value).ToList();
            var roleOnSubjectClaim = context.User.FindFirst($"{CustomClaimTypes.AccessLevel}_{requestContext.CurrentUser.CurrentSubjectId}");

            if (subjectAccessClaims.Contains(requestContext.CurrentUser.CurrentSubjectId.Value.ToString(CultureInfo.InvariantCulture))
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
