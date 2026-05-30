using QR_Code_Prototype.Contracts.Common;
using QR_Code_Prototype.Contracts.QrCodes;

namespace QR_Code_Prototype.Services;

public interface IQrCodeService
{
    Task<ApiResult<QrCodeResponseDto>> CreateForPackageAsync(Guid packageId, CreateQrCodeRequest request, CancellationToken cancellationToken);
    Task<ApiResult<QrCodeResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiResult<QrResolveResponseDto>> ResolveAsync(string token, CancellationToken cancellationToken);
    Task<ApiResult<QrScanResponseDto>> RecordScanAsync(string token, QrScanRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<ApiResult<QrCodeResponseDto>> DeactivateAsync(Guid id, CancellationToken cancellationToken);
}
