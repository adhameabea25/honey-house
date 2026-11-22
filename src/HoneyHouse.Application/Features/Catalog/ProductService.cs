using HoneyHouse.Application.Common;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Features.Catalog;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<ProductListItemDto>> GetFeaturedAsync(int limit, CancellationToken ct = default);
    Task<Result<ProductDetailDto>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<ProductDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
}

public sealed class ProductService(IProductRepository products, ICategoryRepository categories, ILanguageContext lang) : IProductService
{
    public const string Currency = "EGP";

    public async Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query, CancellationToken ct = default)
    {
        var paging = new PagedRequest { Page = query.Page, PageSize = query.PageSize };
        IReadOnlyList<int>? categoryIds = query.CategoryId is { } cid ? await categories.GetSelfAndChildIdsAsync(cid, ct) : null;

        var (items, total) = await products.SearchAsync(new ProductSearchCriteria(
            query.Search?.Trim(), categoryIds, query.MinPrice, query.MaxPrice, query.InStock, query.Featured,
            ParseSort(query.Sort), ActiveOnly: true, paging.Skip, paging.PageSize), ct);

        return new PagedResult<ProductListItemDto>(items.Select(ToListItem).ToList(), paging.Page, paging.PageSize, total);
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetFeaturedAsync(int limit, CancellationToken ct = default)
    {
        var (items, _) = await products.SearchAsync(new ProductSearchCriteria(
            null, null, null, null, false, FeaturedOnly: true, ProductSort.Newest, ActiveOnly: true, 0, Math.Clamp(limit, 1, 50)), ct);
        return items.Select(ToListItem).ToList();
    }

    public async Task<Result<ProductDetailDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var product = await products.GetDetailsAsync(id, activeOnly: true, ct);
        return product is null ? Error.NotFound("product_not_found") : ToDetail(product);
    }

    public async Task<Result<ProductDetailDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var product = await products.GetDetailsBySlugAsync(slug, activeOnly: true, ct);
        return product is null ? Error.NotFound("product_not_found") : ToDetail(product);
    }

    public static ProductSort ParseSort(string? sort) => sort?.ToLowerInvariant() switch
    {
        "price_asc" => ProductSort.PriceAsc,
        "price_desc" => ProductSort.PriceDesc,
        "rating" => ProductSort.Rating,
        "bestselling" => ProductSort.BestSelling,
        "name" => ProductSort.Name,
        _ => ProductSort.Newest
    };

    private ProductListItemDto ToListItem(ProductSummary p) => new(
        p.Id, lang.Pick(p.NameAr, p.NameEn), p.Slug,
        new CategoryRefDto(p.CategoryId, lang.Pick(p.CategoryNameAr, p.CategoryNameEn), p.CategorySlug),
        p.MainImageUrl, p.MinPrice, p.MaxPrice, p.CompareAtPrice, Currency, p.AverageRating, p.ReviewCount, p.IsFeatured, p.InStock);

    private ProductDetailDto ToDetail(Product p) => new(
        p.Id, lang.Pick(p.NameAr, p.NameEn), lang.Pick(p.DescriptionAr ?? "", p.DescriptionEn ?? "") is { Length: > 0 } d ? d : null,
        p.Slug, p.Origin,
        new CategoryRefDto(p.Category.Id, lang.Pick(p.Category.NameAr, p.Category.NameEn), p.Category.Slug),
        p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder)
            .Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.IsMain, i.SortOrder)).ToList(),
        p.Variants.Where(v => v.IsActive).OrderBy(v => v.WeightGrams)
            .Select(v => new VariantDto(v.Id, v.Sku, v.WeightGrams, lang.Pick(v.LabelAr, v.LabelEn), v.Price, v.CompareAtPrice,
                v.StockQuantity > 0, Math.Min(v.StockQuantity, MaxQuantityPerItem))).ToList(),
        Currency, p.AverageRating, p.ReviewCount, p.IsFeatured);

    /// <summary>Clients never see more than this as available quantity (exact stock levels are internal).</summary>
    public const int MaxQuantityPerItem = 20;
}
