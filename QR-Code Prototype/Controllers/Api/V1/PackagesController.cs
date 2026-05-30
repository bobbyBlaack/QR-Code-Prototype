using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QR_Code_Prototype.Contracts.Common;
using QR_Code_Prototype.Contracts.Packages;
using QR_Code_Prototype.Contracts.QrCodes;
using QR_Code_Prototype.Services;

namespace QR_Code_Prototype.Controllers.Api.V1;

[Route("api/v1/packages")]
[Produces("application/json")]
public sealed class PackagesController(IPackageService packageService, IQrCodeService qrCodeService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PackageResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetAll([FromQuery] int? pageNumber, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        if (pageNumber is null && pageSize is null)
        {
            return FromResult(await packageService.GetAllAsync(cancellationToken));
        }

        var result = await packageService.GetPageAsync(new PackageListPageRequest(pageNumber ?? 1, pageSize ?? 20), cancellationToken);
        if (!result.IsSuccess)
        {
            return FromResult(result);
        }

        Response.Headers["X-Page-Number"] = result.Value!.PageNumber.ToString();
        Response.Headers["X-Page-Size"] = result.Value.PageSize.ToString();
        Response.Headers["X-Total-Count"] = result.Value.TotalCount.ToString();
        Response.Headers["X-Total-Pages"] = result.Value.TotalPages.ToString();

        return Ok(result.Value.Items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PackageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        FromResult(await packageService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(PackageResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Create(CreatePackageRequest request, CancellationToken cancellationToken) =>
        FromResult(await packageService.CreateAsync(request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PackageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Update(Guid id, UpdatePackageRequest request, CancellationToken cancellationToken) =>
        FromResult(await packageService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(PackageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateStatus(Guid id, UpdatePackageStatusRequest request, CancellationToken cancellationToken) =>
        FromResult(await packageService.UpdateStatusAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        FromResult(await packageService.DeleteAsync(id, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("{packageId:guid}/qr-codes")]
    [ProducesResponseType(typeof(QrCodeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CreateQrCode(Guid packageId, CreateQrCodeRequest request, CancellationToken cancellationToken) =>
        FromResult(await qrCodeService.CreateForPackageAsync(packageId, request, cancellationToken));
}
