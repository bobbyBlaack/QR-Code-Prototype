using QR_Code_Prototype.Domain.Enums;

namespace QR_Code_Prototype.Contracts.Packages;

public sealed record PackageResponseDto(
    Guid Id,
    string PackageReference,
    string? Description,
    PackageStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? DeliveredAtUtc);
