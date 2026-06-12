using Microsoft.EntityFrameworkCore;
using ShippingExitSystem.Models;

namespace ShippingExitSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ExpectedProduct> ExpectedProducts { get; set; }
        public DbSet<ScannedItem> ScannedItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Shipment>()
                .HasIndex(s => s.ShipmentNumber)
                .IsUnique();

            modelBuilder.Entity<ExpectedProduct>()
                .HasOne(ep => ep.Shipment)
                .WithMany(s => s.ExpectedProducts)
                .HasForeignKey(ep => ep.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExpectedProduct>()
                .HasOne(ep => ep.CreatedByUser)
                .WithMany()
                .HasForeignKey(ep => ep.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ScannedItem>()
                .HasOne(si => si.ExpectedProduct)
                .WithMany(ep => ep.ScannedItems)
                .HasForeignKey(si => si.ExpectedProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}