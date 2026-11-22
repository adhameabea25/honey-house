using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Features.Reviews;

public interface IReviewService
{
    Task<Result<ProductReviewsDto>> GetForProductAsync(int productId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<MyReviewDto>> GetMineAsync(string userId, int productId, CancellationToken ct = default);
    Task<Result<MyReviewDto>> CreateAsync(string userId, int productId, ReviewRequest request, CancellationToken ct = default);
    Task<Result<MyReviewDto>> UpdateAsync(string userId, int reviewId, ReviewRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string userId, int reviewId, CancellationToken ct = default);

    Task<PagedResult<AdminReviewDto>> SearchAsync(AdminReviewQuery query, CancellationToken ct = default);
    Task<Result> ModerateAsync(int reviewId, bool approved, CancellationToken ct = default);
    Task<Result> AdminDeleteAsync(int reviewId, CancellationToken ct = default);
}

public sealed class ReviewService(IReviewRepository reviews, IProductRepository products, IOrderRepository orders, IUnitOfWork uow) : IReviewService
{
    public async Task<Result<ProductReviewsDto>> GetForProductAsync(int productId, int page, int pageSize, CancellationToken ct = default)
    {
        if (await products.GetDetailsAsync(productId, activeOnly: true, ct) is null) return Error.NotFound("product_not_found");
        var paging = new PagedRequest { Page = page, PageSize = pageSize };
        var summary = await reviews.GetSummaryAsync(productId, ct);
        var (items, total) = await reviews.SearchAsync(new ReviewSearchCriteria(productId, true, paging.Skip, paging.PageSize), ct);
        return new ProductReviewsDto(
            new ReviewSummaryDto(summary.Average, summary.Count, summary.Distribution),
            new PagedResult<ReviewDto>(items.Select(r => new ReviewDto(r.Id, r.Rating, r.Comment, PublicName(r.User), r.IsVerifiedPurchase, r.CreatedAt)).ToList(),
                paging.Page, paging.PageSize, total));
    }

    public async Task<Result<MyReviewDto>> GetMineAsync(string userId, int productId, CancellationToken ct = default) =>
        await reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId, ct) is { } r ? ToMine(r) : Error.NotFound("review_not_found");

    public async Task<Result<MyReviewDto>> CreateAsync(string userId, int productId, ReviewRequest request, CancellationToken ct = default)
    {
        if (await products.GetDetailsAsync(productId, activeOnly: true, ct) is null) return Error.NotFound("product_not_found");
        if (await reviews.AnyAsync(r => r.UserId == userId && r.ProductId == productId, ct)) return Error.Conflict("review_exists");

        var review = new Review
        {
            ProductId = productId, UserId = userId, Rating = request.Rating, Comment = request.Comment?.Trim(),
            IsApproved = false, // moderated before it is public
            IsVerifiedPurchase = await orders.HasDeliveredPurchaseAsync(userId, productId, ct)
        };
        await reviews.AddAsync(review, ct);
        await uow.SaveChangesAsync(ct);
        return ToMine(review);
    }

    public async Task<Result<MyReviewDto>> UpdateAsync(string userId, int reviewId, ReviewRequest request, CancellationToken ct = default)
    {
        var review = await reviews.GetByIdAsync(reviewId, ct);
        if (review is null || review.UserId != userId) return Error.NotFound("review_not_found");
        var wasApproved = review.IsApproved;
        review.Rating = request.Rating;
        review.Comment = request.Comment?.Trim();
        review.IsApproved = false; // edited text needs moderation again
        review.IsVerifiedPurchase = review.IsVerifiedPurchase || await orders.HasDeliveredPurchaseAsync(userId, review.ProductId, ct);
        await uow.SaveChangesAsync(ct);
        if (wasApproved) await reviews.RecalculateProductRatingAsync(review.ProductId, ct);
        return ToMine(review);
    }

    public async Task<Result> DeleteAsync(string userId, int reviewId, CancellationToken ct = default)
    {
        var review = await reviews.GetByIdAsync(reviewId, ct);
        if (review is null || review.UserId != userId) return Error.NotFound("review_not_found");
        return await RemoveAsync(review, ct);
    }

