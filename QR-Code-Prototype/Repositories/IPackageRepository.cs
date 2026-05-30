using QR_Code_Prototype.Domain.Entities;

namespace QR_Code_Prototype.Repositories;

public interface IPackageRepository
{
    Task<IReadOnlyList<Package>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Package>> GetPageAsync(int skip, int take, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task<Package?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Package?> GetByReferenceAsync(string packageReference, CancellationToken cancellationToken);
    Task AddAsync(Package package, CancellationToken cancellationToken);
    void Remove(Package package);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
