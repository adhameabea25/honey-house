using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(AppDbContext db) : GenericRepository<Category>(db), ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(bool activeOnly, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(c => !activeOnly || (c.IsActive && (c.ParentCategory == null || c.ParentCategory.IsActive)))
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync(ct);

    // Unique index includes soft-deleted rows, so they must be checked too.
    public Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken ct = default) =>
        Set.IgnoreQueryFilters().AnyAsync(c => c.Slug == slug && c.Id != excludeId, ct);

    public async Task<bool> HasChildrenOrProductsAsync(int id, CancellationToken ct = default) =>
        await Set.AnyAsync(c => c.ParentCategoryId == id, ct) || await Db.Products.AnyAsync(p => p.CategoryId == id, ct);

    public async Task<IReadOnlyList<int>> GetSelfAndChildIdsAsync(int id, CancellationToken ct = default) =>
        await Set.Where(c => c.Id == id || c.ParentCategoryId == id).Select(c => c.Id).ToListAsync(ct);
}

public sealed class ProductRepository(AppDbContext db) : GenericRepository<Product>(db), IProductRepository
{
    public async Task<(IReadOnlyList<ProductSummary> Items, int Total)> SearchAsync(ProductSearchCriteria c, CancellationToken ct = default)
    {
        var q = Set.AsNoTracking().AsQueryable();

        if (c.ActiveOnly) q = q.Where(p => p.IsActive && p.Category.IsActive && p.Variants.Any(v => v.IsActive));
        if (!string.IsNullOrWhiteSpace(c.Search))
        {
            var term = c.Search;
            q = q.Where(p => p.NameAr.Contains(term) || p.NameEn.Contains(term)
                             || (p.DescriptionAr != null && p.DescriptionAr.Contains(term))
                             || (p.DescriptionEn != null && p.DescriptionEn.Contains(term))
                             || p.Variants.Any(v => v.Sku == term));
        }
        if (c.CategoryIds is { Count: > 0 } ids) q = q.Where(p => ids.Contains(p.CategoryId));
        if (c.ProductIds is { } pids) q = q.Where(p => pids.Contains(p.Id));
        if (c.FeaturedOnly) q = q.Where(p => p.IsFeatured);
        if (c.InStockOnly) q = q.Where(p => p.Variants.Any(v => v.IsActive && v.StockQuantity > 0));
        if (c.MinPrice is { } min) q = q.Where(p => p.Variants.Any(v => v.IsActive && v.Price >= min));
        if (c.MaxPrice is { } max) q = q.Where(p => p.Variants.Any(v => v.IsActive && v.Price <= max));

        var total = await q.CountAsync(ct);

        q = c.Sort switch
        {
            ProductSort.PriceAsc => q.OrderBy(p => p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price)).ThenBy(p => p.Id),
            ProductSort.PriceDesc => q.OrderByDescending(p => p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price)).ThenBy(p => p.Id),
            ProductSort.Rating => q.OrderByDescending(p => p.AverageRating).ThenByDescending(p => p.ReviewCount).ThenBy(p => p.Id),
            ProductSort.Name => q.OrderBy(p => p.NameEn).ThenBy(p => p.Id),
            ProductSort.BestSelling => q.OrderByDescending(p =>
                    Db.OrderItems.Where(i => i.ProductId == p.Id && i.Order.Status != OrderStatus.Cancelled).Sum(i => (int?)i.Quantity) ?? 0)
                .ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
        };

        var items = await q.Skip(c.Skip).Take(c.Take).Select(p => new ProductSummary(
            p.Id, p.NameAr, p.NameEn, p.Slug, p.CategoryId, p.Category.NameAr, p.Category.NameEn, p.Category.Slug,
            p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
            p.Variants.Where(v => v.IsActive).Min(v => (decimal?)v.Price) ?? 0,
            p.Variants.Where(v => v.IsActive).Max(v => (decimal?)v.Price) ?? 0,
            p.Variants.Where(v => v.IsActive && v.CompareAtPrice != null).Min(v => v.CompareAtPrice),
            p.AverageRating, p.ReviewCount, p.IsFeatured, p.IsActive,
            p.Variants.Any(v => v.IsActive && v.StockQuantity > 0),
            p.Variants.Sum(v => (int?)v.StockQuantity) ?? 0)).ToListAsync(ct);

        return (items, total);
    }

    public Task<Product?> GetDetailsAsync(int id, bool activeOnly, CancellationToken ct = default) =>
        WithDetails(activeOnly).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetDetailsBySlugAsync(string slug, bool activeOnly, CancellationToken ct = default) =>
        WithDetails(activeOnly).FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken ct = default) =>
        Set.IgnoreQueryFilters().AnyAsync(p => p.Slug == slug && p.Id != excludeId, ct);

    public Task<bool> SkuExistsAsync(string sku, int? excludeVariantId, CancellationToken ct = default)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return Db.ProductVariants.AnyAsync(v => v.Sku == normalized && v.Id != excludeVariantId, ct);
    }

    public Task<ProductVariant?> GetVariantAsync(int variantId, CancellationToken ct = default) =>
        Db.ProductVariants.Include(v => v.Product).FirstOrDefaultAsync(v => v.Id == variantId && !v.Product.IsDeleted, ct);

    private IQueryable<Product> WithDetails(bool activeOnly)
    {
        var q = Set.Include(p => p.Category).Include(p => p.Variants).Include(p => p.Images).AsQueryable();
        return activeOnly ? q.Where(p => p.IsActive && p.Category.IsActive) : q;
    }
}

public sealed class ReviewRepository(AppDbContext db) : GenericRepository<Review>(db), IReviewRepository
{
    public async Task<(IReadOnlyList<Review> Items, int Total)> SearchAsync(ReviewSearchCriteria c, CancellationToken ct = default)
    {
        var q = Set.AsNoTracking().AsQueryable();
        if (c.ProductId is { } pid) q = q.Where(r => r.ProductId == pid);
        if (c.Approved is { } approved) q = q.Where(r => r.IsApproved == approved);
        var total = await q.CountAsync(ct);
        var items = await q.Include(r => r.User).Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).Skip(c.Skip).Take(c.Take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<ReviewSummary> GetSummaryAsync(int productId, CancellationToken ct = default)
    {
        var counts = await Set.Where(r => r.ProductId == productId && r.IsApproved)
            .GroupBy(r => r.Rating).Select(g => new { Rating = g.Key, Count = g.Count() }).ToListAsync(ct);
        var total = counts.Sum(x => x.Count);
        var avg = total == 0 ? 0 : Math.Round((decimal)counts.Sum(x => x.Rating * x.Count) / total, 2);
        var distribution = Enumerable.Range(1, 5).ToDictionary(i => i, i => counts.FirstOrDefault(x => x.Rating == i)?.Count ?? 0);
        return new ReviewSummary(avg, total, distribution);
    }

    public async Task RecalculateProductRatingAsync(int productId, CancellationToken ct = default)
    {
        var summary = await GetSummaryAsync(productId, ct);
        await Db.Products.IgnoreQueryFilters().Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.AverageRating, summary.Average).SetProperty(p => p.ReviewCount, summary.Count), ct);
    }
}

public sealed class FavoriteRepository(AppDbContext db) : GenericRepository<Favorite>(db), IFavoriteRepository
{
    public async Task<IReadOnlyList<int>> GetProductIdsAsync(string userId, CancellationToken ct = default) =>
        await Set.Where(f => f.UserId == userId).OrderByDescending(f => f.CreatedAt).Select(f => f.ProductId).ToListAsync(ct);
}
