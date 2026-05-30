using Microsoft.EntityFrameworkCore;
using QR_Code_Prototype.Data;
using QR_Code_Prototype.Domain.Entities;

namespace QR_Code_Prototype.Repositories;

public sealed class QrCodeRepository(AppDbContext dbContext) : IQrCodeRepository
{
    public Task<QrCodeRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.QrCodeRecords
            .Include(qrCode => qrCode.Package)
            .FirstOrDefaultAsync(qrCode => qrCode.Id == id, cancellationToken);

    public Task<QrCodeRecord?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        dbContext.QrCodeRecords
            .AsNoTracking()
            .Include(qrCode => qrCode.Package)
            .FirstOrDefaultAsync(qrCode => qrCode.Token == token, cancellationToken);

    public Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken) =>
        dbContext.QrCodeRecords.AnyAsync(qrCode => qrCode.Token == token, cancellationToken);

    public Task AddAsync(QrCodeRecord qrCodeRecord, CancellationToken cancellationToken) =>
        dbContext.QrCodeRecords.AddAsync(qrCodeRecord, cancellationToken).AsTask();

    public Task AddScanEventAsync(QrScanEvent scanEvent, CancellationToken cancellationToken) =>
        dbContext.QrScanEvents.AddAsync(scanEvent, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
