using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.IntegrationTests.Infrastructure;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CatalogTests(HoneyHouseApiFactory factory)
{
    private static string Unique(string p) => $"{p}-{Guid.NewGuid():N}"[..20].ToLowerInvariant().TrimEnd('-');
    private static string Sku() => "T-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    private static CreateProductRequest NewProduct(int categoryId, string? slug = null, bool active = true, decimal price = 100, int stock = 10) =>
        new(categoryId, "عسل تجريبي", "Test Honey", "وصف", "Description", slug ?? Unique("test-honey"), "سيناء", active, false,
            [
                new VariantUpsertRequest(Sku(), 250, "250 جرام", "250 g", price, null, stock),
                new VariantUpsertRequest(Sku(), 1000, "1 كيلو", "1 kg", price * 3, price * 4, 0)
            ]);

    private async Task<AdminCategoryDto> CreateCategoryAsync(HttpClient admin, int? parentId = null)
    {
        var res = await admin.PostAsJsonAsync("/api/v1/admin/categories",
            new CategoryUpsertRequest("تصنيف", "Category", null, null, Unique("cat"), null, parentId));
        res.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await res.Content.ReadFromJsonAsync<AdminCategoryDto>(HttpExtensions.Json))!;
    }

    private async Task<AdminProductDto> CreateProductAsync(HttpClient admin, CreateProductRequest req)
    {
        var res = await admin.PostAsJsonAsync("/api/v1/admin/products", req);
        res.StatusCode.ShouldBe(HttpStatusCode.Created, await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<AdminProductDto>(HttpExtensions.Json))!;
    }

    [Fact]
    public async Task Category_tree_is_public_and_localized()
    {
        var client = factory.CreateClient().WithLanguage("en");
        var tree = await client.GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories", HttpExtensions.Json);
        tree!.ShouldContain(c => c.Name == "Natural Honey" && c.Slug == "natural-honey");

        var ar = await factory.CreateClient().WithLanguage("ar").GetFromJsonAsync<List<CategoryDto>>("/api/v1/categories", HttpExtensions.Json);
        ar!.ShouldContain(c => c.Name == "عسل طبيعي");
    }

    [Fact]
    public async Task Customers_cannot_manage_catalog()
    {
        var (customer, _) = await factory.CreateCustomerClientAsync();
        var res = await customer.PostAsJsonAsync("/api/v1/admin/categories", new CategoryUpsertRequest("a", "b", null, null, null, null, null));
        res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await res.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("forbidden");

        (await factory.CreateClient().GetAsync("/api/v1/admin/products")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_category_crud_with_rules()
    {
        var admin = await factory.CreateAdminClientAsync();
        var parent = await CreateCategoryAsync(admin);
        var child = await CreateCategoryAsync(admin, parent.Id);

        // Third level is not allowed
        var third = await admin.PostAsJsonAsync("/api/v1/admin/categories",
            new CategoryUpsertRequest("x", "x", null, null, null, null, child.Id));
        third.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Duplicate slug
        var dup = await admin.PostAsJsonAsync("/api/v1/admin/categories",
            new CategoryUpsertRequest("x", "x", null, null, parent.Slug, null, null));
        dup.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // Cannot delete a parent with children
        (await admin.DeleteAsync($"/api/v1/admin/categories/{parent.Id}")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await admin.DeleteAsync($"/api/v1/admin/categories/{child.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.DeleteAsync($"/api/v1/admin/categories/{parent.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await factory.CreateClient().GetAsync($"/api/v1/categories/{parent.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_creates_product_with_weights_and_it_appears_in_store()
    {
        var admin = await factory.CreateAdminClientAsync();
        var cat = await CreateCategoryAsync(admin);
        var created = await CreateProductAsync(admin, NewProduct(cat.Id, price: 120));
        created.Variants.Count.ShouldBe(2);
        created.Variants[0].StockQuantity.ShouldBe(10);

        var client = factory.CreateClient().WithLanguage("ar");
        var detail = await client.GetFromJsonAsync<ProductDetailDto>($"/api/v1/products/{created.Id}", HttpExtensions.Json);
        detail!.Name.ShouldBe("عسل تجريبي");
        detail.Currency.ShouldBe("EGP");
        detail.Variants.Select(v => v.WeightGrams).ShouldBe([250, 1000]);
        detail.Variants[0].Label.ShouldBe("250 جرام");
        detail.Variants[1].InStock.ShouldBeFalse();

        var bySlug = await client.GetFromJsonAsync<ProductDetailDto>($"/api/v1/products/slug/{created.Slug}", HttpExtensions.Json);
        bySlug!.Id.ShouldBe(created.Id);

        var list = await client.GetFromJsonAsync<PagedResult<ProductListItemDto>>($"/api/v1/products?categoryId={cat.Id}", HttpExtensions.Json);
        var item = list!.Items.ShouldHaveSingleItem();
        item.MinPrice.ShouldBe(120);
        item.MaxPrice.ShouldBe(360);
        item.InStock.ShouldBeTrue();
    }

    [Fact]
    public async Task Inactive_products_are_hidden_from_customers_but_visible_to_admin()
    {
        var admin = await factory.CreateAdminClientAsync();
        var cat = await CreateCategoryAsync(admin);
        var p = await CreateProductAsync(admin, NewProduct(cat.Id, active: false));

        (await factory.CreateClient().GetAsync($"/api/v1/products/{p.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await admin.GetAsync($"/api/v1/admin/products/{p.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_filter_sort_and_paging()
    {
        var client = factory.CreateClient().WithLanguage("en");
        var search = await client.GetFromJsonAsync<PagedResult<ProductListItemDto>>("/api/v1/products?search=Sidr", HttpExtensions.Json);
        search!.Items.ShouldContain(p => p.Slug == "sidr-honey");

        var arabic = await client.GetFromJsonAsync<PagedResult<ProductListItemDto>>("/api/v1/products?search=السدر", HttpExtensions.Json);
        arabic!.Items.ShouldContain(p => p.Slug == "sidr-honey");

        var natural = await client.GetFromJsonAsync<PagedResult<ProductListItemDto>>(
            "/api/v1/products?categoryId=1&sort=price_asc&pageSize=2&page=1", HttpExtensions.Json);
        natural!.Items.Count.ShouldBe(2);
        natural.TotalCount.ShouldBeGreaterThanOrEqualTo(5);
        natural.Items[0].MinPrice.ShouldBeLessThanOrEqualTo(natural.Items[1].MinPrice);

        var cheap = await client.GetFromJsonAsync<PagedResult<ProductListItemDto>>("/api/v1/products?maxPrice=100", HttpExtensions.Json);
        cheap!.Items.ShouldAllBe(p => p.MinPrice <= 100);

        var featured = await client.GetFromJsonAsync<List<ProductListItemDto>>("/api/v1/products/featured", HttpExtensions.Json);
        featured!.ShouldAllBe(p => p.IsFeatured);
        featured!.ShouldContain(p => p.Slug == "sidr-honey");
    }

    [Fact]
    public async Task Duplicate_sku_and_slug_are_rejected()
    {
        var admin = await factory.CreateAdminClientAsync();
        var cat = await CreateCategoryAsync(admin);
        var p = await CreateProductAsync(admin, NewProduct(cat.Id));

        (await admin.PostAsJsonAsync("/api/v1/admin/products", NewProduct(cat.Id, p.Slug))).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var dupSku = await admin.PostAsJsonAsync($"/api/v1/admin/products/{p.Id}/variants",
            new VariantUpsertRequest(p.Variants[0].Sku, 500, "500", "500", 50, null));
        dupSku.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await dupSku.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("sku_taken");
    }

    [Fact]
    public async Task Product_validation_errors()
    {
        var admin = await factory.CreateAdminClientAsync();
        var res = await admin.PostAsJsonAsync("/api/v1/admin/products",
            new CreateProductRequest(1, "", "", null, null, "Bad Slug!", null, true, false,
                [new VariantUpsertRequest("", 0, "", "", -5, null, -1)]));
        res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await res.ReadJsonAsync()).GetProperty("errors");
        errors.TryGetProperty("nameAr", out _).ShouldBeTrue();
        errors.TryGetProperty("slug", out _).ShouldBeTrue();
        errors.TryGetProperty("variants[0].price", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Variant_update_and_last_variant_protection()
    {
        var admin = await factory.CreateAdminClientAsync();
        var cat = await CreateCategoryAsync(admin);
        var p = await CreateProductAsync(admin, NewProduct(cat.Id));
        var v0 = p.Variants[0];
        var v1 = p.Variants[1];

        var upd = await admin.PutAsJsonAsync($"/api/v1/admin/variants/{v0.Id}",
            new VariantUpsertRequest(v0.Sku, 250, "ربع كيلو", "Quarter kg", 99.5m, 120, StockQuantity: 999));
        var dto = (await upd.Content.ReadFromJsonAsync<AdminVariantDto>(HttpExtensions.Json))!;
        dto.Price.ShouldBe(99.5m);
        dto.StockQuantity.ShouldBe(10); // stock is not editable here

        (await admin.DeleteAsync($"/api/v1/admin/variants/{v1.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var last = await admin.DeleteAsync($"/api/v1/admin/variants/{v0.Id}");
        last.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await last.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("product_needs_variant");
    }

    [Fact]
    public async Task Product_soft_delete_hides_it_and_frees_nothing()
    {
        var admin = await factory.CreateAdminClientAsync();
        var cat = await CreateCategoryAsync(admin);
        var p = await CreateProductAsync(admin, NewProduct(cat.Id));
        (await admin.DeleteAsync($"/api/v1/admin/products/{p.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/v1/admin/products/{p.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        // Slug of a deleted product stays reserved (keeps old links/order history consistent)
        (await admin.PostAsJsonAsync("/api/v1/admin/products", NewProduct(cat.Id, p.Slug))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Image_upload_set_main_and_delete()
    {
        var admin = await factory.CreateAdminClientAsync();
        var cat = await CreateCategoryAsync(admin);
        var p = await CreateProductAsync(admin, NewProduct(cat.Id));

        async Task<ProductImageDto> Upload(string type = "image/png")
        {
            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 1, 2, 3]);
            file.Headers.ContentType = new MediaTypeHeaderValue(type);
            form.Add(file, "file", "honey.png");
            var res = await admin.PostAsync($"/api/v1/admin/products/{p.Id}/images", form);
            res.StatusCode.ShouldBe(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
            return (await res.Content.ReadFromJsonAsync<ProductImageDto>(HttpExtensions.Json))!;
        }

        var first = await Upload();
        first.IsMain.ShouldBeTrue();
        first.Url.ShouldStartWith("/uploads/products/");
        (await factory.CreateClient().GetAsync(first.Url)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await Upload();
        second.IsMain.ShouldBeFalse();
        (await admin.PutAsync($"/api/v1/admin/products/{p.Id}/images/{second.Id}/main", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var detail = await admin.GetFromJsonAsync<AdminProductDto>($"/api/v1/admin/products/{p.Id}", HttpExtensions.Json);
        detail!.Images.First().Id.ShouldBe(second.Id);

        (await admin.DeleteAsync($"/api/v1/admin/products/{p.Id}/images/{first.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await factory.CreateClient().GetAsync(first.Url)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var bad = new MultipartFormDataContent();
        var gif = new ByteArrayContent([1, 2, 3]);
        gif.Headers.ContentType = new MediaTypeHeaderValue("image/gif");
        bad.Add(gif, "file", "x.gif");
        (await admin.PostAsync($"/api/v1/admin/products/{p.Id}/images", bad)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
