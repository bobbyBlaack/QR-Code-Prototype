using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using QR_Code_Prototype.Contracts.Auth;
using QR_Code_Prototype.Contracts.Common;
using QR_Code_Prototype.Domain.Entities;
using QR_Code_Prototype.Domain.Enums;
using QR_Code_Prototype.Repositories;

namespace QR_Code_Prototype.Services;

public sealed class AuthService(IUserRepository userRepository, IConfiguration configuration) : IAuthService
{
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public async Task<ApiResult<AuthResponseDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResult<AuthResponseDto>.Failure("validation_error", "Email and password are required.", StatusCodes.Status400BadRequest);
        }

        if (!Enum.IsDefined(request.Role))
        {
            return ApiResult<AuthResponseDto>.Failure("validation_error", "Role is invalid.", StatusCodes.Status400BadRequest);
        }

        if (await userRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return ApiResult<AuthResponseDto>.Failure("email_exists", "A user with this email already exists.", StatusCodes.Status409Conflict);
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = string.Empty,
            Role = request.Role,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
        return ApiResult<AuthResponseDto>.Success(CreateAuthResponse(user), StatusCodes.Status201Created);
    }

    public async Task<ApiResult<AuthResponseDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return ApiResult<AuthResponseDto>.Failure("invalid_credentials", "Invalid email or password.", StatusCodes.Status401Unauthorized);
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return ApiResult<AuthResponseDto>.Failure("invalid_credentials", "Invalid email or password.", StatusCodes.Status401Unauthorized);
        }

        return ApiResult<AuthResponseDto>.Success(CreateAuthResponse(user));
    }

    public async Task<ApiResult<AuthResponseDto>> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return ApiResult<AuthResponseDto>.Failure("unauthorized", "JWT subject is missing or invalid.", StatusCodes.Status401Unauthorized);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? ApiResult<AuthResponseDto>.Failure("user_not_found", "User was not found.", StatusCodes.Status404NotFound)
            : ApiResult<AuthResponseDto>.Success(CreateAuthResponse(user));
    }

    private AuthResponseDto CreateAuthResponse(AppUser user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(8);
        return new AuthResponseDto(user.Id, user.Email, user.Role, GenerateToken(user, expiresAtUtc), expiresAtUtc);
    }

    private string GenerateToken(AppUser user, DateTime expiresAtUtc)
    {
        var jwt = configuration.GetSection("Jwt");
        var secretKey = jwt["SecretKey"] ?? jwt["Key"]!;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
