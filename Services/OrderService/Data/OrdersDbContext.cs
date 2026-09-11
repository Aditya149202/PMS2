using Microsoft.EntityFrameworkCore;
using OrderService.Entities;

namespace OrderService.Data;


public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentIntent>(entity =>
        {
            entity.ToTable("PaymentIntents");

            entity.Property(p => p.RazorpayOrderId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.RazorpayPaymentId)
                .HasMaxLength(100);

            entity.Property(p => p.Amount)
                .HasColumnType("decimal(10,2)");

            entity.Property(p => p.Status)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            

            entity.HasIndex(p => p.RazorpayOrderId).IsUnique();
            entity.HasIndex(p => p.DoctorId);
            entity.HasIndex(p => new { p.Status, p.CreatedAt });

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_PaymentIntents_Status",
                "Status IN ('CREATED','PAID','FAILED','EXPIRED')"));
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.Property(o => o.DoctorNameSnapshot)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(o => o.Status)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(o => o.CancelledBy)
                .HasMaxLength(10);

            entity.Property(o => o.TotalAmount)
                .HasColumnType("decimal(10,2)");

            entity.Property(o => o.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(o => o.DoctorId);
            entity.HasIndex(o => o.Status);
            entity.HasIndex(o => new { o.DoctorId, o.Status });
            entity.HasIndex(o => new { o.Status, o.CreatedAt, o.VerifiedAt });

            entity.HasIndex(o => o.PaymentIntentId)
                .IsUnique();

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Orders_Status",
                "Status IN ('NEW','VERIFIED','COMPLETED','CANCELLED')"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Orders_CancelledBy",
                "CancelledBy IN ('ADMIN','SYSTEM','DOCTOR')"));

            entity.HasOne(o => o.PaymentIntent)
                .WithOne(pi => pi.Order)
                .HasForeignKey<Order>(o => o.PaymentIntentId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");

            entity.Property(oi => oi.UnitPriceAtOrder)
                .HasColumnType("decimal(10,2)");

            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}