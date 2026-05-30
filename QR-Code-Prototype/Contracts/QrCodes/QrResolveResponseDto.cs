using QR_Code_Prototype.Domain.Enums;

namespace QR_Code_Prototype.Contracts.QrCodes;

public sealed record QrResolveResponseDto(
    Guid QrCodeId,
    Guid PackageId,
    string PackageReference,
    string? Description,
    PackageStatus Status,
    DateTime QrCreatedAtUtc,
    DateTime? QrExpiresAtUtc);
