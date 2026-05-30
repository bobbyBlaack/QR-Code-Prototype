namespace QR_Code_Prototype.Contracts.Packages;

public sealed record PackageListPageDto(
    IReadOnlyList<PackageResponseDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
