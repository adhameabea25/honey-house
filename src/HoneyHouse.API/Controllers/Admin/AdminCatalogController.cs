using HoneyHouse.API.Common;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers.Admin;

/// <summary>Category management (Admin, Manager).</summary>
[Authorize(Roles = Roles.AdminOrManager)]
[Route("api/v1/admin/categories")]
public class AdminCategoriesController(ICategoryService categories) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminCategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await categories.GetAllForAdminAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminCategoryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => FromResult(await categories.GetForAdminAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<AdminCategoryDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CategoryUpsertRequest request, CancellationToken ct) =>
        CreatedFromResult(await categories.CreateAsync(request, ct), nameof(Get), c => new { id = c.Id });

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminCategoryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, CategoryUpsertRequest request, CancellationToken ct) =>
        FromResult(await categories.UpdateAsync(id, request, ct));

    /// <summary>Soft-deletes an empty category.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) => FromResult(await categories.DeleteAsync(id, ct));
}

/// <summary>Product, weight-option and image management (Admin, Manager).</summary>
[Authorize(Roles = Roles.AdminOrManager)]
[Route("api/v1/admin")]
public class AdminProductsController(IProductAdminService products) : ApiControllerBase
{
    [HttpGet("products")]
    [ProducesResponseType<PagedResult<AdminProductListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] ProductQuery query, [FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        Ok(await products.SearchAsync(query, includeInactive, ct));

    [HttpGet("products/{id:int}")]
    [ProducesResponseType<AdminProductDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => FromResult(await products.GetAsync(id, ct));

    /// <summary>Create a product with its weight options and initial stock.</summary>
    [HttpPost("products")]
    [ProducesResponseType<AdminProductDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken ct) =>
        CreatedFromResult(await products.CreateAsync(request, ct), nameof(Get), p => new { id = p.Id });

    [HttpPut("products/{id:int}")]
    [ProducesResponseType<AdminProductDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request, CancellationToken ct) =>
        FromResult(await products.UpdateAsync(id, request, ct));

    [HttpDelete("products/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) => FromResult(await products.DeleteAsync(id, ct));

    [HttpPost("products/{id:int}/variants")]
    [ProducesResponseType<AdminVariantDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddVariant(int id, VariantUpsertRequest request, CancellationToken ct) =>
        FromResult(await products.AddVariantAsync(id, request, ct));

    /// <summary>Update price, label, SKU and threshold. Stock changes go through inventory adjustments.</summary>
    [HttpPut("variants/{variantId:int}")]
    [ProducesResponseType<AdminVariantDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateVariant(int variantId, VariantUpsertRequest request, CancellationToken ct) =>
        FromResult(await products.UpdateVariantAsync(variantId, request, ct));

    /// <summary>Deactivates a weight option (kept for order history).</summary>
    [HttpDelete("variants/{variantId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateVariant(int variantId, CancellationToken ct) =>
        FromResult(await products.DeactivateVariantAsync(variantId, ct));

    /// <summary>Upload a product image (JPG, PNG or WEBP, max 5 MB).</summary>
    [HttpPost("products/{id:int}/images")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType<ProductImageDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, [FromForm] bool isMain = false, CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream();
        return FromResult(await products.AddImageAsync(id, new UploadedImage(stream, file.FileName, file.ContentType, file.Length), isMain, ct));
    }

    [HttpPut("products/{id:int}/images/{imageId:int}/main")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetMainImage(int id, int imageId, CancellationToken ct) =>
        FromResult(await products.SetMainImageAsync(id, imageId, ct));

    [HttpDelete("products/{id:int}/images/{imageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken ct) =>
        FromResult(await products.DeleteImageAsync(id, imageId, ct));
}
