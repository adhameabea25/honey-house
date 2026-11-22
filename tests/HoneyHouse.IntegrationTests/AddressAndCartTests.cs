using System.Net;
using System.Net.Http.Json;
using HoneyHouse.Application.Features.Addresses;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.IntegrationTests.Infrastructure;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AddressAndCartTests(HoneyHouseApiFactory factory)
{
    [Fact]
    public async Task Governorates_are_public_with_fees()
    {
        var list = await factory.CreateClient().WithLanguage("ar").GetFromJsonAsync<List<GovernorateDto>>("/api/v1/governorates", HttpExtensions.Json);
        list!.Count.ShouldBeGreaterThanOrEqualTo(26);
        var cairo = list.First(g => g.Id == 1);
        cairo.Name.ShouldBe("القاهرة");
        cairo.ShippingFee.ShouldBe(50);
        cairo.Currency.ShouldBe("EGP");
    }

    [Fact]
    public async Task Admin_updates_shipping_fee_and_disables_governorate()
    {
        var admin = await factory.CreateAdminClientAsync();
        (await admin.PutAsJsonAsync("/api/v1/admin/governorates/27", new UpdateGovernorateRequest(130, false))).StatusCode.ShouldBe(HttpStatusCode.OK);

        var list = await factory.CreateClient().GetFromJsonAsync<List<GovernorateDto>>("/api/v1/governorates", HttpExtensions.Json);
        list!.ShouldNotContain(g => g.Id == 27);

        var (customer, _) = await factory.CreateCustomerClientAsync();
        (await customer.PostAsJsonAsync("/api/v1/addresses", TestData.Address(27))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await admin.PutAsJsonAsync("/api/v1/admin/governorates/27", new UpdateGovernorateRequest(110, true))).StatusCode.ShouldBe(HttpStatusCode.OK);
        var (manager, _) = await factory.CreateCustomerClientAsync();
        (await manager.PutAsJsonAsync("/api/v1/admin/governorates/27", new UpdateGovernorateRequest(1, true))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Address_crud_and_default_handling()
    {
        var (client, _) = await factory.CreateCustomerClientAsync();
        var first = await client.CreateAddressAsync();
        first.IsDefault.ShouldBeTrue();
        first.ShippingFee.ShouldBe(50);

        var second = await client.CreateAddressAsync(governorateId: 3);
        second.IsDefault.ShouldBeFalse();

        (await client.PutAsync($"/api/v1/addresses/{second.Id}/default", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var all = await client.GetFromJsonAsync<List<AddressDto>>("/api/v1/addresses", HttpExtensions.Json);
        all!.Single(a => a.IsDefault).Id.ShouldBe(second.Id);

        var upd = await client.PutAsJsonAsync($"/api/v1/addresses/{first.Id}", TestData.Address(2) with { City = "الدقي" });
        (await upd.Content.ReadFromJsonAsync<AddressDto>(HttpExtensions.Json))!.City.ShouldBe("الدقي");

        (await client.DeleteAsync($"/api/v1/addresses/{second.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        all = await client.GetFromJsonAsync<List<AddressDto>>("/api/v1/addresses", HttpExtensions.Json);
        all!.ShouldHaveSingleItem().IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task Users_cannot_see_each_others_addresses()
    {
        var (alice, _) = await factory.CreateCustomerClientAsync();
        var (bob, _) = await factory.CreateCustomerClientAsync();
        var address = await alice.CreateAddressAsync();
        (await bob.GetAsync($"/api/v1/addresses/{address.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.DeleteAsync($"/api/v1/addresses/{address.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Address_phone_must_be_egyptian()
    {
        var (client, _) = await factory.CreateCustomerClientAsync();
        var res = await client.PostAsJsonAsync("/api/v1/addresses", TestData.Address() with { Phone = "12345" });
        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await res.ReadJsonAsync()).GetProperty("errors").TryGetProperty("phone", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Cart_add_merge_update_remove_and_totals()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(price: 100, stock: 10);
        var v250 = product.Variants.Single(v => v.WeightGrams == 250);
        var v1k = product.Variants.Single(v => v.WeightGrams == 1000);
        var (client, _) = await factory.CreateCustomerClientAsync();

        (await client.GetFromJsonAsync<CartDto>("/api/v1/cart", HttpExtensions.Json))!.Items.ShouldBeEmpty();

        await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(v250.Id, 2));
        await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(v250.Id, 1));
        var res = await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(v1k.Id, 1));
        var cart = (await res.Content.ReadFromJsonAsync<CartDto>(HttpExtensions.Json))!;

        cart.Items.Count.ShouldBe(2);
        cart.Items.Single(i => i.VariantId == v250.Id).Quantity.ShouldBe(3);
        cart.Subtotal.ShouldBe(3 * 100 + 300);
        cart.ItemCount.ShouldBe(4);
        cart.Currency.ShouldBe("EGP");

        var itemId = cart.Items.Single(i => i.VariantId == v250.Id).Id;
        cart = (await (await client.PutAsJsonAsync($"/api/v1/cart/items/{itemId}", new UpdateCartItemRequest(5)))
            .Content.ReadFromJsonAsync<CartDto>(HttpExtensions.Json))!;
        cart.Subtotal.ShouldBe(5 * 100 + 300);

        cart = (await (await client.DeleteAsync($"/api/v1/cart/items/{itemId}")).Content.ReadFromJsonAsync<CartDto>(HttpExtensions.Json))!;
        cart.Items.ShouldHaveSingleItem();

        (await client.DeleteAsync("/api/v1/cart")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<CartDto>("/api/v1/cart", HttpExtensions.Json))!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Cart_enforces_stock_and_max_quantity()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 3);
        var variant = product.Variants.Single(v => v.WeightGrams == 250);
        var (client, _) = await factory.CreateCustomerClientAsync();
        client.WithLanguage("en");

        var res = await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(variant.Id, 4));
        res.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var json = await res.ReadJsonAsync();
        json.GetProperty("code").GetString().ShouldBe("insufficient_stock");
        json.GetProperty("title").GetString().ShouldBe("Requested quantity is not available. Only 3 left.");

        var big = await admin.CreateProductAsync(stock: 100);
        var max = await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(big.Variants[0].Id, 21));
        (await max.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("max_quantity_exceeded");
    }

    [Fact]
    public async Task Deactivated_items_are_flagged_and_excluded_from_subtotal()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(price: 50);
        var (client, _) = await factory.CreateCustomerClientAsync();
        await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[0].Id, 2));
        await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[1].Id, 1));

        (await admin.DeleteAsync($"/api/v1/admin/variants/{product.Variants[0].Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var cart = await client.GetFromJsonAsync<CartDto>("/api/v1/cart", HttpExtensions.Json);
        cart!.HasUnavailableItems.ShouldBeTrue();
        cart.Items.Single(i => i.VariantId == product.Variants[0].Id).IsAvailable.ShouldBeFalse();
        cart.Subtotal.ShouldBe(150);

        (await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[0].Id, 1)))
            .StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Cart_requires_authentication() =>
        (await factory.CreateClient().GetAsync("/api/v1/cart")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
}
