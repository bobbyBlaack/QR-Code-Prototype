using QR_Code_Prototype.Contracts.Common;
using QR_Code_Prototype.Contracts.Packages;
using QR_Code_Prototype.Domain.Entities;
using QR_Code_Prototype.Domain.Enums;
using QR_Code_Prototype.Repositories;

namespace QR_Code_Prototype.Services;

public sealed class PackageService(IPackageRepository packageRepository) : IPackageService
{
    public async Task<ApiResult<IReadOnlyList<PackageResponseDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var packages = await packageRepository.GetAllAsync(cancellationToken);
        return ApiResult<IReadOnlyList<PackageResponseDto>>.Success(packages.Select(ToDto).ToList());
    }

    public async Task<ApiResult<PackageListPageDto>> GetPageAsync(PackageListPageRequest request, CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1)
        {
            return ApiResult<PackageListPageDto>.Failure("validation_error", "PageNumber must be greater than or equal to 1.", StatusCodes.Status400BadRequest);
        }

        if (request.PageSize is < 1 or > 100)
        {
            return ApiResult<PackageListPageDto>.Failure("validation_error", "PageSize must be between 1 and 100.", StatusCodes.Status400BadRequest);
        }

        var totalCount = await packageRepository.CountAsync(cancellationToken);
        var skip = (request.PageNumber - 1) * request.PageSize;
        var packages = await packageRepository.GetPageAsync(skip, request.PageSize, cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return ApiResult<PackageListPageDto>.Success(new PackageListPageDto(
            packages.Select(ToDto).ToList(),
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages));
    }

    public async Task<ApiResult<PackageResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var package = await packageRepository.GetByIdAsync(id, cancellationToken);
        return package is null
            ? ApiResult<PackageResponseDto>.Failure("package_not_found", "Package was not found.", StatusCodes.Status404NotFound)
            : ApiResult<PackageResponseDto>.Success(ToDto(package));
    }

    public async Task<ApiResult<PackageResponseDto>> CreateAsync(CreatePackageRequest request, CancellationToken cancellationToken)
    {
        var packageReference = request.PackageReference.Trim();
        if (string.IsNullOrWhiteSpace(packageReference))
        {
            return ApiResult<PackageResponseDto>.Failure("validation_error", "PackageReference is required.", StatusCodes.Status400BadRequest);
        }

        if (await packageRepository.GetByReferenceAsync(packageReference, cancellationToken) is not null)
        {
            return ApiResult<PackageResponseDto>.Failure("package_reference_exists", "A package with this reference already exists.", StatusCodes.Status409Conflict);
        }

        var package = new Package
        {
            Id = Guid.NewGuid(),
            PackageReference = packageReference,
            Description = request.Description,
            Status = PackageStatus.Created,
            CreatedAtUtc = DateTime.UtcNow
        };

        await packageRepository.AddAsync(package, cancellationToken);
        await packageRepository.SaveChangesAsync(cancellationToken);
        return ApiResult<PackageResponseDto>.Success(ToDto(package), StatusCodes.Status201Created);
    }

    public async Task<ApiResult<PackageResponseDto>> UpdateAsync(Guid id, UpdatePackageRequest request, CancellationToken cancellationToken)
    {
        var package = await packageRepository.GetByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return ApiResult<PackageResponseDto>.Failure("package_not_found", "Package was not found.", StatusCodes.Status404NotFound);
        }

        var packageReference = request.PackageReference.Trim();
        if (string.IsNullOrWhiteSpace(packageReference))
        {
            return ApiResult<PackageResponseDto>.Failure("validation_error", "PackageReference is required.", StatusCodes.Status400BadRequest);
        }

        var duplicate = await packageRepository.GetByReferenceAsync(packageReference, cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
        {
            return ApiResult<PackageResponseDto>.Failure("package_reference_exists", "A package with this reference already exists.", StatusCodes.Status409Conflict);
        }

        package.PackageReference = packageReference;
        package.Description = request.Description;
        package.UpdatedAtUtc = DateTime.UtcNow;
        await packageRepository.SaveChangesAsync(cancellationToken);
        return ApiResult<PackageResponseDto>.Success(ToDto(package));
    }

    public async Task<ApiResult<PackageResponseDto>> UpdateStatusAsync(Guid id, UpdatePackageStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return ApiResult<PackageResponseDto>.Failure("validation_error", "Package status is invalid.", StatusCodes.Status400BadRequest);
        }

        var package = await packageRepository.GetByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return ApiResult<PackageResponseDto>.Failure("package_not_found", "Package was not found.", StatusCodes.Status404NotFound);
        }

        package.Status = request.Status;
        package.DeliveredAtUtc = request.Status == PackageStatus.Delivered ? DateTime.UtcNow : null;
        package.UpdatedAtUtc = DateTime.UtcNow;
        await packageRepository.SaveChangesAsync(cancellationToken);
        return ApiResult<PackageResponseDto>.Success(ToDto(package));
    }

    public async Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var package = await packageRepository.GetByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return ApiResult<bool>.Failure("package_not_found", "Package was not found.", StatusCodes.Status404NotFound);
        }

        packageRepository.Remove(package);
        await packageRepository.SaveChangesAsync(cancellationToken);
        return ApiResult<bool>.Success(true, StatusCodes.Status204NoContent);
    }

    private static PackageResponseDto ToDto(Package package) =>
        new(package.Id, package.PackageReference, package.Description, package.Status, package.CreatedAtUtc, package.UpdatedAtUtc, package.DeliveredAtUtc);
}
