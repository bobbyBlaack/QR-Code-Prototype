using QR_Code_Prototype.Domain.Enums;

namespace QR_Code_Prototype.Contracts.Auth;

public sealed record AuthResponseDto(Guid UserId, string Email, UserRole Role, string Token, DateTime ExpiresAtUtc);
