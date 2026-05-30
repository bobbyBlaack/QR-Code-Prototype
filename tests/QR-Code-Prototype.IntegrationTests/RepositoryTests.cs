using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QR_Code_Prototype.Data;
using QR_Code_Prototype.Domain.Entities;
using QR_Code_Prototype.Domain.Enums;
using QR_Code_Prototype.Repositories;

namespace QR_Code_Prototype.IntegrationTests;

public sealed class RepositoryTests
{
    [Fact]
    public async Task PackageRepository_returns_newest_packages_first_and_paginates()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var repository = new PackageRepository(fixture.Context);
        var oldest = NewPackage("PKG-1", DateTime.UtcNow.AddMinutes(-3));
        var middle = NewPackage("PKG-2", DateTime.UtcNow.AddMinutes(-2));
        var newest = NewPackage("PKG-3", DateTime.UtcNow.AddMinutes(-1));
        await fixture.Context.Packages.AddRangeAsync(oldest, middle, newest);
        await fixture.Context.SaveChangesAsync();

        var all = await repository.GetAllAsync(CancellationToken.None);
        var page = await repository.GetPageAsync(1, 1, CancellationToken.None);
        var count = await repository.CountAsync(CancellationToken.None);

        Assert.Equal(["PKG-3", "PKG-2", "PKG-1"], all.Select(package => package.PackageReference).ToArray());
        Assert.Equal("PKG-2", page.Single().PackageReference);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Package_reference_token_and_email_are_unique()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var package = NewPackage("PKG-10001", DateTime.UtcNow);
        await fixture.Context.Packages.AddRangeAsync(package, NewPackage("PKG-10001", DateTime.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task QrCodeRepository_loads_package_for_token_resolution()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var package = NewPackage("PKG-10001", DateTime.UtcNow);
        var qrCode = new QrCodeRecord
        {
            Id = Guid.NewGuid(),
            Package = package,
            PackageId = package.Id,
            Token = "token-1",
            PayloadJson = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await fixture.Context.Packages.AddAsync(package);
        await fixture.Context.QrCodeRecords.AddAsync(qrCode);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();
        var repository = new QrCodeRepository(fixture.Context);

        var result = await repository.GetByTokenAsync("token-1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Package);
        Assert.Equal("PKG-10001", result.Package.PackageReference);
    }

    [Fact]
    public async Task Deleting_package_cascades_qr_records_and_preserves_scan_history()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var package = NewPackage("PKG-10001", DateTime.UtcNow);
        var qrCode = new QrCodeRecord
        {
            Id = Guid.NewGuid(),
            Package = package,
            PackageId = package.Id,
            Token = "token-1",
            PayloadJson = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        var scan = new QrScanEvent
        {
            Id = Guid.NewGuid(),
            QrCodeRecord = qrCode,
            QrCodeRecordId = qrCode.Id,
            Token = qrCode.Token,
            ScannedAtUtc = DateTime.UtcNow,
            WasSuccessful = true
        };
        await fixture.Context.AddRangeAsync(package, qrCode, scan);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.Packages.Remove(package);
        await fixture.Context.SaveChangesAsync();

        Assert.Empty(await fixture.Context.QrCodeRecords.ToListAsync());
        var storedScan = await fixture.Context.QrScanEvents.SingleAsync();
        Assert.Null(storedScan.QrCodeRecordId);
        Assert.Equal("token-1", storedScan.Token);
    }

    private static Package NewPackage(string reference, DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            PackageReference = reference,
            Status = PackageStatus.Created,
            CreatedAtUtc = createdAtUtc
        };

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SqliteFixture(SqliteConnection connection, AppDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public AppDbContext Context { get; }

        public static async Task<SqliteFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new SqliteFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
