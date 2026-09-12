using Microsoft.EntityFrameworkCore;

using SupplierInventoryService.Entities;

namespace SupplierInventoryService.Data;

public class SupplierInventoryDbContext : DbContext
{
    public SupplierInventoryDbContext(
        DbContextOptions<SupplierInventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");

            entity.Property(s => s.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(s => s.ContactInfo)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(s => s.Address)
                .HasMaxLength(255);
        });

        modelBuilder.Entity<Drug>(entity =>
        {
            entity.ToTable("Drugs");

            entity.Property(d => d.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(d => d.Price)
                .HasColumnType("decimal(10,2)");

            entity.Property(d => d.QuantityInStock)
                .HasDefaultValue(0);

            entity.Property(d => d.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(d => d.SupplierId);
            entity.HasIndex(d => d.IsActive);

            entity.HasOne(d => d.Supplier)
                .WithMany(s => s.Drugs)
                .HasForeignKey(d => d.SupplierId);
        });

        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("StockReservations");

            entity.Property(sr => sr.Status)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(sr => sr.ReservedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(sr => sr.PaymentIntentId);
            entity.HasIndex(sr => new { sr.Status, sr.ExpiresAt });

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_StockReservations_Status",
                "Status IN ('ACTIVE','RELEASED','COMMITTED')"));

            entity.HasOne(sr => sr.Drug)
                .WithMany(d => d.StockReservations)
                .HasForeignKey(sr => sr.DrugId);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sales");

            entity.Property(s => s.Amount)
                .HasColumnType("decimal(10,2)");

            entity.Property(s => s.SaleDate)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(s => s.OrderId)
                .IsUnique();
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("ProcessedEvents");

            entity.HasKey(pe => pe.OrderId);
            entity.Property(pe => pe.OrderId)
                .ValueGeneratedNever();
            entity.Property(pe => pe.ProcessedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }
}