    public async Task<PagedResult<AdminReviewDto>> SearchAsync(AdminReviewQuery q, CancellationToken ct = default)
    {
        var paging = new PagedRequest { Page = q.Page, PageSize = q.PageSize };
        bool? approved = q.Status?.ToLowerInvariant() switch { "pending" => false, "approved" => true, _ => null };
        var (items, total) = await reviews.SearchAsync(new ReviewSearchCriteria(q.ProductId, approved, paging.Skip, paging.PageSize), ct);
        return new PagedResult<AdminReviewDto>(items.Select(r => new AdminReviewDto(r.Id, r.ProductId, r.Product.NameAr, r.Product.NameEn,
            r.UserId, r.User.FullName, r.User.Email ?? "", r.Rating, r.Comment, r.IsApproved, r.IsVerifiedPurchase, r.CreatedAt)).ToList(),
            paging.Page, paging.PageSize, total);
    }

    public async Task<Result> ModerateAsync(int reviewId, bool approved, CancellationToken ct = default)
    {
        var review = await reviews.GetByIdAsync(reviewId, ct);
        if (review is null) return Error.NotFound("review_not_found");
        review.IsApproved = approved;
        await uow.SaveChangesAsync(ct);
        await reviews.RecalculateProductRatingAsync(review.ProductId, ct);
        return Result.Success();
    }

    public async Task<Result> AdminDeleteAsync(int reviewId, CancellationToken ct = default)
    {
        var review = await reviews.GetByIdAsync(reviewId, ct);
        return review is null ? Error.NotFound("review_not_found") : await RemoveAsync(review, ct);
    }

    private async Task<Result> RemoveAsync(Review review, CancellationToken ct)
    {
        reviews.Remove(review);
        await uow.SaveChangesAsync(ct);
        if (review.IsApproved) await reviews.RecalculateProductRatingAsync(review.ProductId, ct);
        return Result.Success();
    }

    /// <summary>Only the first name is shown publicly.</summary>
    public static string PublicName(ApplicationUser? user) =>
        user?.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "—";

    private static MyReviewDto ToMine(Review r) => new(r.Id, r.ProductId, r.Rating, r.Comment, r.IsApproved, r.IsVerifiedPurchase, r.CreatedAt, r.UpdatedAt);
}

public interface IFavoriteService
{
    Task<IReadOnlyList<ProductListItemDto>> GetAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetIdsAsync(string userId, CancellationToken ct = default);
    Task<Result> AddAsync(string userId, int productId, CancellationToken ct = default);
    Task RemoveAsync(string userId, int productId, CancellationToken ct = default);
}

public sealed class FavoriteService(IFavoriteRepository favorites, IProductRepository products, IProductService catalog, IUnitOfWork uow) : IFavoriteService
{
    public async Task<IReadOnlyList<ProductListItemDto>> GetAsync(string userId, CancellationToken ct = default)
    {
        var ids = await favorites.GetProductIdsAsync(userId, ct);
        if (ids.Count == 0) return [];
        var items = await catalog.GetByIdsAsync(ids, ct);
        return ids.Select(id => items.FirstOrDefault(p => p.Id == id)).OfType<ProductListItemDto>().ToList();
    }

    public Task<IReadOnlyList<int>> GetIdsAsync(string userId, CancellationToken ct = default) => favorites.GetProductIdsAsync(userId, ct);

    public async Task<Result> AddAsync(string userId, int productId, CancellationToken ct = default)
    {
        if (await products.GetDetailsAsync(productId, activeOnly: true, ct) is null) return Error.NotFound("product_not_found");
        if (await favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId, ct)) return Result.Success();
        await favorites.AddAsync(new Favorite { UserId = userId, ProductId = productId }, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task RemoveAsync(string userId, int productId, CancellationToken ct = default)
    {
        var fav = await favorites.FindAsync([userId, productId], ct);
        if (fav is null) return;
        favorites.Remove(fav);
        await uow.SaveChangesAsync(ct);
    }
}
