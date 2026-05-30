using QR_Code_Prototype.Domain.Entities;

namespace QR_Code_Prototype.Repositories;

public interface IQrCodeRepository
{
    Task<QrCodeRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<QrCodeRecord?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken);
    Task AddAsync(QrCodeRecord qrCodeRecord, CancellationToken cancellationToken);
    Task AddScanEventAsync(QrScanEvent scanEvent, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
