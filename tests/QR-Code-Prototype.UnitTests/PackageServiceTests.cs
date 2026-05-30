using QR_Code_Prototype.Contracts.Packages;
using QR_Code_Prototype.Domain.Entities;
using QR_Code_Prototype.Domain.Enums;
using QR_Code_Prototype.Repositories;
using QR_Code_Prototype.Services;

namespace QR_Code_Prototype.UnitTests;

public sealed class PackageServiceTests
{
    [Fact]
    public async Task CreateAsync_creates_package_with_trimmed_reference()
    {
        var repository = new FakePackageRepository();
        var service = new PackageService(repository);

        var result = await service.CreateAsync(new CreatePackageRequest
        {
            PackageReference = " PKG-10001 ",
            Description = "Battery box"
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal("PKG-10001", result.Value!.PackageReference);
        Assert.Single(repository.Packages);
        Assert.Equal(PackageStatus.Created, repository.Packages[0].Status);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_reference()
    {
        var repository = new FakePackageRepository();
        repository.Packages.Add(NewPackage("PKG-10001"));
        var service = new PackageService(repository);

        var result = await service.CreateAsync(new CreatePackageRequest
        {
            PackageReference = "PKG-10001"
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal("package_reference_exists", result.Error!.Code);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetPageAsync_rejects_invalid_pagination(int pageNumber, int pageSize)
    {
        var service = new PackageService(new FakePackageRepository());

        var result = await service.GetPageAsync(new PackageListPageRequest(pageNumber, pageSize), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Fact]
    public async Task GetPageAsync_returns_requested_page_metadata()
    {
        var repository = new FakePackageRepository();
        repository.Packages.AddRange([
            NewPackage("PKG-1", DateTime.UtcNow.AddMinutes(-3)),
            NewPackage("PKG-2", DateTime.UtcNow.AddMinutes(-2)),
            NewPackage("PKG-3", DateTime.UtcNow.AddMinutes(-1))
        ]);
        var service = new PackageService(repository);

        var result = await service.GetPageAsync(new PackageListPageRequest(2, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.PageNumber);
        Assert.Equal(2, result.Value.PageSize);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Single(result.Value.Items);
        Assert.Equal("PKG-1", result.Value.Items[0].PackageReference);
    }

    [Fact]
    public async Task UpdateStatusAsync_sets_delivered_timestamp()
    {
        var package = NewPackage("PKG-10001");
        var repository = new FakePackageRepository();
        repository.Packages.Add(package);
        var service = new PackageService(repository);

        var result = await service.UpdateStatusAsync(package.Id, new UpdatePackageStatusRequest
        {
            Status = PackageStatus.Delivered
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PackageStatus.Delivered, result.Value!.Status);
        Assert.NotNull(result.Value.DeliveredAtUtc);
    }

    private static Package NewPackage(string reference, DateTime? createdAtUtc = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            PackageReference = reference,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
            Status = PackageStatus.Created
        };

    private sealed class FakePackageRepository : IPackageRepository
    {
        public List<Package> Packages { get; } = [];

        public Task<IReadOnlyList<Package>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Package>>(Packages.OrderByDescending(package => package.CreatedAtUtc).ToList());

        public Task<IReadOnlyList<Package>> GetPageAsync(int skip, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Package>>(Packages
                .OrderByDescending(package => package.CreatedAtUtc)
                .Skip(skip)
                .Take(take)
                .ToList());

        public Task<int> CountAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Packages.Count);

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
}
