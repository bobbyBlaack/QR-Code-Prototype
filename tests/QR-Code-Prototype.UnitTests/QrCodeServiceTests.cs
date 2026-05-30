using QR_Code_Prototype.Contracts.QrCodes;
using QR_Code_Prototype.Domain.Entities;
using QR_Code_Prototype.Domain.Enums;
using QR_Code_Prototype.Repositories;
using QR_Code_Prototype.Services;

namespace QR_Code_Prototype.UnitTests;

public sealed class QrCodeServiceTests
{
    [Fact]
    public async Task CreateForPackageAsync_generates_unique_url_safe_token()
    {
        var packageRepository = new FakePackageRepository();
        var package = NewPackage();
        packageRepository.Packages.Add(package);
        var qrRepository = new FakeQrCodeRepository();
        var service = new QrCodeService(packageRepository, qrRepository);

        var result = await service.CreateForPackageAsync(package.Id, new CreateQrCodeRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.DoesNotContain("+", result.Value!.Token);
        Assert.DoesNotContain("/", result.Value.Token);
        Assert.DoesNotContain("=", result.Value.Token);
        Assert.Contains(package.PackageReference, result.Value.PayloadJson);
    }

    [Fact]
    public async Task CreateForPackageAsync_rejects_missing_package()
    {
        var service = new QrCodeService(new FakePackageRepository(), new FakeQrCodeRepository());

        var result = await service.CreateForPackageAsync(Guid.NewGuid(), new CreateQrCodeRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal("package_not_found", result.Error!.Code);
    }

    [Fact]
    public async Task ResolveAsync_rejects_inactive_qr_code()
    {
        var package = NewPackage();
        var qrRepository = new FakeQrCodeRepository();
        qrRepository.QrCodes.Add(new QrCodeRecord
        {
            Id = Guid.NewGuid(),
            PackageId = package.Id,
            Package = package,
            Token = "inactive-token",
            PayloadJson = "{}",
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow
        });
        var service = new QrCodeService(new FakePackageRepository(), qrRepository);

        var result = await service.ResolveAsync("inactive-token", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("qr_code_inactive", result.Error!.Code);
    }

    [Fact]
    public async Task RecordScanAsync_records_failed_scan_for_unknown_token()
    {
        var qrRepository = new FakeQrCodeRepository();
        var service = new QrCodeService(new FakePackageRepository(), qrRepository);

        var result = await service.RecordScanAsync("missing-token", new QrScanRequest(), "127.0.0.1", "tests", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("qr_code_not_found", result.Error!.Code);
        Assert.Single(qrRepository.ScanEvents);
        Assert.False(qrRepository.ScanEvents[0].WasSuccessful);
        Assert.Equal("missing-token", qrRepository.ScanEvents[0].Token);
    }

    private static Package NewPackage() =>
        new()
        {
            Id = Guid.NewGuid(),
            PackageReference = "PKG-10001",
            Status = PackageStatus.Created,
            CreatedAtUtc = DateTime.UtcNow
        };

    private sealed class FakePackageRepository : IPackageRepository
    {
        public List<Package> Packages { get; } = [];

        public Task<IReadOnlyList<Package>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Package>>(Packages);

        public Task<IReadOnlyList<Package>> GetPageAsync(int skip, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Package>>(Packages.Skip(skip).Take(take).ToList());

        public Task<int> CountAsync(CancellationToken cancellationToken) => Task.FromResult(Packages.Count);

        public Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Packages.FirstOrDefault(package => package.Id == id));

        public Task<Package?> GetByReferenceAsync(string packageReference, CancellationToken cancellationToken) =>
            Task.FromResult(Packages.FirstOrDefault(package => package.PackageReference == packageReference));

        public Task AddAsync(Package package, CancellationToken cancellationToken)
        {
            Packages.Add(package);
            return Task.CompletedTask;
        }

        public void Remove(Package package) => Packages.Remove(package);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeQrCodeRepository : IQrCodeRepository
    {
        public List<QrCodeRecord> QrCodes { get; } = [];
        public List<QrScanEvent> ScanEvents { get; } = [];

        public Task<QrCodeRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(QrCodes.FirstOrDefault(qrCode => qrCode.Id == id));

        public Task<QrCodeRecord?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
            Task.FromResult(QrCodes.FirstOrDefault(qrCode => qrCode.Token == token));

        public Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken) =>
            Task.FromResult(QrCodes.Any(qrCode => qrCode.Token == token));

        public Task AddAsync(QrCodeRecord qrCodeRecord, CancellationToken cancellationToken)
        {
            QrCodes.Add(qrCodeRecord);
            return Task.CompletedTask;
        }

        public Task AddScanEventAsync(QrScanEvent scanEvent, CancellationToken cancellationToken)
        {
            ScanEvents.Add(scanEvent);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
