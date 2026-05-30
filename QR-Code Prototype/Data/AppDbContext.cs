using Microsoft.EntityFrameworkCore;
using QR_Code_Prototype.Domain.Entities;

namespace QR_Code_Prototype.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<QrCodeRecord> QrCodeRecords => Set<QrCodeRecord>();
    public DbSet<QrScanEvent> QrScanEvents => Set<QrScanEvent>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(package => package.Id);
            entity.Property(package => package.PackageReference).IsRequired().HasMaxLength(100);
            entity.Property(package => package.Description).HasMaxLength(500);
            entity.HasIndex(package => package.PackageReference).IsUnique();
            entity.HasMany(package => package.QrCodes)
                .WithOne(qrCode => qrCode.Package)
                .HasForeignKey(qrCode => qrCode.PackageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QrCodeRecord>(entity =>
        {
            entity.HasKey(qrCode => qrCode.Id);
            entity.Property(qrCode => qrCode.Token).IsRequired().HasMaxLength(128);
            entity.Property(qrCode => qrCode.PayloadJson).IsRequired();
            entity.HasIndex(qrCode => qrCode.Token).IsUnique();
            entity.HasMany(qrCode => qrCode.ScanEvents)
                .WithOne(scan => scan.QrCodeRecord)
                .HasForeignKey(scan => scan.QrCodeRecordId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QrScanEvent>(entity =>
        {
            entity.HasKey(scan => scan.Id);
            entity.Property(scan => scan.Token).HasMaxLength(128);
            entity.Property(scan => scan.IpAddress).HasMaxLength(64);
            entity.Property(scan => scan.UserAgent).HasMaxLength(512);
            entity.Property(scan => scan.FailureReason).HasMaxLength(200);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).IsRequired().HasMaxLength(256);
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });
    }
}
