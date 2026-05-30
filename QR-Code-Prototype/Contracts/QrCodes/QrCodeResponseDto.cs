namespace QR_Code_Prototype.Contracts.QrCodes;

public sealed record QrCodeResponseDto(
    Guid Id,
    Guid PackageId,
    string Token,
    string PayloadJson,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc);
