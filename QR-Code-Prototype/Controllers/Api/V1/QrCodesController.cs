using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QR_Code_Prototype.Contracts.Common;
using QR_Code_Prototype.Contracts.QrCodes;
using QR_Code_Prototype.Services;

namespace QR_Code_Prototype.Controllers.Api.V1;

[Route("api/v1/qr-codes")]
[Produces("application/json")]
public sealed class QrCodesController(IQrCodeService qrCodeService) : ApiControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QrCodeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        FromResult(await qrCodeService.GetByIdAsync(id, cancellationToken));

    [AllowAnonymous]
    [HttpGet("resolve/{token}")]
    [ProducesResponseType(typeof(QrResolveResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Resolve(string token, CancellationToken cancellationToken) =>
        FromResult(await qrCodeService.ResolveAsync(token, cancellationToken));

    [AllowAnonymous]
    [HttpPost("resolve/{token}/scan")]
    [ProducesResponseType(typeof(QrScanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RecordScan(string token, QrScanRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        return FromResult(await qrCodeService.RecordScanAsync(token, request, ipAddress, userAgent, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(QrCodeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        FromResult(await qrCodeService.DeactivateAsync(id, cancellationToken));
}
