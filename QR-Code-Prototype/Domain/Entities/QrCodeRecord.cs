namespace QR_Code_Prototype.Domain.Entities;

public class QrCodeRecord
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public Package Package { get; set; } = null!;
    public required string Token { get; set; }
    public required string PayloadJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public ICollection<QrScanEvent> ScanEvents { get; set; } = new List<QrScanEvent>();
}
