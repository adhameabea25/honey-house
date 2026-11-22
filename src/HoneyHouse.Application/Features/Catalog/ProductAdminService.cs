using HoneyHouse.Application.Common;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Application.Features.Catalog;

public interface IProductAdminService
{
    Task<PagedResult<AdminProductListItemDto>> SearchAsync(ProductQuery query, bool includeInactive, CancellationToken ct = default, bool? isActive = null);
    Task<Result<AdminProductDto>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<AdminProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<Result<AdminProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<AdminVariantDto>> AddVariantAsync(int productId, VariantUpsertRequest request, CancellationToken ct = default);
    Task<Result<AdminVariantDto>> UpdateVariantAsync(int variantId, VariantUpsertRequest request, CancellationToken ct = default);
    Task<Result> DeactivateVariantAsync(int variantId, CancellationToken ct = default);
    Task<Result<ProductImageDto>> AddImageAsync(int productId, UploadedImage image, bool isMain, CancellationToken ct = default);
    Task<Result> SetMainImageAsync(int productId, int imageId, CancellationToken ct = default);
    Task<Result> DeleteImageAsync(int productId, int imageId, CancellationToken ct = default);
}

public sealed class ProductAdminService(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork uow,
    IFileStorage files,
    ICurrentUser currentUser) : IProductAdminService
{
    public const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<PagedResult<AdminProductListItemDto>> SearchAsync(ProductQuery query, bool includeInactive, CancellationToken ct = default, bool? isActive = null)
    {
        var paging = new PagedRequest { Page = query.Page, PageSize = query.PageSize };
        IReadOnlyList<int>? categoryIds = query.CategoryId is { } cid ? await categories.GetSelfAndChildIdsAsync(cid, ct) : null;
        var (items, total) = await products.SearchAsync(new ProductSearchCriteria(
            query.Search?.Trim(), categoryIds, query.MinPrice, query.MaxPrice, query.InStock, query.Featured,
            ProductService.ParseSort(query.Sort), ActiveOnly: !includeInactive, paging.Skip, paging.PageSize, IsActive: isActive), ct);

        return new PagedResult<AdminProductListItemDto>(items.Select(p => new AdminProductListItemDto(
            p.Id, p.NameAr, p.NameEn, p.Slug, p.CategoryId, p.CategoryNameAr, p.CategoryNameEn, p.MainImageUrl,
            p.MinPrice, p.MaxPrice, p.TotalStock, p.IsActive, p.IsFeatured)).ToList(), paging.Page, paging.PageSize, total);
    }

    public async Task<Result<AdminProductDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var product = await products.GetDetailsAsync(id, activeOnly: false, ct);
        return product is null ? Error.NotFound("product_not_found") : ToDto(product);
    }

    public async Task<Result<AdminProductDto>> CreateAsync(CreateProductRequest r, CancellationToken ct = default)
    {
        if (await categories.GetByIdAsync(r.CategoryId, ct) is null) return Error.Validation("category_not_found", "categoryId");

        var slug = string.IsNullOrWhiteSpace(r.Slug) ? SlugHelper.Generate(r.NameEn) : r.Slug.Trim();
        if (await products.SlugExistsAsync(slug, null, ct)) return Error.Conflict("slug_taken");
        foreach (var v in r.Variants)
            if (await products.SkuExistsAsync(v.Sku, null, ct)) return Error.Conflict("sku_taken", v.Sku);

        var product = new Product
        {
            CategoryId = r.CategoryId,
            NameAr = r.NameAr.Trim(), NameEn = r.NameEn.Trim(),
            DescriptionAr = r.DescriptionAr, DescriptionEn = r.DescriptionEn,
            Slug = slug, Origin = r.Origin, IsActive = r.IsActive, IsFeatured = r.IsFeatured,
            Variants = r.Variants.Select(NewVariant).ToList()
        };
        await products.AddAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        await RecordInitialStockAsync(product.Variants, ct);
        return (await GetAsync(product.Id, ct));
    }

    public async Task<Result<AdminProductDto>> UpdateAsync(int id, UpdateProductRequest r, CancellationToken ct = default)
    {
        var product = await products.GetDetailsAsync(id, activeOnly: false, ct);
        if (product is null) return Error.NotFound("product_not_found");
        if (product.CategoryId != r.CategoryId && await categories.GetByIdAsync(r.CategoryId, ct) is null)
            return Error.Validation("category_not_found", "categoryId");

        var slug = string.IsNullOrWhiteSpace(r.Slug) ? product.Slug : r.Slug.Trim();
        if (slug != product.Slug && await products.SlugExistsAsync(slug, id, ct)) return Error.Conflict("slug_taken");

        product.CategoryId = r.CategoryId;
        product.NameAr = r.NameAr.Trim();
        product.NameEn = r.NameEn.Trim();
        product.DescriptionAr = r.DescriptionAr;
        product.DescriptionEn = r.DescriptionEn;
        product.Slug = slug;
        product.Origin = r.Origin;
        product.IsActive = r.IsActive;
        product.IsFeatured = r.IsFeatured;
        await uow.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct);
        if (product is null) return Error.NotFound("product_not_found");
        products.Remove(product); // soft delete
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<AdminVariantDto>> AddVariantAsync(int productId, VariantUpsertRequest r, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(productId, ct);
        if (product is null) return Error.NotFound("product_not_found");
        if (await products.SkuExistsAsync(r.Sku, null, ct)) return Error.Conflict("sku_taken", r.Sku);

        var variant = NewVariant(r);
        variant.ProductId = productId;
        await uow.Repository<ProductVariant>().AddAsync(variant, ct);
        await uow.SaveChangesAsync(ct);
        await RecordInitialStockAsync([variant], ct);
        return ToDto(variant);
    }

    public async Task<Result<AdminVariantDto>> UpdateVariantAsync(int variantId, VariantUpsertRequest r, CancellationToken ct = default)
    {
        var variant = await products.GetVariantAsync(variantId, ct);
        if (variant is null) return Error.NotFound("variant_not_found");
        if (!string.Equals(variant.Sku, r.Sku, StringComparison.OrdinalIgnoreCase) && await products.SkuExistsAsync(r.Sku, variantId, ct))
            return Error.Conflict("sku_taken", r.Sku);
        if (!r.IsActive && variant.IsActive && !await HasOtherActiveVariantAsync(variant, ct))
            return Error.Business("product_needs_variant");

        variant.Sku = r.Sku.Trim().ToUpperInvariant();
        variant.WeightGrams = r.WeightGrams;
        variant.LabelAr = r.LabelAr.Trim();
        variant.LabelEn = r.LabelEn.Trim();
        variant.Price = r.Price;
        variant.CompareAtPrice = r.CompareAtPrice;
        variant.LowStockThreshold = r.LowStockThreshold;
        variant.IsActive = r.IsActive;
        // Stock is intentionally NOT changed here: use the inventory adjustment endpoint so every change is audited.
        await uow.SaveChangesAsync(ct);
        return ToDto(variant);
    }

    public async Task<Result> DeactivateVariantAsync(int variantId, CancellationToken ct = default)
    {
        var variant = await products.GetVariantAsync(variantId, ct);
        if (variant is null) return Error.NotFound("variant_not_found");
        if (!variant.IsActive) return Result.Success();
        if (!await HasOtherActiveVariantAsync(variant, ct)) return Error.Business("product_needs_variant");

        variant.IsActive = false;
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ProductImageDto>> AddImageAsync(int productId, UploadedImage image, bool isMain, CancellationToken ct = default)
    {
        if (!AllowedImageTypes.Contains(image.ContentType) || image.Length is <= 0 or > MaxImageBytes)
            return Error.Validation("image_invalid", "file");

        var product = await products.GetDetailsAsync(productId, activeOnly: false, ct);
        if (product is null) return Error.NotFound("product_not_found");

        var url = await files.SaveAsync(image.Content, image.FileName, "products", ct);
        var makeMain = isMain || product.Images.Count == 0;
        if (makeMain) foreach (var img in product.Images) img.IsMain = false;

        var entity = new ProductImage
        {
            ProductId = productId, Url = url, AltText = product.NameEn, IsMain = makeMain,
            SortOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1
        };
        product.Images.Add(entity);
        await uow.SaveChangesAsync(ct);
        return new ProductImageDto(entity.Id, entity.Url, entity.AltText, entity.IsMain, entity.SortOrder);
    }

    public async Task<Result> SetMainImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        var product = await products.GetDetailsAsync(productId, activeOnly: false, ct);
        if (product is null) return Error.NotFound("product_not_found");
        if (product.Images.All(i => i.Id != imageId)) return Error.NotFound("image_not_found");
        foreach (var img in product.Images) img.IsMain = img.Id == imageId;
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        var product = await products.GetDetailsAsync(productId, activeOnly: false, ct);
        if (product is null) return Error.NotFound("product_not_found");
        var image = product.Images.FirstOrDefault(i => i.Id == imageId);
        if (image is null) return Error.NotFound("image_not_found");

        product.Images.Remove(image);
        uow.Repository<ProductImage>().Remove(image);
        if (image.IsMain && product.Images.OrderBy(i => i.SortOrder).FirstOrDefault() is { } next) next.IsMain = true;
        await uow.SaveChangesAsync(ct);
        await files.DeleteAsync(image.Url, ct);
        return Result.Success();
    }

    private async Task<bool> HasOtherActiveVariantAsync(ProductVariant variant, CancellationToken ct) =>
        await uow.Repository<ProductVariant>().AnyAsync(v => v.ProductId == variant.ProductId && v.Id != variant.Id && v.IsActive, ct);

    private async Task RecordInitialStockAsync(IEnumerable<ProductVariant> variants, CancellationToken ct)
    {
        var txs = variants.Where(v => v.StockQuantity > 0).Select(v => new InventoryTransaction
        {
            VariantId = v.Id, QuantityChange = v.StockQuantity, StockAfter = v.StockQuantity,
            Type = InventoryTransactionType.Restock, Note = "Initial stock", CreatedByUserId = currentUser.UserId
        }).ToList();
        if (txs.Count == 0) return;
        await uow.Repository<InventoryTransaction>().AddRangeAsync(txs, ct);
        await uow.SaveChangesAsync(ct);
    }

    private static ProductVariant NewVariant(VariantUpsertRequest r) => new()
    {
        Sku = r.Sku.Trim().ToUpperInvariant(), WeightGrams = r.WeightGrams, LabelAr = r.LabelAr.Trim(), LabelEn = r.LabelEn.Trim(),
        Price = r.Price, CompareAtPrice = r.CompareAtPrice, StockQuantity = r.StockQuantity,
        LowStockThreshold = r.LowStockThreshold, IsActive = r.IsActive
    };

    private static AdminVariantDto ToDto(ProductVariant v) => new(v.Id, v.Sku, v.WeightGrams, v.LabelAr, v.LabelEn, v.Price,
        v.CompareAtPrice, v.StockQuantity, v.LowStockThreshold, v.IsActive, v.IsLowStock);

    private static AdminProductDto ToDto(Product p) => new(p.Id, p.CategoryId, p.NameAr, p.NameEn, p.DescriptionAr, p.DescriptionEn,
        p.Slug, p.Origin, p.IsActive, p.IsFeatured, p.AverageRating, p.ReviewCount,
        p.Variants.OrderBy(v => v.WeightGrams).Select(ToDto).ToList(),
        p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder)
            .Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.IsMain, i.SortOrder)).ToList(),
        p.CreatedAt, p.UpdatedAt);
}
