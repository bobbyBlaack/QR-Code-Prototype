using System.ComponentModel.DataAnnotations;
using QR_Code_Prototype.Domain.Enums;

namespace QR_Code_Prototype.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;
}
