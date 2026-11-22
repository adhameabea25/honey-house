using HoneyHouse.API.Common;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Application.Features.Reviews;
using HoneyHouse.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers;

/// <summary>Product reviews.</summary>
[Route("api/v1")]
public class ReviewsController(IReviewService reviews) : ApiControllerBase
{
    /// <summary>Approved reviews of a product with the rating summary.</summary>
    [HttpGet("products/{productId:int}/reviews")]
    [ProducesResponseType<ProductReviewsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForProduct(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        FromResult(await reviews.GetForProductAsync(productId, page, pageSize, ct));

    /// <summary>The signed-in customer's review of this product (also when pending approval).</summary>
    [Authorize]
    [HttpGet("products/{productId:int}/reviews/mine")]
    [ProducesResponseType<MyReviewDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(int productId, CancellationToken ct) =>
        FromResult(await reviews.GetMineAsync(User.GetUserId(), productId, ct));

    /// <summary>Review a product (one review per customer; shown after moderation).</summary>
    [Authorize]
    [HttpPost("products/{productId:int}/reviews")]
    [ProducesResponseType<MyReviewDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(int productId, ReviewRequest request, CancellationToken ct) =>
        CreatedFromResult(await reviews.CreateAsync(User.GetUserId(), productId, request, ct), nameof(GetMine), r => new { productId = r.ProductId });

    [Authorize]
    [HttpPut("reviews/{id:int}")]
    [ProducesResponseType<MyReviewDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, ReviewRequest request, CancellationToken ct) =>
        FromResult(await reviews.UpdateAsync(User.GetUserId(), id, request, ct));

    [Authorize]
    [HttpDelete("reviews/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) => FromResult(await reviews.DeleteAsync(User.GetUserId(), id, ct));
}

/// <summary>Review moderation (Admin, Manager).</summary>
[Authorize(Roles = Roles.AdminOrManager)]
[Route("api/v1/admin/reviews")]
public class AdminReviewsController(IReviewService reviews) : ApiControllerBase
{
    /// <summary>status: pending (default) | approved | all</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminReviewDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] AdminReviewQuery query, CancellationToken ct) => Ok(await reviews.SearchAsync(query, ct));

    [HttpPut("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Moderate(int id, ModerateReviewRequest request, CancellationToken ct) =>
        FromResult(await reviews.ModerateAsync(id, request.Approved, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) => FromResult(await reviews.AdminDeleteAsync(id, ct));
}

/// <summary>The signed-in customer's favorite products.</summary>
[Authorize]
[Route("api/v1/favorites")]
public class FavoritesController(IFavoriteService favorites) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await favorites.GetAsync(User.GetUserId(), ct));

    /// <summary>Only the ids, for showing heart icons in lists.</summary>
    [HttpGet("ids")]
    [ProducesResponseType<IReadOnlyList<int>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIds(CancellationToken ct) => Ok(await favorites.GetIdsAsync(User.GetUserId(), ct));

    [HttpPost("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Add(int productId, CancellationToken ct) => FromResult(await favorites.AddAsync(User.GetUserId(), productId, ct));

    [HttpDelete("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(int productId, CancellationToken ct)
    {
        await favorites.RemoveAsync(User.GetUserId(), productId, ct);
        return NoContent();
    }
}
