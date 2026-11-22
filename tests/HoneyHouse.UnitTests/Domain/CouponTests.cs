using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;
using Shouldly;

namespace HoneyHouse.UnitTests.Domain;

public class CouponTests
{
    [Theory]
    [InlineData(10, 500, 50)]
    [InlineData(15, 333.33, 50)]
    [InlineData(100, 200, 200)]
    public void Percentage_discount_is_rounded_to_two_decimals(decimal percent, decimal subtotal, decimal expected)
    {
        var coupon = new Coupon { DiscountType = DiscountType.Percentage, Value = percent };
        coupon.CalculateDiscount(subtotal).ShouldBe(expected);
    }

    [Fact]
    public void Percentage_discount_is_capped_by_max_discount()
    {
        var coupon = new Coupon { DiscountType = DiscountType.Percentage, Value = 50, MaxDiscountAmount = 100 };
        coupon.CalculateDiscount(1000).ShouldBe(100);
    }

    [Fact]
    public void Fixed_discount_never_exceeds_subtotal()
    {
        var coupon = new Coupon { DiscountType = DiscountType.FixedAmount, Value = 300 };
        coupon.CalculateDiscount(120).ShouldBe(120);
    }

    [Fact]
    public void Zero_subtotal_gives_zero_discount()
    {
        new Coupon { DiscountType = DiscountType.FixedAmount, Value = 50 }.CalculateDiscount(0).ShouldBe(0);
    }
}
