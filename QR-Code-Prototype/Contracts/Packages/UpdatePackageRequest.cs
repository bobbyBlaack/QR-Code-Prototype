using System.ComponentModel.DataAnnotations;

namespace QR_Code_Prototype.Contracts.Packages;

public sealed class UpdatePackageRequest
{
    [Required]
    [MaxLength(100)]
    public string PackageReference { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
