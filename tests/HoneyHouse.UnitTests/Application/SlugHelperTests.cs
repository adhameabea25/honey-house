using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Application.Interfaces.Repositories;
using Shouldly;

namespace HoneyHouse.UnitTests.Application;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Sidr Honey", "sidr-honey")]
    [InlineData("  Black   Seed -- Honey! ", "black-seed-honey")]
    [InlineData("Honey 500g", "honey-500g")]
    public void Generates_url_safe_slugs(string input, string expected) => SlugHelper.Generate(input).ShouldBe(expected);

    [Fact]
    public void Arabic_only_text_gets_random_slug()
    {
        var slug = SlugHelper.Generate("عسل");
        slug.Length.ShouldBe(8);
        SlugHelper.IsValid(slug).ShouldBeTrue();
    }

    [Theory]
    [InlineData("sidr-honey", true)]
    [InlineData("Sidr", false)]
    [InlineData("-x", false)]
    [InlineData("a--b", false)]
    public void Validates_slugs(string slug, bool valid) => SlugHelper.IsValid(slug).ShouldBe(valid);

    [Theory]
    [InlineData("price_asc", ProductSort.PriceAsc)]
    [InlineData("PRICE_DESC", ProductSort.PriceDesc)]
    [InlineData("bestselling", ProductSort.BestSelling)]
    [InlineData(null, ProductSort.Newest)]
    [InlineData("garbage", ProductSort.Newest)]
    public void Parses_sort(string? input, ProductSort expected) => ProductService.ParseSort(input).ShouldBe(expected);
}
