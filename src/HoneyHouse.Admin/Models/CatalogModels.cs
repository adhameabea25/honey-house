using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Catalog;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HoneyHouse.Admin.Models;

public sealed class CategoryForm
{
    public int Id { get; set; }
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Slug { get; set; }
    public string? ImageUrl { get; set; }
    public int? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RemoveImage { get; set; }

    public IReadOnlyList<SelectListItem> Parents { get; set; } = [];

    public CategoryUpsertRequest ToRequest() => new(NameAr?.Trim() ?? "", NameEn?.Trim() ?? "", Blank(DescriptionAr), Blank(DescriptionEn),
        Blank(Slug), Blank(ImageUrl), ParentCategoryId, SortOrder, IsActive);

    public static CategoryForm From(AdminCategoryDto c) => new()
    {
        Id = c.Id, NameAr = c.NameAr, NameEn = c.NameEn, DescriptionAr = c.DescriptionAr, DescriptionEn = c.DescriptionEn,
        Slug = c.Slug, ImageUrl = c.ImageUrl, ParentCategoryId = c.ParentCategoryId, SortOrder = c.SortOrder, IsActive = c.IsActive
    };

    internal static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

public sealed class CategoryRow
{
    public required AdminCategoryDto Category { get; init; }
    public int Depth { get; init; }
    public string? ParentName { get; init; }
}

public sealed class ProductListViewModel
{
    public required PagedResult<AdminProductListItemDto> Result { get; init; }
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<SelectListItem> Categories { get; init; } = [];
}

public sealed class ProductForm
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Slug { get; set; }
    public string? Origin { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    /// <summary>First weight option, required when creating a product.</summary>
    public VariantForm Variant { get; set; } = new();

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
    public AdminProductDto? Existing { get; set; }

    public CreateProductRequest ToCreateRequest() => new(CategoryId, NameAr?.Trim() ?? "", NameEn?.Trim() ?? "",
        CategoryForm.Blank(DescriptionAr), CategoryForm.Blank(DescriptionEn), CategoryForm.Blank(Slug), CategoryForm.Blank(Origin),
        IsActive, IsFeatured, [Variant.ToRequest()]);

    public UpdateProductRequest ToUpdateRequest() => new(CategoryId, NameAr?.Trim() ?? "", NameEn?.Trim() ?? "",
        CategoryForm.Blank(DescriptionAr), CategoryForm.Blank(DescriptionEn), CategoryForm.Blank(Slug), CategoryForm.Blank(Origin),
        IsActive, IsFeatured);

    public static ProductForm From(AdminProductDto p) => new()
    {
        Id = p.Id, CategoryId = p.CategoryId, NameAr = p.NameAr, NameEn = p.NameEn, DescriptionAr = p.DescriptionAr,
        DescriptionEn = p.DescriptionEn, Slug = p.Slug, Origin = p.Origin, IsActive = p.IsActive, IsFeatured = p.IsFeatured, Existing = p
    };
}

public sealed class VariantForm
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Sku { get; set; } = "";
    public int WeightGrams { get; set; }
    public string LabelAr { get; set; } = "";
    public string LabelEn { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    /// <summary>Opening stock (new options only; later changes go through Inventory).</summary>
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;

    public string? ProductName { get; set; }

    public VariantUpsertRequest ToRequest() => new((Sku ?? "").Trim().ToUpperInvariant(), WeightGrams, LabelAr?.Trim() ?? "", LabelEn?.Trim() ?? "",
        Price, CompareAtPrice, StockQuantity, LowStockThreshold, IsActive);

    public static VariantForm From(int productId, AdminVariantDto v) => new()
    {
        Id = v.Id, ProductId = productId, Sku = v.Sku, WeightGrams = v.WeightGrams, LabelAr = v.LabelAr, LabelEn = v.LabelEn,
        Price = v.Price, CompareAtPrice = v.CompareAtPrice, StockQuantity = v.StockQuantity, LowStockThreshold = v.LowStockThreshold,
        IsActive = v.IsActive
    };
}
