using System.ComponentModel.DataAnnotations;

namespace Ahk.Web.Server.Auth.Dto;

public sealed class LoginRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

/// <summary>
/// Why a sign-in was refused. Sent with the 401 so the SPA can say what actually happened — a locked-out
/// account and a mistyped password are the same status code but very different things to be told.
/// </summary>
public sealed class LoginFailureResponse
{
    /// <summary>Machine-readable: <c>InvalidCredentials</c>, <c>LockedOut</c> or <c>NotAllowed</c>.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Sentence shown to the person signing in.</summary>
    public string Error { get; set; } = string.Empty;
}
