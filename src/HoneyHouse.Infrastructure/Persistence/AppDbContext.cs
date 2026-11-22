using System.Linq.Expressions;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider? clock = null)
    : IdentityDbContext<ApplicationUser>(options)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Governorate> Governorates => Set<Governorate>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;

            if (typeof(ISoftDelete).IsAssignableFrom(clr))
            {
                var p = Expression.Parameter(clr, "e");
                var filter = Expression.Lambda(
                    Expression.Not(Expression.Property(p, nameof(ISoftDelete.IsDeleted))), p);
                builder.Entity(clr).HasQueryFilter(filter);
            }

            foreach (var prop in entityType.GetProperties()
                         .Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?)))
            {
                if (prop.GetPrecision() is null) prop.SetPrecision(18);
                if (prop.GetScale() is null) prop.SetScale(2);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.Entity)
            {
                case BaseEntity e when entry.State == EntityState.Added:
                    if (e.CreatedAt == default) e.CreatedAt = now;
                    break;
                case BaseEntity e when entry.State == EntityState.Modified:
                    e.UpdatedAt = now;
                    break;
                case Favorite f when entry.State == EntityState.Added && f.CreatedAt == default:
                    f.CreatedAt = now;
                    break;
                case ApplicationUser u when entry.State == EntityState.Added && u.CreatedAt == default:
                    u.CreatedAt = now;
                    break;
            }

            if (entry.Entity is ISoftDelete sd && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                sd.IsDeleted = true;
                sd.DeletedAt = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
