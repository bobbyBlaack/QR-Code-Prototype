using System.Security.Cryptography;
using System.Text.Json;
using QR_Code_Prototype.Contracts.Common;
using QR_Code_Prototype.Contracts.QrCodes;
using QR_Code_Prototype.Domain.Entities;
using QR_Code_Prototype.Repositories;

namespace QR_Code_Prototype.Services;

public sealed class QrCodeService(IPackageRepository packageRepository, IQrCodeRepository qrCodeRepository) : IQrCodeService
{
    public async Task<ApiResult<QrCodeResponseDto>> CreateForPackageAsync(Guid packageId, CreateQrCodeRequest request, CancellationToken cancellationToken)
    {
        var package = await packageRepository.GetByIdAsync(packageId, cancellationToken);
        if (package is null)
        {
            return ApiResult<QrCodeResponseDto>.Failure("package_not_found", "Package was not found.", StatusCodes.Status404NotFound);
        }

        if (request.ExpiresAtUtc is not null && request.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return ApiResult<QrCodeResponseDto>.Failure("validation_error", "ExpiresAtUtc must be in the future.", StatusCodes.Status400BadRequest);
        }

        var token = await GenerateUniqueTokenAsync(cancellationToken);
        var createdAtUtc = DateTime.UtcNow;
        var payload = new
        {
            packageId = package.Id,
            packageReference = package.PackageReference,
            token,
            createdAtUtc,
            expiresAtUtc = request.ExpiresAtUtc,
            additionalPayload = request.AdditionalPayload
        };

        var qrCode = new QrCodeRecord
        {
            Id = Guid.NewGuid(),
            PackageId = package.Id,
            Token = token,
            PayloadJson = JsonSerializer.Serialize(payload),
            IsActive = true,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = request.ExpiresAtUtc
        };

        await qrCodeRepository.AddAsync(qrCode, cancellationToken);
        await qrCodeRepository.SaveChangesAsync(cancellationToken);
        qrCode.Package = package;
        return ApiResult<QrCodeResponseDto>.Success(ToDto(qrCode), StatusCodes.Status201Created);
    }

    public async Task<ApiResult<QrCodeResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var qrCode = await qrCodeRepository.GetByIdAsync(id, cancellationToken);
        return qrCode is null
            ? ApiResult<QrCodeResponseDto>.Failure("qr_code_not_found", "QR code was not found.", StatusCodes.Status404NotFound)
            : ApiResult<QrCodeResponseDto>.Success(ToDto(qrCode));
    }

    public async Task<ApiResult<QrResolveResponseDto>> ResolveAsync(string token, CancellationToken cancellationToken)
    {
        var validation = await ValidateTokenAsync(token, cancellationToken);
        return validation.Error is not null
            ? ApiResult<QrResolveResponseDto>.Failure(validation.Error.Code, validation.Error.Message, validation.StatusCode)
            : ApiResult<QrResolveResponseDto>.Success(ToResolveDto(validation.Value!));
    }

    public async Task<ApiResult<QrScanResponseDto>> RecordScanAsync(string token, QrScanRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        _ = request;
        var validation = await ValidateTokenAsync(token, cancellationToken);
        var scannedAtUtc = DateTime.UtcNow;
        var scanEvent = new QrScanEvent
        {
            Id = Guid.NewGuid(),
            QrCodeRecordId = validation.Value?.Id,
            Token = token,
            ScannedAtUtc = scannedAtUtc,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            WasSuccessful = validation.IsSuccess,
            FailureReason = validation.Error?.Message
        };

        await qrCodeRepository.AddScanEventAsync(scanEvent, cancellationToken);
        await qrCodeRepository.SaveChangesAsync(cancellationToken);

        if (!validation.IsSuccess)
        {
            return ApiResult<QrScanResponseDto>.Failure(
                validation.Error!.Code,
                validation.Error.Message,
                validation.StatusCode,
                new QrScanResponseDto(scanEvent.Id, false, validation.Error.Message, scannedAtUtc, null));
        }

        return ApiResult<QrScanResponseDto>.Success(new QrScanResponseDto(scanEvent.Id, true, null, scannedAtUtc, ToResolveDto(validation.Value!)));
    }

    public async Task<ApiResult<QrCodeResponseDto>> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var qrCode = await qrCodeRepository.GetByIdAsync(id, cancellationToken);
        if (qrCode is null)
        {
            return ApiResult<QrCodeResponseDto>.Failure("qr_code_not_found", "QR code was not found.", StatusCodes.Status404NotFound);
        }

        qrCode.IsActive = false;
        await qrCodeRepository.SaveChangesAsync(cancellationToken);
        return ApiResult<QrCodeResponseDto>.Success(ToDto(qrCode));
    }

    private async Task<string> GenerateUniqueTokenAsync(CancellationToken cancellationToken)
    {
        string token;
        do
        {
            token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-", StringComparison.Ordinal)
                .Replace("/", "_", StringComparison.Ordinal)
                .TrimEnd('=');
        }
        while (await qrCodeRepository.TokenExistsAsync(token, cancellationToken));

        return token;
    }

    private async Task<ApiResult<QrCodeRecord>> ValidateTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ApiResult<QrCodeRecord>.Failure("validation_error", "Token is required.", StatusCodes.Status400BadRequest);
        }

        var qrCode = await qrCodeRepository.GetByTokenAsync(token, cancellationToken);
        if (qrCode is null)
        {
            return ApiResult<QrCodeRecord>.Failure("qr_code_not_found", "QR code token was not found.", StatusCodes.Status404NotFound);
        }

        if (!qrCode.IsActive)
        {
            return ApiResult<QrCodeRecord>.Failure("qr_code_inactive", "QR code is inactive.", StatusCodes.Status400BadRequest);
        }

        if (qrCode.ExpiresAtUtc is not null && qrCode.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return ApiResult<QrCodeRecord>.Failure("qr_code_expired", "QR code has expired.", StatusCodes.Status400BadRequest);
        }

        return ApiResult<QrCodeRecord>.Success(qrCode);
    }

    private static QrCodeResponseDto ToDto(QrCodeRecord qrCode) =>
        new(qrCode.Id, qrCode.PackageId, qrCode.Token, qrCode.PayloadJson, qrCode.IsActive, qrCode.CreatedAtUtc, qrCode.ExpiresAtUtc);

    private static QrResolveResponseDto ToResolveDto(QrCodeRecord qrCode) =>
        new(qrCode.Id, qrCode.Package.Id, qrCode.Package.PackageReference, qrCode.Package.Description, qrCode.Package.Status, qrCode.CreatedAtUtc, qrCode.ExpiresAtUtc);
}
