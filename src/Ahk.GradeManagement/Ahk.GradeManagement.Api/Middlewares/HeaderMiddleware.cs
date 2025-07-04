using Ahk.GradeManagement.Dal;

namespace Ahk.GradeManagement.Api.Middlewares;

public class HeaderMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, GradeManagementDbContext gradeManagementDbContext)
    {
        if (context.Request.Headers.TryGetValue("X-Subject-Id-Value", out var subjectIdHeader))
        {
            if (long.TryParse(subjectIdHeader, out var subjectId))
                gradeManagementDbContext.SubjectIdValue = subjectId;
        }

        await next(context);
    }
}
