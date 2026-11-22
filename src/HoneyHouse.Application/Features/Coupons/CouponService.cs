using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Features.Coupons;

public sealed record CouponEvaluation(Coupon Coupon, decimal Discount);

public interface ICouponService
{
    /// <summary>Validates a code for a user and subtotal (used by checkout).</summary>
    Task<Result<CouponEvaluation>> EvaluateAsync(string userId, string code, decimal subtotal, CancellationToken ct = default);
    Task<Result<CouponValidationDto>> ValidateForCartAsync(string userId, ValidateCouponRequest request, CancellationToken ct = default);

    Task<PagedResult<AdminCouponDto>> SearchAsync(CouponQuery query, CancellationToken ct = default);
    Task<Result<AdminCouponDto>> GetAsync(int id, CancellationToken ct = default);
    Task<Result<AdminCouponDto>> CreateAsync(CouponUpsertRequest request, CancellationToken ct = default);
    Task<Result<AdminCouponDto>> UpdateAsync(int id, CouponUpsertRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public static class CouponRules
{
    /// <summary>Pure coupon eligibility check.</summary>
    public static Result<decimal> Evaluate(Coupon c, decimal subtotal, DateTime now, int userUsageCount)
    {
        if (!c.IsActive) return Error.Business("coupon_invalid");
        if (c.StartsAt is { } start && now < start) return Error.Business("coupon_not_started");
        if (c.ExpiresAt is { } end && now >= end) return Error.Business("coupon_expired");
        if (c.UsageLimit is { } limit && c.UsedCount >= limit) return Error.Business("coupon_usage_limit");
        if (c.UsageLimitPerUser is { } perUser && userUsageCount >= perUser) return Error.Business("coupon_user_limit");
        if (c.MinOrderAmount is { } min && subtotal < min) return Error.Business("coupon_min_order", min.ToString("0.##"));
        return c.CalculateDiscount(subtotal);
    }

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();
}

public sealed class CouponService(IUnitOfWork uow, ICartService cart, ILanguageContext lang, TimeProvider clock) : ICouponService
{
    private IGenericRepository<Coupon> Coupons => uow.Repository<Coupon>();
    private IGenericRepository<CouponUsage> Usages => uow.Repository<CouponUsage>();

    public async Task<Result<CouponEvaluation>> EvaluateAsync(string userId, string code, decimal subtotal, CancellationToken ct = default)
    {
        var normalized = CouponRules.Normalize(code);
        var coupon = await Coupons.FirstOrDefaultAsync(c => c.Code == normalized, ct);
        if (coupon is null) return Error.Business("coupon_invalid");

        var userUsage = coupon.UsageLimitPerUser is null ? 0 : await Usages.CountAsync(u => u.CouponId == coupon.Id && u.UserId == userId, ct);
        var result = CouponRules.Evaluate(coupon, subtotal, clock.GetUtcNow().UtcDateTime, userUsage);
        return result.IsSuccess ? new CouponEvaluation(coupon, result.Value) : Result<CouponEvaluation>.Failure(result.Errors);
    }

    public async Task<Result<CouponValidationDto>> ValidateForCartAsync(string userId, ValidateCouponRequest request, CancellationToken ct = default)
    {
        var current = await cart.GetAsync(userId, ct);
        if (current.Subtotal <= 0) return Error.Business("cart_empty");

        var eval = await EvaluateAsync(userId, request.Code, current.Subtotal, ct);
        if (eval.IsFailure) return Result<CouponValidationDto>.Failure(eval.Errors);

        var c = eval.Value.Coupon;
        var description = lang.Pick(c.DescriptionAr ?? "", c.DescriptionEn ?? "");
        return new CouponValidationDto(c.Code, description.Length > 0 ? description : null, current.Subtotal, eval.Value.Discount,
            current.Subtotal - eval.Value.Discount, "EGP");
    }

    public async Task<PagedResult<AdminCouponDto>> SearchAsync(CouponQuery query, CancellationToken ct = default)
    {
        var paging = new PagedRequest { Page = query.Page, PageSize = query.PageSize };
        var search = query.Search?.Trim().ToUpperInvariant();
        var all = await Coupons.ListAsync(c => (search == null || c.Code.Contains(search)) && (query.IsActive == null || c.IsActive == query.IsActive), ct);
        var page = all.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id).Skip(paging.Skip).Take(paging.PageSize).Select(ToDto).ToList();
        return new PagedResult<AdminCouponDto>(page, paging.Page, paging.PageSize, all.Count);
    }

    public async Task<Result<AdminCouponDto>> GetAsync(int id, CancellationToken ct = default) =>
        await Coupons.GetByIdAsync(id, ct) is { } c ? ToDto(c) : Error.NotFound("coupon_not_found");

    public async Task<Result<AdminCouponDto>> CreateAsync(CouponUpsertRequest r, CancellationToken ct = default)
    {
        var code = CouponRules.Normalize(r.Code);
        if (await Coupons.AnyAsync(c => c.Code == code, ct)) return Error.Conflict("coupon_code_taken");
        var coupon = new Coupon();
        Apply(coupon, r, code);
        await Coupons.AddAsync(coupon, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(coupon);
    }

    public async Task<Result<AdminCouponDto>> UpdateAsync(int id, CouponUpsertRequest r, CancellationToken ct = default)
    {
        var coupon = await Coupons.GetByIdAsync(id, ct);
        if (coupon is null) return Error.NotFound("coupon_not_found");
        var code = CouponRules.Normalize(r.Code);
        if (code != coupon.Code && await Coupons.AnyAsync(c => c.Code == code && c.Id != id, ct)) return Error.Conflict("coupon_code_taken");
        Apply(coupon, r, code);
        await uow.SaveChangesAsync(ct);
        return ToDto(coupon);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var coupon = await Coupons.GetByIdAsync(id, ct);
        if (coupon is null) return Error.NotFound("coupon_not_found");
        if (coupon.UsedCount > 0 || await Usages.AnyAsync(u => u.CouponId == id, ct)) return Error.Conflict("coupon_in_use");
        Coupons.Remove(coupon);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static void Apply(Coupon c, CouponUpsertRequest r, string code)
    {
        c.Code = code;
        c.DescriptionAr = r.DescriptionAr;
        c.DescriptionEn = r.DescriptionEn;
        c.DiscountType = r.DiscountType;
        c.Value = r.Value;
        c.MinOrderAmount = r.MinOrderAmount;
        c.MaxDiscountAmount = r.MaxDiscountAmount;
        c.StartsAt = r.StartsAt?.ToUniversalTime();
        c.ExpiresAt = r.ExpiresAt?.ToUniversalTime();
        c.UsageLimit = r.UsageLimit;
        c.UsageLimitPerUser = r.UsageLimitPerUser;
        c.IsActive = r.IsActive;
    }

    private static AdminCouponDto ToDto(Coupon c) => new(c.Id, c.Code, c.DescriptionAr, c.DescriptionEn, c.DiscountType, c.Value,
        c.MinOrderAmount, c.MaxDiscountAmount, c.StartsAt, c.ExpiresAt, c.UsageLimit, c.UsageLimitPerUser, c.UsedCount, c.IsActive, c.CreatedAt);
}
