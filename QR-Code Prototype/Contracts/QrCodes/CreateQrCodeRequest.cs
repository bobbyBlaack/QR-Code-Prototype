namespace QR_Code_Prototype.Contracts.QrCodes;

public sealed class CreateQrCodeRequest
{
    public DateTime? ExpiresAtUtc { get; set; }
    public object? AdditionalPayload { get; set; }
}
