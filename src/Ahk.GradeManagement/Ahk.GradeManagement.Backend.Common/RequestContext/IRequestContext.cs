namespace Ahk.GradeManagement.Backend.Common.RequestContext;

public interface IRequestContext
{
    public bool IsAuthenticated { get; }
    public RequestUser? CurrentUser { get; }
    public CancellationToken RequestAborted { get; }
}

public record RequestUser(string Email, string DisplayName, long? CurrentSubjectId);
