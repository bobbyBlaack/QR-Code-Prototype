using System.ComponentModel.DataAnnotations;
using QR_Code_Prototype.Domain.Enums;

namespace QR_Code_Prototype.Contracts.Packages;

public sealed class UpdatePackageStatusRequest
{
    [Required]
    public PackageStatus Status { get; set; }
}
