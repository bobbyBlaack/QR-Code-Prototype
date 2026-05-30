using Microsoft.EntityFrameworkCore;
using QR_Code_Prototype.Data;
using QR_Code_Prototype.Domain.Entities;

namespace QR_Code_Prototype.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.AppUsers.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task AddAsync(AppUser user, CancellationToken cancellationToken) =>
        dbContext.AppUsers.AddAsync(user, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
