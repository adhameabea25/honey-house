using System.Net;
using System.Net.Http.Json;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Application.Features.Reviews;
using HoneyHouse.Domain.Enums;
using HoneyHouse.IntegrationTests.Infrastructure;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ReviewAndFavoriteTests(HoneyHouseApiFactory factory)
{
    private static async Task<MyReviewDto> ReviewAsync(HttpClient c, int productId, int rating, string? comment = "عسل ممتاز")
    {
        var res = await c.PostAsJsonAsync($"/api/v1/products/{productId}/reviews", new ReviewRequest(rating, comment));
        res.StatusCode.ShouldBe(HttpStatusCode.Created, await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<MyReviewDto>(HttpExtensions.Json))!;
    }

    private static Task<HttpResponseMessage> Approve(HttpClient admin, int reviewId, bool approved = true) =>
        admin.PutAsJsonAsync($"/api/v1/admin/reviews/{reviewId}/approve", new ModerateReviewRequest(approved));

    [Fact]
    public async Task Reviews_are_moderated_and_update_product_rating()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync();
        var (c1, _) = await factory.CreateCustomerClientAsync();
        var (c2, _) = await factory.CreateCustomerClientAsync();
        var r1 = await ReviewAsync(c1, product.Id, 5);
        var r2 = await ReviewAsync(c2, product.Id, 2, null);
        r1.IsApproved.ShouldBeFalse();

        var publicBefore = await factory.CreateClient().GetFromJsonAsync<ProductReviewsDto>($"/api/v1/products/{product.Id}/reviews", HttpExtensions.Json);
        publicBefore!.Summary.Count.ShouldBe(0);

        var pending = await admin.GetFromJsonAsync<PagedResult<AdminReviewDto>>($"/api/v1/admin/reviews?productId={product.Id}", HttpExtensions.Json);
        pending!.Items.Count.ShouldBe(2);

        (await Approve(admin, r1.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Approve(admin, r2.Id)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var reviews = await factory.CreateClient().GetFromJsonAsync<ProductReviewsDto>($"/api/v1/products/{product.Id}/reviews", HttpExtensions.Json);
        reviews!.Summary.Count.ShouldBe(2);
        reviews.Summary.Average.ShouldBe(3.5m);
        reviews.Summary.Distribution[5].ShouldBe(1);
        reviews.Summary.Distribution[2].ShouldBe(1);
        reviews.Reviews.Items.First().ReviewerName.ShouldBe("Test"); // first name only

        var detail = await factory.CreateClient().GetFromJsonAsync<ProductDetailDto>($"/api/v1/products/{product.Id}", HttpExtensions.Json);
        detail!.AverageRating.ShouldBe(3.5m);
        detail.ReviewCount.ShouldBe(2);

        // Editing a review sends it back to moderation and removes it from the rating
        (await c2.PutAsJsonAsync($"/api/v1/reviews/{r2.Id}", new ReviewRequest(4, "أفضل مما توقعت"))).StatusCode.ShouldBe(HttpStatusCode.OK);
        detail = await factory.CreateClient().GetFromJsonAsync<ProductDetailDto>($"/api/v1/products/{product.Id}", HttpExtensions.Json);
        detail!.ReviewCount.ShouldBe(1);
        detail.AverageRating.ShouldBe(5);

        (await c1.DeleteAsync($"/api/v1/reviews/{r1.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        detail = await factory.CreateClient().GetFromJsonAsync<ProductDetailDto>($"/api/v1/products/{product.Id}", HttpExtensions.Json);
        detail!.ReviewCount.ShouldBe(0);
        detail.AverageRating.ShouldBe(0);
    }

    [Fact]
    public async Task One_review_per_customer_and_owner_only_edits()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync();
        var (c1, _) = await factory.CreateCustomerClientAsync();
        var (c2, _) = await factory.CreateCustomerClientAsync();
        var review = await ReviewAsync(c1, product.Id, 4);

        var dup = await c1.PostAsJsonAsync($"/api/v1/products/{product.Id}/reviews", new ReviewRequest(5, null));
        dup.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await c2.PutAsJsonAsync($"/api/v1/reviews/{review.Id}", new ReviewRequest(1, "x"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await c2.DeleteAsync($"/api/v1/reviews/{review.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var mine = await c1.GetFromJsonAsync<MyReviewDto>($"/api/v1/products/{product.Id}/reviews/mine", HttpExtensions.Json);
        mine!.Id.ShouldBe(review.Id);
        (await c1.PostAsJsonAsync($"/api/v1/products/{product.Id}/reviews", new ReviewRequest(6, null))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await c2.PostAsJsonAsync($"/api/v1/products/{product.Id}/reviews", new ReviewRequest(0, null))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delivered_buyers_get_verified_purchase_badge()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync();
        var (buyer, _) = await factory.CreateCustomerClientAsync();
        var address = await buyer.CreateAddressAsync();
        await buyer.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[0].Id, 1));
        var placed = await (await buyer.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest(address.Id, PaymentMethod.CashOnDelivery, null, null)))
            .Content.ReadFromJsonAsync<OrderPlacedDto>(HttpExtensions.Json);
        foreach (var s in new[] { OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered })
            (await admin.PutAsJsonAsync($"/api/v1/admin/orders/{placed!.Order.Id}/status", new UpdateOrderStatusRequest(s, null, null))).EnsureSuccessStatusCode();

        (await ReviewAsync(buyer, product.Id, 5)).IsVerifiedPurchase.ShouldBeTrue();
        var (other, _) = await factory.CreateCustomerClientAsync();
        (await ReviewAsync(other, product.Id, 3)).IsVerifiedPurchase.ShouldBeFalse();
    }

    [Fact]
    public async Task Customers_cannot_moderate()
    {
        var (c, _) = await factory.CreateCustomerClientAsync();
        (await c.GetAsync("/api/v1/admin/reviews")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Favorites_add_list_and_remove()
    {
        var admin = await factory.CreateAdminClientAsync();
        var p1 = await admin.CreateProductAsync(price: 80);
        var p2 = await admin.CreateProductAsync(price: 90);
        var (c, _) = await factory.CreateCustomerClientAsync();

        (await c.PostAsync($"/api/v1/favorites/{p1.Id}", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await c.PostAsync($"/api/v1/favorites/{p2.Id}", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await c.PostAsync($"/api/v1/favorites/{p2.Id}", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent); // idempotent
        (await c.PostAsync("/api/v1/favorites/999999", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var list = await c.WithLanguage("en").GetFromJsonAsync<List<ProductListItemDto>>("/api/v1/favorites", HttpExtensions.Json);
        list!.Select(p => p.Id).ShouldBe([p2.Id, p1.Id]); // most recent first
        list![0].Name.ShouldBe("Test Honey");

        var ids = await c.GetFromJsonAsync<List<int>>("/api/v1/favorites/ids", HttpExtensions.Json);
        ids!.Count.ShouldBe(2);

        (await c.DeleteAsync($"/api/v1/favorites/{p1.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.DeleteAsync($"/api/v1/admin/products/{p2.Id}")).EnsureSuccessStatusCode(); // deleted products disappear
        (await c.GetFromJsonAsync<List<ProductListItemDto>>("/api/v1/favorites", HttpExtensions.Json))!.ShouldBeEmpty();
    }
}
