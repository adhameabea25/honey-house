using HoneyHouse.Application.Features.Coupons;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;
using Shouldly;

namespace HoneyHouse.UnitTests.Application;

public class CouponRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Coupon C(Action<Coupon>? configure = null)
    {
        var c = new Coupon { Code = "X", DiscountType = DiscountType.FixedAmount, Value = 50, IsActive = true };
        configure?.Invoke(c);
        return c;
    }

    [Fact]
    public void Eligible_coupon_returns_discount() => CouponRules.Evaluate(C(), 300, Now, 0).Value.ShouldBe(50);

    [Fact]
    public void Inactive() => CouponRules.Evaluate(C(c => c.IsActive = false), 300, Now, 0).FirstError!.Code.ShouldBe("coupon_invalid");

    [Fact]
    public void Not_started() => CouponRules.Evaluate(C(c => c.StartsAt = Now.AddSeconds(1)), 300, Now, 0).FirstError!.Code.ShouldBe("coupon_not_started");

    [Fact]
    public void Expired_at_exact_end() => CouponRules.Evaluate(C(c => c.ExpiresAt = Now), 300, Now, 0).FirstError!.Code.ShouldBe("coupon_expired");

    [Fact]
    public void Global_limit() => CouponRules.Evaluate(C(c => { c.UsageLimit = 5; c.UsedCount = 5; }), 300, Now, 0).FirstError!.Code.ShouldBe("coupon_usage_limit");

    [Fact]
    public void Per_user_limit() => CouponRules.Evaluate(C(c => c.UsageLimitPerUser = 1), 300, Now, 1).FirstError!.Code.ShouldBe("coupon_user_limit");

    [Fact]
    public void Minimum_order()
    {
        var result = CouponRules.Evaluate(C(c => c.MinOrderAmount = 500), 499.99m, Now, 0);
        result.FirstError!.Code.ShouldBe("coupon_min_order");
        result.FirstError.Args![0].ShouldBe("500");
        CouponRules.Evaluate(C(c => c.MinOrderAmount = 500), 500, Now, 0).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Codes_are_normalized() => CouponRules.Normalize("  honey10 ").ShouldBe("HONEY10");
}
