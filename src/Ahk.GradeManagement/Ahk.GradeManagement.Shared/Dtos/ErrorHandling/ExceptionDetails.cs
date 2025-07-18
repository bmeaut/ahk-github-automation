namespace Ahk.GradeManagement.Shared.Dtos.ErrorHandling;

/// <summary>
/// Exception details for ProblemDetails DTO
/// </summary>
/// <remarks>
/// Only used for OpenApi contract generation.
/// </remarks>
public class ExceptionDetails
{
    public required string Message { get; init; }
    public required string Type { get; init; }
    public required string? StackTrace { get; init; }
}

