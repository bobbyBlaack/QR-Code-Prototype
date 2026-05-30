namespace QR_Code_Prototype.Domain.Entities;

public class QrScanEvent
{
    public Guid Id { get; set; }
    public Guid? QrCodeRecordId { get; set; }
    public QrCodeRecord? QrCodeRecord { get; set; }
    public string? Token { get; set; }
    public DateTime ScannedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool WasSuccessful { get; set; }
    public string? FailureReason { get; set; }
}
