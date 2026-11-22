using FluentValidation;
using HoneyHouse.Application.Common;

namespace HoneyHouse.Application.Features.Reviews;

public sealed record ReviewDto(int Id, int Rating, string? Comment, string ReviewerName, bool IsVerifiedPurchase, DateTime CreatedAt);
public sealed record ReviewSummaryDto(decimal Average, int Count, IReadOnlyDictionary<int, int> Distribution);
public sealed record ProductReviewsDto(ReviewSummaryDto Summary, PagedResult<ReviewDto> Reviews);
public sealed record MyReviewDto(int Id, int ProductId, int Rating, string? Comment, bool IsApproved, bool IsVerifiedPurchase, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record ReviewRequest(int Rating, string? Comment);
public sealed record ModerateReviewRequest(bool Approved);
public sealed record AdminReviewQuery(string? Status = "pending", int? ProductId = null, int Page = 1, int PageSize = 20);

public sealed record AdminReviewDto(int Id, int ProductId, string ProductNameAr, string ProductNameEn, string UserId, string UserName,
    string UserEmail, int Rating, string? Comment, bool IsApproved, bool IsVerifiedPurchase, DateTime CreatedAt);

public sealed class ReviewRequestValidator : AbstractValidator<ReviewRequest>
{
    public ReviewRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}
