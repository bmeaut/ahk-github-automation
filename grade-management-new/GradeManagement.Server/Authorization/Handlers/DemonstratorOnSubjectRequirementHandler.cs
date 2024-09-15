using GradeManagement.Bll.Services;
using GradeManagement.Data;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace GradeManagement.Server.Authorization.Handlers;

public class DemonstratorOnSubjectRequirementHandler(UserService userService, SubjectTeacherService subjectTeacherService, HttpContextAccessor httpContextAccessor) : AuthorizationHandler<DemonstratorOnSubjectRequirement>
{

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, DemonstratorOnSubjectRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return;
        }

        if (!httpContext.Request.RouteValues.TryGetValue("id", out var routeId) || !long.TryParse(routeId?.ToString(), out var subjectId))
        {
            context.Fail();
            return;
        }

        var emailAddress = context.User.FindFirst(ClaimTypes.Email)?.Value ?? context.User.FindFirst("email")?.Value;
        var user = await userService.GetOrCreateUserByEmailAsync(emailAddress ?? throw new InvalidOperationException("Email address was null"));
        var role = await subjectTeacherService.GetRoleIfConnectionExistsAsync(user.Id, subjectId);

        if (role is UserRoleOnSubject.Demonstrator or UserRoleOnSubject.Teacher || user.Type == UserType.Admin)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
