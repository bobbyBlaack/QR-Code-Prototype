using System.Security.Claims;
using QR_Code_Prototype.Contracts.Auth;
using QR_Code_Prototype.Contracts.Common;

namespace QR_Code_Prototype.Services;

public interface IAuthService
{
    Task<ApiResult<AuthResponseDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<ApiResult<AuthResponseDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ApiResult<AuthResponseDto>> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
