using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Models;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HoneyHouse.Admin.Controllers;

public sealed class CategoriesController(ICategoryService categories, IFileStorage files) : AdminControllerBase
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var all = await categories.GetAllForAdminAsync(ct);
        var rows = new List<CategoryRow>();
        foreach (var top in all.Where(c => c.ParentCategoryId is null).OrderBy(c => c.SortOrder).ThenBy(c => c.Id))
        {
            rows.Add(new CategoryRow { Category = top });
            rows.AddRange(all.Where(c => c.ParentCategoryId == top.Id).OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
                .Select(c => new CategoryRow { Category = c, Depth = 1, ParentName = T.IsArabic ? top.NameAr : top.NameEn }));
        }
        return View(rows);
    }

    public async Task<IActionResult> Create(CancellationToken ct) =>
        View("Form", await WithParentsAsync(new CategoryForm(), ct));

    [HttpPost]
    public async Task<IActionResult> Create(CategoryForm form, IFormFile? image, CancellationToken ct)
    {
        if (!CheckImage(image) | !await ValidateAsync(form.ToRequest()))
            return View("Form", await WithParentsAsync(form, ct));

        var uploaded = await SaveImageAsync(form, image, ct);
        var result = await categories.CreateAsync(form.ToRequest(), ct);
        if (result.IsFailure)
        {
            if (uploaded is not null) await files.DeleteAsync(uploaded, ct);
            form.ImageUrl = null;
            AddErrors(result.Errors);
            return View("Form", await WithParentsAsync(form, ct));
        }
        Success();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var category = await categories.GetForAdminAsync(id, ct);
        if (category.IsFailure) return NotFound();
        return View("Form", await WithParentsAsync(CategoryForm.From(category.Value), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryForm form, IFormFile? image, CancellationToken ct)
    {
        form.Id = id;
        var existing = await categories.GetForAdminAsync(id, ct);
        if (existing.IsFailure) return NotFound();
        var oldImage = existing.Value.ImageUrl;
        form.ImageUrl = form.RemoveImage ? null : oldImage;

        if (!CheckImage(image) | !await ValidateAsync(form.ToRequest()))
            return View("Form", await WithParentsAsync(form, ct));

        var uploaded = await SaveImageAsync(form, image, ct);
        var result = await categories.UpdateAsync(id, form.ToRequest(), ct);
        if (result.IsFailure)
        {
            if (uploaded is not null) await files.DeleteAsync(uploaded, ct);
            form.ImageUrl = oldImage;
            AddErrors(result.Errors);
            return View("Form", await WithParentsAsync(form, ct));
        }
        if (oldImage is not null && oldImage != result.Value.ImageUrl) await files.DeleteAsync(oldImage, ct);
        Success();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var existing = await categories.GetForAdminAsync(id, ct);
        var result = await categories.DeleteAsync(id, ct);
        if (result.IsFailure) Failure(result.Errors[0]);
        else
        {
            if (existing.Value?.ImageUrl is { } url) await files.DeleteAsync(url, ct);
            Success("deleted");
        }
        return RedirectToAction(nameof(Index));
    }

    private bool CheckImage(IFormFile? image)
    {
        if (image is null || image.Length == 0 || ImageUpload.IsValid(image)) return true;
        ModelState.AddModelError("image", Describe(ImageUpload.Invalid));
        return false;
    }

    /// <returns>The new image URL, or null when no file was sent.</returns>
    private async Task<string?> SaveImageAsync(CategoryForm form, IFormFile? image, CancellationToken ct)
    {
        if (image is null || image.Length == 0) return null;
        await using var stream = image.OpenReadStream();
        return form.ImageUrl = await files.SaveAsync(stream, image.FileName, "categories", ct);
    }

    private async Task<CategoryForm> WithParentsAsync(CategoryForm form, CancellationToken ct)
    {
        var all = await categories.GetAllForAdminAsync(ct);
        form.Parents = all.Where(c => c.ParentCategoryId is null && c.Id != form.Id)
            .OrderBy(c => c.SortOrder)
            .Select(c => new SelectListItem(T.IsArabic ? c.NameAr : c.NameEn, c.Id.ToString()))
            .ToList();
        return form;
    }
}
