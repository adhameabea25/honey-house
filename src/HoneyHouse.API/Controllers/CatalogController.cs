using HoneyHouse.API.Common;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers;

/// <summary>Public category tree.</summary>
[Route("api/v1/categories")]
public class CategoriesController(ICategoryService categories) : ApiControllerBase
{
    /// <summary>Active categories as a two-level tree, localized.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(CancellationToken ct) => Ok(await categories.GetTreeAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => FromResult(await categories.GetAsync(id, ct));
}

/// <summary>Public product browsing.</summary>
[Route("api/v1/products")]
public class ProductsController(IProductService products) : ApiControllerBase
{
    /// <summary>Search and filter products.</summary>
    /// <remarks>sort: newest | price_asc | price_desc | rating | bestselling | name</remarks>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] ProductQuery query, CancellationToken ct) =>
        Ok(await products.SearchAsync(query, ct));

    [HttpGet("featured")]
    [ProducesResponseType<IReadOnlyList<ProductListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Featured([FromQuery] int limit = 10, CancellationToken ct = default) =>
        Ok(await products.GetFeaturedAsync(limit, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => FromResult(await products.GetAsync(id, ct));

    [HttpGet("slug/{slug}")]
    [ProducesResponseType<ProductDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct) => FromResult(await products.GetBySlugAsync(slug, ct));
}
