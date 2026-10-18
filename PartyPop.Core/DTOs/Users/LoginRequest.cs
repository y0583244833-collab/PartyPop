using System.ComponentModel.DataAnnotations;

namespace PartyPop.Core.DTOs.Users;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
