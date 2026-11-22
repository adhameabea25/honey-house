using HoneyHouse.Application.Common;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Features.Catalog;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetTreeAsync(CancellationToken ct = default);
    Task<Result<CategoryDto>> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<AdminCategoryDto>> GetAllForAdminAsync(CancellationToken ct = default);
    Task<Result<AdminCategoryDto>> GetForAdminAsync(int id, CancellationToken ct = default);
    Task<Result<AdminCategoryDto>> CreateAsync(CategoryUpsertRequest request, CancellationToken ct = default);
    Task<Result<AdminCategoryDto>> UpdateAsync(int id, CategoryUpsertRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class CategoryService(ICategoryRepository categories, IUnitOfWork uow, ILanguageContext lang) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var all = await categories.GetAllAsync(activeOnly: true, ct);
        return BuildTree(all, null);
    }

    public async Task<Result<CategoryDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var all = await categories.GetAllAsync(activeOnly: true, ct);
        var category = all.FirstOrDefault(c => c.Id == id);
        return category is null ? Error.NotFound("category_not_found") : ToDto(category, all);
    }

    public async Task<IReadOnlyList<AdminCategoryDto>> GetAllForAdminAsync(CancellationToken ct = default) =>
        (await categories.GetAllAsync(activeOnly: false, ct)).Select(ToAdminDto).ToList();

    public async Task<Result<AdminCategoryDto>> GetForAdminAsync(int id, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(id, ct);
        return category is null ? Error.NotFound("category_not_found") : ToAdminDto(category);
    }

    public async Task<Result<AdminCategoryDto>> CreateAsync(CategoryUpsertRequest request, CancellationToken ct = default)
    {
        var category = new Category();
        var applied = await ApplyAsync(category, request, ct);
        if (applied.IsFailure) return Result<AdminCategoryDto>.Failure(applied.Errors);

        await categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);
        return ToAdminDto(category);
    }

    public async Task<Result<AdminCategoryDto>> UpdateAsync(int id, CategoryUpsertRequest request, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(id, ct);
        if (category is null) return Error.NotFound("category_not_found");

        var applied = await ApplyAsync(category, request, ct);
        if (applied.IsFailure) return Result<AdminCategoryDto>.Failure(applied.Errors);

        await uow.SaveChangesAsync(ct);
        return ToAdminDto(category);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(id, ct);
        if (category is null) return Error.NotFound("category_not_found");
        if (await categories.HasChildrenOrProductsAsync(id, ct)) return Error.Conflict("category_not_empty");

        categories.Remove(category);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result> ApplyAsync(Category category, CategoryUpsertRequest r, CancellationToken ct)
    {
        if (r.ParentCategoryId is { } parentId)
        {
            if (parentId == category.Id) return Error.Validation("parent_category_invalid", "parentCategoryId");
            var parent = await categories.GetByIdAsync(parentId, ct);
            // Only two levels: the parent must itself be top-level, and a category with children cannot become a child.
            if (parent is null || parent.ParentCategoryId is not null)
                return Error.Validation("parent_category_invalid", "parentCategoryId");
            if (category.Id != 0 && await categories.AnyAsync(c => c.ParentCategoryId == category.Id, ct))
                return Error.Validation("parent_category_invalid", "parentCategoryId");
        }

        var slug = string.IsNullOrWhiteSpace(r.Slug) ? SlugHelper.Generate(r.NameEn) : r.Slug.Trim();
        if (await categories.SlugExistsAsync(slug, category.Id == 0 ? null : category.Id, ct))
            return Error.Conflict("slug_taken");

        category.NameAr = r.NameAr.Trim();
        category.NameEn = r.NameEn.Trim();
        category.DescriptionAr = r.DescriptionAr;
        category.DescriptionEn = r.DescriptionEn;
        category.Slug = slug;
        category.ImageUrl = r.ImageUrl;
        category.ParentCategoryId = r.ParentCategoryId;
        category.SortOrder = r.SortOrder;
        category.IsActive = r.IsActive;
        return Result.Success();
    }

    private IReadOnlyList<CategoryDto> BuildTree(IReadOnlyList<Category> all, int? parentId) =>
        all.Where(c => c.ParentCategoryId == parentId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => ToDto(c, all))
            .ToList();

    private CategoryDto ToDto(Category c, IReadOnlyList<Category> all) =>
        new(c.Id, lang.Pick(c.NameAr, c.NameEn), c.Slug, lang.Pick(c.DescriptionAr ?? "", c.DescriptionEn ?? "") is { Length: > 0 } d ? d : null,
            c.ImageUrl, c.ParentCategoryId, BuildTree(all, c.Id));

    private static AdminCategoryDto ToAdminDto(Category c) =>
        new(c.Id, c.NameAr, c.NameEn, c.DescriptionAr, c.DescriptionEn, c.Slug, c.ImageUrl, c.ParentCategoryId, c.SortOrder, c.IsActive);
}
