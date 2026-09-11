using Microsoft.EntityFrameworkCore;
using UserAuthService.Entities;
namespace UserAuthService.Data;


public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.Property(r => r.Name)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(r => r.Name)
                .IsUnique();

            entity.HasData(
                new Role { Id = 1, Name = "ADMIN" },
                new Role { Id = 2, Name = "DOCTOR" }
            );
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.Property(u => u.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(u => u.RoleId);

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.Property(rt => rt.TokenHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(rt => rt.IssuedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");

            entity.Property(prt => prt.TokenHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(prt => prt.TokenHash);
            entity.HasIndex(prt => prt.UserId);

            entity.HasOne(prt => prt.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(prt => prt.UserId);
        });
    }
}