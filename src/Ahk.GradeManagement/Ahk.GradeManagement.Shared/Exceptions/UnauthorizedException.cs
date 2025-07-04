namespace GradeManagement.Shared.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("Current user has no authorization for this operation.")
    {
    }

    public UnauthorizedException(string message) : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
