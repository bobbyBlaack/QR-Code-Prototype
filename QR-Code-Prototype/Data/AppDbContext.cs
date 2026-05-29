using Microsoft.EntityFrameworkCore;
using QR_Code_Prototype.Models;

namespace QR_Code_Prototype.Data
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<UserModel> User { get; set; }
        public DbSet<RolesModel> Roles { get; set; }
        public DbSet<PackagePassModel> PackagePass { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
}
