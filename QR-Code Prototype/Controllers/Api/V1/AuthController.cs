using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QR_Code_Prototype.Contracts.Auth;
using QR_Code_Prototype.Contracts.Common;
using QR_Code_Prototype.Services;

namespace QR_Code_Prototype.Controllers.Api.V1;

[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        FromResult(await authService.RegisterAsync(request, cancellationToken));

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        FromResult(await authService.LoginAsync(request, cancellationToken));

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Me(CancellationToken cancellationToken) =>
        FromResult(await authService.GetCurrentUserAsync(User, cancellationToken));
}
