using Microsoft.EntityFrameworkCore;
using QR_Code_Prototype.Data;
using QR_Code_Prototype.Domain.Entities;

namespace QR_Code_Prototype.Repositories;

public sealed class PackageRepository(AppDbContext dbContext) : IPackageRepository
{
    public async Task<IReadOnlyList<Package>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Packages
            .AsNoTracking()
            .OrderByDescending(package => package.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Package>> GetPageAsync(int skip, int take, CancellationToken cancellationToken) =>
        await dbContext.Packages
            .AsNoTracking()
            .OrderByDescending(package => package.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        dbContext.Packages.AsNoTracking().CountAsync(cancellationToken);

    public Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Packages.FirstOrDefaultAsync(package => package.Id == id, cancellationToken);

    public Task<Package?> GetByReferenceAsync(string packageReference, CancellationToken cancellationToken) =>
        dbContext.Packages
            .AsNoTracking()
            .FirstOrDefaultAsync(package => package.PackageReference == packageReference, cancellationToken);

    public Task AddAsync(Package package, CancellationToken cancellationToken) =>
        dbContext.Packages.AddAsync(package, cancellationToken).AsTask();

    public void Remove(Package package) => dbContext.Packages.Remove(package);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
