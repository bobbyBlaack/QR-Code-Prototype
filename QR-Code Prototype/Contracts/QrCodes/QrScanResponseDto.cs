namespace QR_Code_Prototype.Contracts.QrCodes;

public sealed record QrScanResponseDto(
    Guid ScanEventId,
    bool WasSuccessful,
    string? FailureReason,
    DateTime ScannedAtUtc,
    QrResolveResponseDto? ResolvedPackage);
