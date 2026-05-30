using QR_Code_Prototype.Domain.Enums;

namespace QR_Code_Prototype.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAtUtc { get; set; }
}
