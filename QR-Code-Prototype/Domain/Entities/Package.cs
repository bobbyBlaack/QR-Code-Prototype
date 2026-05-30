using QR_Code_Prototype.Domain.Enums;

namespace QR_Code_Prototype.Domain.Entities;

public class Package
{
    public Guid Id { get; set; }
    public required string PackageReference { get; set; }
    public string? Description { get; set; }
    public PackageStatus Status { get; set; } = PackageStatus.Created;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public ICollection<QrCodeRecord> QrCodes { get; set; } = new List<QrCodeRecord>();
}
