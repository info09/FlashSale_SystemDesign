using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// DbContext chinh cua PostgreSQLPrimary (ghi Order + Outbox + User).
/// Kethua IdentityDbContext de ho tro ASP.NET Core Identity.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: PostgreSQLPrimary, User, Outbox, Order.
/// </summary>
public class PostgreSQLPrimaryContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public PostgreSQLPrimaryContext(DbContextOptions<PostgreSQLPrimaryContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<Outbox> Outboxes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Dat ten bang va ten cot theo PascalCase (quyet da chot, docs/decisions.md)
        // EF Core mac dinh su dung ten property (PascalCase) lam ten cot, khong can HasColumnName.
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("User");
        });

        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Order");
        });

        builder.Entity<Outbox>(entity =>
        {
            entity.ToTable("Outbox");
            entity.HasIndex(o => o.ProcessingStatus);
            entity.HasIndex(o => o.OccurredOn);
        });

        builder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("Role");
        });
    }
}
