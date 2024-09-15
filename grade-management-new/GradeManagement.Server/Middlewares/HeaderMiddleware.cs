using GradeManagement.Data;

namespace GradeManagement.Server.Middlewares;

public class HeaderMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, GradeManagementDbContext gradeManagementDbContext)
    {
        if (context.Request.Headers.TryGetValue("Subject-Id-Value", out var subjectIdHeader))
        {
            if (long.TryParse(subjectIdHeader, out var subjectId))
            {
                gradeManagementDbContext.SubjectIdValue = subjectId;
            }
        }

        await next(context);
    }
}
