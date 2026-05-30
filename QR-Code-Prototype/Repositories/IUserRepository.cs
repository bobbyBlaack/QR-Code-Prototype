using QR_Code_Prototype.Domain.Entities;

namespace QR_Code_Prototype.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(AppUser user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
