using HoneyHouse.Application.Common;
using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Interfaces.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<IReadOnlyList<Category>> GetAllAsync(bool activeOnly, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken ct = default);
    Task<bool> HasChildrenOrProductsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetSelfAndChildIdsAsync(int id, CancellationToken ct = default);
}

public enum ProductSort { Newest, PriceAsc, PriceDesc, Rating, BestSelling, Name }

public sealed record ProductSearchCriteria(
    string? Search,
    IReadOnlyList<int>? CategoryIds,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool InStockOnly,
    bool FeaturedOnly,
    ProductSort Sort,
    bool ActiveOnly,
    int Skip,
    int Take);

/// <summary>Flat bilingual projection used for product listings.</summary>
public sealed record ProductSummary(
    int Id,
    string NameAr,
    string NameEn,
    string Slug,
    int CategoryId,
    string CategoryNameAr,
    string CategoryNameEn,
    string CategorySlug,
    string? MainImageUrl,
    decimal MinPrice,
    decimal MaxPrice,
    decimal? CompareAtPrice,
    decimal AverageRating,
    int ReviewCount,
    bool IsFeatured,
    bool IsActive,
    bool InStock,
    int TotalStock);

public interface IProductRepository : IGenericRepository<Product>
{
    Task<(IReadOnlyList<ProductSummary> Items, int Total)> SearchAsync(ProductSearchCriteria criteria, CancellationToken ct = default);
    Task<Product?> GetDetailsAsync(int id, bool activeOnly, CancellationToken ct = default);
    Task<Product?> GetDetailsBySlugAsync(string slug, bool activeOnly, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, int? excludeVariantId, CancellationToken ct = default);
    Task<ProductVariant?> GetVariantAsync(int variantId, CancellationToken ct = default);
}
