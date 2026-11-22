namespace HoneyHouse.Application.Features.Catalog;

// ---------- Public (localized) ----------
public sealed record CategoryDto(int Id, string Name, string Slug, string? Description, string? ImageUrl, int? ParentCategoryId, IReadOnlyList<CategoryDto> SubCategories);

public sealed record CategoryRefDto(int Id, string Name, string Slug);

public sealed record ProductListItemDto(
    int Id, string Name, string Slug, CategoryRefDto Category, string? MainImageUrl,
    decimal MinPrice, decimal MaxPrice, decimal? CompareAtPrice, string Currency,
    decimal AverageRating, int ReviewCount, bool IsFeatured, bool InStock);

public sealed record VariantDto(int Id, string Sku, int WeightGrams, string Label, decimal Price, decimal? CompareAtPrice, bool InStock, int AvailableQuantity);

public sealed record ProductImageDto(int Id, string Url, string? AltText, bool IsMain, int SortOrder);

public sealed record ProductDetailDto(
    int Id, string Name, string? Description, string Slug, string? Origin, CategoryRefDto Category,
    IReadOnlyList<ProductImageDto> Images, IReadOnlyList<VariantDto> Variants, string Currency,
    decimal AverageRating, int ReviewCount, bool IsFeatured);

public sealed record ProductQuery
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool InStock { get; init; }
    public bool Featured { get; init; }
    /// <summary>newest | price_asc | price_desc | rating | bestselling | name</summary>
    public string? Sort { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// ---------- Admin (bilingual) ----------
public sealed record AdminCategoryDto(int Id, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn,
    string Slug, string? ImageUrl, int? ParentCategoryId, int SortOrder, bool IsActive);

public sealed record CategoryUpsertRequest(string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn,
    string? Slug, string? ImageUrl, int? ParentCategoryId, int SortOrder = 0, bool IsActive = true);

public sealed record VariantUpsertRequest(string Sku, int WeightGrams, string LabelAr, string LabelEn, decimal Price,
    decimal? CompareAtPrice, int StockQuantity = 0, int LowStockThreshold = 5, bool IsActive = true);

public sealed record CreateProductRequest(int CategoryId, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn,
    string? Slug, string? Origin, bool IsActive, bool IsFeatured, IReadOnlyList<VariantUpsertRequest> Variants);

public sealed record UpdateProductRequest(int CategoryId, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn,
    string? Slug, string? Origin, bool IsActive, bool IsFeatured);

public sealed record AdminVariantDto(int Id, string Sku, int WeightGrams, string LabelAr, string LabelEn, decimal Price,
    decimal? CompareAtPrice, int StockQuantity, int LowStockThreshold, bool IsActive, bool IsLowStock);

public sealed record AdminProductDto(int Id, int CategoryId, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn,
    string Slug, string? Origin, bool IsActive, bool IsFeatured, decimal AverageRating, int ReviewCount,
    IReadOnlyList<AdminVariantDto> Variants, IReadOnlyList<ProductImageDto> Images, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record AdminProductListItemDto(int Id, string NameAr, string NameEn, string Slug, int CategoryId, string CategoryNameAr,
    string CategoryNameEn, string? MainImageUrl, decimal MinPrice, decimal MaxPrice, int TotalStock, bool IsActive, bool IsFeatured);

public sealed record UploadedImage(Stream Content, string FileName, string ContentType, long Length);
