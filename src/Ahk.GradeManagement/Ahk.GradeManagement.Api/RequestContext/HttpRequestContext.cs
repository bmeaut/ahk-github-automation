using Ahk.GradeManagement.Backend.Common.RequestContext;
using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Constants;

using Microsoft.Identity.Web;

namespace Ahk.GradeManagement.Api.RequestContext;

public class HttpRequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    public bool IsAuthenticated => HttpContext.User?.Identity?.IsAuthenticated ?? false;

    public RequestUser? CurrentUser => HttpContext.User?.Identity?.IsAuthenticated ?? false
        ? new RequestUser(
            DisplayName: HttpContext.User.GetDisplayName()!,
            Email: HttpContext.User.GetCurrentUserEmail(),
            CurrentSubjectId: HttpContext.Request.Headers.TryGetValue(Headers.XSubjectId, out var subjectIdHeader) && long.TryParse(subjectIdHeader, out var subjectId) ? subjectId : null)
        : null;

    public CancellationToken RequestAborted => HttpContext.RequestAborted;

    private HttpContext HttpContext => httpContextAccessor.HttpContext!;
}
