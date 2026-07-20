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
