using QR_Code_Prototype.Contracts.Common;
using QR_Code_Prototype.Contracts.Packages;

namespace QR_Code_Prototype.Services;

public interface IPackageService
{
    Task<ApiResult<IReadOnlyList<PackageResponseDto>>> GetAllAsync(CancellationToken cancellationToken);
    Task<ApiResult<PackageListPageDto>> GetPageAsync(PackageListPageRequest request, CancellationToken cancellationToken);
    Task<ApiResult<PackageResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiResult<PackageResponseDto>> CreateAsync(CreatePackageRequest request, CancellationToken cancellationToken);
    Task<ApiResult<PackageResponseDto>> UpdateAsync(Guid id, UpdatePackageRequest request, CancellationToken cancellationToken);
    Task<ApiResult<PackageResponseDto>> UpdateStatusAsync(Guid id, UpdatePackageStatusRequest request, CancellationToken cancellationToken);
    Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
