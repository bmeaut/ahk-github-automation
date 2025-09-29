using Ahk.GradeManagement.Backend.Common.RequestContext;

namespace Ahk.GradeManagement.QueueFunction.RequestContext;

public class QueueFunctionRequestContext : IRequestContext
{
    public bool IsAuthenticated => true;
    public RequestUser? CurrentUser => null;
    public CancellationToken RequestAborted => CancellationToken.None;
}
