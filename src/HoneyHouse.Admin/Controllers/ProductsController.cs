using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Models;
using HoneyHouse.Application.Features.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HoneyHouse.Admin.Controllers;

public sealed class ProductsController(IProductAdminService products, ICategoryService categories) : AdminControllerBase
{
    private const int PageSize = 20;

    /// <param name="status">active | inactive | (empty = all)</param>
    public async Task<IActionResult> Index(string? search, int? categoryId, string? status, int page = 1, CancellationToken ct = default)
    {
        bool? isActive = status switch { "active" => true, "inactive" => false, _ => null };
        var result = await products.SearchAsync(new ProductQuery
        {
            Search = search, CategoryId = categoryId, Sort = "newest", Page = page, PageSize = PageSize
        }, includeInactive: true, ct, isActive);

        return View(new ProductListViewModel
        {
            Result = result, Search = search, CategoryId = categoryId, Status = status,
            Categories = await CategoryOptionsAsync(ct)
        });
    }

    public async Task<IActionResult> Create(CancellationToken ct) =>
        View("Create", await WithOptionsAsync(new ProductForm(), ct));

    [HttpPost]
    public async Task<IActionResult> Create(ProductForm form, CancellationToken ct)
    {
        var request = form.ToCreateRequest();
        if (!await ValidateAsync(request, mapKey: k => k.Replace("Variants[0].", "Variant.")))
            return View("Create", await WithOptionsAsync(form, ct));

        var result = await products.CreateAsync(request, ct);
        if (result.IsFailure)
        {
            AddErrors(result.Errors);
            return View("Create", await WithOptionsAsync(form, ct));
        }
        Success();
        return RedirectToAction(nameof(Edit), new { id = result.Value.Id });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var product = await products.GetAsync(id, ct);
        if (product.IsFailure) return NotFound();
        return View(await WithOptionsAsync(ProductForm.From(product.Value), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ProductForm form, CancellationToken ct)
    {
        var existing = await products.GetAsync(id, ct);
        if (existing.IsFailure) return NotFound();
        form.Id = id;
        form.Existing = existing.Value;

        var request = form.ToUpdateRequest();
        if (!await ValidateAsync(request)) return View(await WithOptionsAsync(form, ct));

        var result = await products.UpdateAsync(id, request, ct);
        if (result.IsFailure)
        {
            AddErrors(result.Errors);
            return View(await WithOptionsAsync(form, ct));
        }
        Success();
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await products.DeleteAsync(id, ct);
        if (result.IsFailure) { Failure(result.Errors[0]); return RedirectToAction(nameof(Edit), new { id }); }
        Success("deleted");
        return RedirectToAction(nameof(Index));
    }

    // ---------- Weight options ----------

    public async Task<IActionResult> AddVariant(int id, CancellationToken ct)
    {
        var product = await products.GetAsync(id, ct);
        if (product.IsFailure) return NotFound();
        return View("Variant", new VariantForm { ProductId = id, ProductName = Name(product.Value) });
    }

    [HttpPost]
    public async Task<IActionResult> AddVariant(int id, VariantForm form, CancellationToken ct)
    {
        var product = await products.GetAsync(id, ct);
        if (product.IsFailure) return NotFound();
        form.ProductId = id;
        form.ProductName = Name(product.Value);

        var request = form.ToRequest();
        if (!await ValidateAsync(request)) return View("Variant", form);

        var result = await products.AddVariantAsync(id, request, ct);
        if (result.IsFailure) { AddErrors(result.Errors); return View("Variant", form); }
        Success();
        return RedirectToAction(nameof(Edit), new { id });
    }

    public async Task<IActionResult> EditVariant(int id, int variantId, CancellationToken ct)
    {
        var product = await products.GetAsync(id, ct);
        var variant = product.Value?.Variants.FirstOrDefault(v => v.Id == variantId);
        if (variant is null) return NotFound();
        var form = VariantForm.From(id, variant);
        form.ProductName = Name(product.Value!);
        return View("Variant", form);
    }

    [HttpPost]
    public async Task<IActionResult> EditVariant(int id, int variantId, VariantForm form, CancellationToken ct)
    {
        var product = await products.GetAsync(id, ct);
        var variant = product.Value?.Variants.FirstOrDefault(v => v.Id == variantId);
        if (variant is null) return NotFound();
        form.Id = variantId;
        form.ProductId = id;
        form.ProductName = Name(product.Value!);
        form.StockQuantity = variant.StockQuantity; // stock changes go through Inventory

        var request = form.ToRequest();
        if (!await ValidateAsync(request)) return View("Variant", form);

        var result = await products.UpdateVariantAsync(variantId, request, ct);
        if (result.IsFailure) { AddErrors(result.Errors); return View("Variant", form); }
        Success();
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ---------- Images ----------

    [HttpPost]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> UploadImages(int id, List<IFormFile> images, bool isMain, CancellationToken ct)
    {
        if (images.Count == 0) { Failure(ImageUpload.Invalid); return RedirectToAction(nameof(Edit), new { id }); }

        var added = 0;
        foreach (var file in images)
        {
            await using var stream = file.OpenReadStream();
            var result = await products.AddImageAsync(id, new UploadedImage(stream, file.FileName, file.ContentType, file.Length),
                isMain && added == 0, ct);
            if (result.IsFailure) { Failure(result.Errors[0]); break; }
            added++;
        }
        if (added > 0 && TempData[FlashError] is null) Success();
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> SetMainImage(int id, int imageId, CancellationToken ct)
    {
        var result = await products.SetMainImageAsync(id, imageId, ct);
        if (result.IsFailure) Failure(result.Errors[0]); else Success();
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken ct)
    {
        var result = await products.DeleteImageAsync(id, imageId, ct);
        if (result.IsFailure) Failure(result.Errors[0]); else Success("deleted");
        return RedirectToAction(nameof(Edit), new { id });
    }

    private string Name(AdminProductDto p) => T.IsArabic ? p.NameAr : p.NameEn;

    private async Task<ProductForm> WithOptionsAsync(ProductForm form, CancellationToken ct)
    {
        form.Categories = await CategoryOptionsAsync(ct);
        return form;
    }

    private async Task<IReadOnlyList<SelectListItem>> CategoryOptionsAsync(CancellationToken ct)
    {
        var all = await categories.GetAllForAdminAsync(ct);
        var items = new List<SelectListItem>();
        foreach (var top in all.Where(c => c.ParentCategoryId is null).OrderBy(c => c.SortOrder))
        {
            items.Add(new SelectListItem(T.IsArabic ? top.NameAr : top.NameEn, top.Id.ToString()));
            items.AddRange(all.Where(c => c.ParentCategoryId == top.Id).OrderBy(c => c.SortOrder)
                .Select(c => new SelectListItem("— " + (T.IsArabic ? c.NameAr : c.NameEn), c.Id.ToString())));
        }
        return items;
    }
}
