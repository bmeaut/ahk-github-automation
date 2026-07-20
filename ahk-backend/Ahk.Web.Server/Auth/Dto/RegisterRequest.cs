using System.ComponentModel.DataAnnotations;

namespace Ahk.Web.Server.Auth.Dto;

public sealed class RegisterRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
