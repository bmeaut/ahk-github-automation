using System.Net;

namespace Ahk.Web.Services.GitHub;

/// <summary>
/// A GitHub call that was expected to succeed did not. Carries GitHub's own status and message so the caller
/// can tell a student something true ("the organization does not allow this") rather than "something failed".
/// </summary>
public sealed class GitHubOperationException : Exception
{
    public GitHubOperationException(string operation, HttpStatusCode status, string? gitHubMessage)
        : base(BuildMessage(operation, status, gitHubMessage))
    {
        Operation = operation;
        Status = status;
        GitHubMessage = gitHubMessage;
    }

    public GitHubOperationException()
    {
    }

    public GitHubOperationException(string message)
        : base(message)
    {
    }

    public GitHubOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string? Operation { get; }

    public HttpStatusCode Status { get; }

    public string? GitHubMessage { get; }

    private static string BuildMessage(string operation, HttpStatusCode status, string? gitHubMessage) =>
        string.IsNullOrWhiteSpace(gitHubMessage)
            ? $"GitHub returned {(int)status} {status} for {operation}."
            : $"GitHub returned {(int)status} {status} for {operation}: {gitHubMessage}";
}
