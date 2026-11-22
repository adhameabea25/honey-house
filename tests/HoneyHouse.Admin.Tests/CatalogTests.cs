using System.Net;
using System.Net.Http.Headers;
using HoneyHouse.Admin.Tests.Infrastructure;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Admin.Tests;

[Collection(AdminCollection.Name)]
public class CatalogTests(AdminAppFactory factory)
{
    // 1x1 transparent PNG
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    [Fact]
    public async Task Categories_list_shows_seeded_tree()
    {
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync("/Categories");
        html.ShouldContain("data-testid=\"categories\"");
        html.ShouldContain("عسل"); // seeded honey category
    }

    [Fact]
    public async Task Category_can_be_created_with_image_edited_and_deleted()
    {
        var client = await factory.SignedInAsync();
        var slug = TestData.Unique("cat");
        var page = await client.GetPageAsync("/Categories/Create");

        using var form = new MultipartFormDataContent
        {
            { new StringContent("تصنيف تجريبي"), "NameAr" },
            { new StringContent("Test Category"), "NameEn" },
            { new StringContent(slug), "Slug" },
            { new StringContent("3"), "SortOrder" },
            { new StringContent("true"), "IsActive" },
            { new StringContent(BrowserExtensions.Token(page)), "__RequestVerificationToken" },
        };
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "image", "cat.png");

        var created = await client.PostAsync("/Categories/Create", form);
        created.StatusCode.ShouldBe(HttpStatusCode.Redirect, await created.Content.ReadAsStringAsync());

        Category? category = null;
        await factory.WithDbAsync(async db => category = await db.Categories.SingleAsync(c => c.Slug == slug));
        category!.ImageUrl.ShouldStartWith("/uploads/categories/");
        File.Exists(Path.Combine(factory.UploadsPath, "categories", Path.GetFileName(category.ImageUrl!))).ShouldBeTrue();

        var edit = await client.SubmitAsync($"/Categories/Edit/{category.Id}", $"/Categories/Edit/{category.Id}",
            new { NameAr = "تصنيف معدل", NameEn = "Edited", Slug = slug, SortOrder = "1", IsActive = "false", RemoveImage = "true" });
        edit.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        await factory.WithDbAsync(async db =>
        {
            var c = await db.Categories.AsNoTracking().SingleAsync(x => x.Id == category.Id);
            c.NameAr.ShouldBe("تصنيف معدل");
            c.IsActive.ShouldBeFalse();
            c.ImageUrl.ShouldBeNull();
        });
        File.Exists(Path.Combine(factory.UploadsPath, "categories", Path.GetFileName(category.ImageUrl!))).ShouldBeFalse();

        var deleted = await client.SubmitAsync("/Categories", $"/Categories/Delete/{category.Id}", new { });
        deleted.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.GetPageAsync("/Categories")).ShouldContain("تم الحذف");
    }

    [Fact]
    public async Task Category_form_shows_localized_validation_errors()
    {
        var client = await factory.SignedInAsync();
        var response = await client.SubmitAsync("/Categories/Create", "/Categories/Create", new { NameAr = "", NameEn = "", Slug = "Bad Slug!" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("هذا الحقل مطلوب");
    }

    [Fact]
    public async Task Category_with_products_cannot_be_deleted()
    {
        var client = await factory.SignedInAsync();
        var response = await client.SubmitAsync("/Categories", "/Categories/Delete/1", new { });
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.GetPageAsync("/Categories")).ShouldContain("لا يمكن حذف تصنيف يحتوي على منتجات");
    }

    [Fact]
    public async Task Product_lifecycle_create_edit_variants_images_delete()
    {
        var client = await factory.SignedInAsync();
        var sku = TestData.Sku();
        var slug = TestData.Unique("prod");

        // Create with first weight option
        var created = await client.SubmitAsync("/Products/Create", "/Products/Create", new Dictionary<string, string>
        {
            ["CategoryId"] = "1", ["NameAr"] = "عسل زهور البرسيم", ["NameEn"] = "Clover Honey", ["Slug"] = slug,
            ["IsActive"] = "true", ["Variant.Sku"] = sku, ["Variant.WeightGrams"] = "500", ["Variant.LabelAr"] = "نصف كيلو",
            ["Variant.LabelEn"] = "500 g", ["Variant.Price"] = "180.50", ["Variant.StockQuantity"] = "12",
            ["Variant.LowStockThreshold"] = "3", ["Variant.IsActive"] = "true"
        });
        created.StatusCode.ShouldBe(HttpStatusCode.Redirect, await created.Content.ReadAsStringAsync());
        var editUrl = created.Location();
        editUrl.ShouldStartWith("/Products/Edit/");
        var id = int.Parse(editUrl.Split('/').Last());

        var html = await client.GetPageAsync(editUrl);
        html.ShouldContain("عسل زهور البرسيم");
        html.ShouldContain(sku);
        html.ShouldContain("180.5");

        // Opening stock is recorded as an inventory transaction
        await factory.WithDbAsync(async db =>
            (await db.InventoryTransactions.CountAsync(t => t.Variant.Sku == sku && t.QuantityChange == 12)).ShouldBe(1));

        // Edit product
        var edited = await client.SubmitAsync(editUrl, editUrl, new Dictionary<string, string>
        {
            ["CategoryId"] = "1", ["NameAr"] = "عسل برسيم", ["NameEn"] = "Clover Honey", ["Slug"] = slug, ["IsActive"] = "true", ["IsFeatured"] = "true"
        });
        edited.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // Add a second weight
        var sku2 = TestData.Sku();
        var added = await client.SubmitAsync($"/Products/AddVariant/{id}", $"/Products/AddVariant/{id}", new Dictionary<string, string>
        {
            ["Sku"] = sku2, ["WeightGrams"] = "1000", ["LabelAr"] = "1 كيلو", ["LabelEn"] = "1 kg", ["Price"] = "340",
            ["CompareAtPrice"] = "360", ["StockQuantity"] = "4", ["LowStockThreshold"] = "2", ["IsActive"] = "true"
        });
        added.StatusCode.ShouldBe(HttpStatusCode.Redirect, await added.Content.ReadAsStringAsync());

        // Edit the second weight: price changes, stock field is ignored
        int variantId = 0;
        await factory.WithDbAsync(async db => variantId = (await db.ProductVariants.SingleAsync(v => v.Sku == sku2)).Id);
        var url = $"/Products/EditVariant/{id}?variantId={variantId}";
        var updated = await client.SubmitAsync(url, url, new Dictionary<string, string>
        {
            ["Sku"] = sku2, ["WeightGrams"] = "1000", ["LabelAr"] = "1 كيلو", ["LabelEn"] = "1 kg", ["Price"] = "350",
            ["StockQuantity"] = "999", ["LowStockThreshold"] = "2", ["IsActive"] = "true"
        });
        updated.StatusCode.ShouldBe(HttpStatusCode.Redirect, await updated.Content.ReadAsStringAsync());
        await factory.WithDbAsync(async db =>
        {
            var v = await db.ProductVariants.AsNoTracking().SingleAsync(x => x.Id == variantId);
            v.Price.ShouldBe(350);
            v.StockQuantity.ShouldBe(4);
            var p = await db.Products.AsNoTracking().SingleAsync(x => x.Id == id);
            p.NameAr.ShouldBe("عسل برسيم");
            p.IsFeatured.ShouldBeTrue();
        });

        // Upload two images, first becomes main; then switch main and delete one
        var page = await client.GetPageAsync(editUrl);
        using var upload = new MultipartFormDataContent { { new StringContent(BrowserExtensions.Token(page)), "__RequestVerificationToken" } };
        foreach (var name in new[] { "a.png", "b.png" })
        {
            var f = new ByteArrayContent(Png);
            f.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            upload.Add(f, "images", name);
        }
        (await client.PostAsync($"/Products/UploadImages/{id}", upload)).StatusCode.ShouldBe(HttpStatusCode.Redirect);

        List<ProductImage> images = [];
        await factory.WithDbAsync(async db => images = await db.ProductImages.Where(i => i.ProductId == id).OrderBy(i => i.SortOrder).ToListAsync());
        images.Count.ShouldBe(2);
        images[0].IsMain.ShouldBeTrue();

        (await client.SubmitAsync(editUrl, $"/Products/SetMainImage/{id}?imageId={images[1].Id}", new { })).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.SubmitAsync(editUrl, $"/Products/DeleteImage/{id}?imageId={images[0].Id}", new { })).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        await factory.WithDbAsync(async db =>
        {
            var left = await db.ProductImages.Where(i => i.ProductId == id).ToListAsync();
            left.Count.ShouldBe(1);
            left[0].IsMain.ShouldBeTrue();
        });

        // Listing filters
        (await client.GetPageAsync("/Products?search=" + Uri.EscapeDataString("عسل برسيم"))).ShouldContain($"/Products/Edit/{id}");

        // Delete (soft)
        (await client.SubmitAsync(editUrl, $"/Products/Delete/{id}", new { })).Location().ShouldBe("/Products");
        (await client.GetAsync(editUrl)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Product_create_rejects_duplicate_sku_and_bad_values()
    {
        var existing = await factory.CreateProductAsync();
        var client = await factory.SignedInAsync();
        var response = await client.SubmitAsync("/Products/Create", "/Products/Create", new Dictionary<string, string>
        {
            ["CategoryId"] = "1", ["NameAr"] = "منتج", ["NameEn"] = "Product", ["IsActive"] = "true",
            ["Variant.Sku"] = existing.Variants[0].Sku, ["Variant.WeightGrams"] = "250", ["Variant.LabelAr"] = "250 جرام",
            ["Variant.LabelEn"] = "250 g", ["Variant.Price"] = "50", ["Variant.LowStockThreshold"] = "5", ["Variant.IsActive"] = "true"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("رمز المنتج (SKU) مستخدم بالفعل");

        var bad = await client.SubmitAsync("/Products/Create", "/Products/Create", new Dictionary<string, string>
        {
            ["CategoryId"] = "1", ["NameAr"] = "منتج", ["NameEn"] = "Product", ["Variant.Sku"] = TestData.Sku(),
            ["Variant.WeightGrams"] = "0", ["Variant.LabelAr"] = "x", ["Variant.LabelEn"] = "x", ["Variant.Price"] = "abc"
        });
        var html = await bad.Content.ReadAsStringAsync();
        html.ShouldContain("يجب إدخال رقم"); // price not a number... or invalid value
    }

    [Fact]
    public async Task Inactive_filter_lists_only_hidden_products()
    {
        var hidden = await factory.CreateProductAsync(nameAr: "منتج مخفي للاختبار");
        await factory.WithDbAsync(async db =>
        {
            var p = await db.Products.SingleAsync(x => x.Id == hidden.Id);
            p.IsActive = false;
            await db.SaveChangesAsync();
        });
        var client = await factory.SignedInAsync();
        var inactive = await client.GetPageAsync("/Products?status=inactive");
        inactive.ShouldContain("منتج مخفي للاختبار");
        inactive.ShouldNotContain("bg-success\">نشط");
        (await client.GetPageAsync("/Products?status=active")).ShouldNotContain("منتج مخفي للاختبار");
    }

    [Fact]
    public async Task Invalid_image_type_is_rejected()
    {
        var product = await factory.CreateProductAsync();
        var client = await factory.SignedInAsync();
        var page = await client.GetPageAsync($"/Products/Edit/{product.Id}");
        using var upload = new MultipartFormDataContent { { new StringContent(BrowserExtensions.Token(page)), "__RequestVerificationToken" } };
        var f = new ByteArrayContent("not an image"u8.ToArray());
        f.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        upload.Add(f, "images", "x.txt");
        (await client.PostAsync($"/Products/UploadImages/{product.Id}", upload)).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.GetPageAsync($"/Products/Edit/{product.Id}")).ShouldContain("الصورة يجب أن تكون JPG");
    }

    [Fact]
    public async Task Manager_can_manage_catalog()
    {
        var client = await factory.SignedInAsync(await factory.CreateUserAsync(Roles.Manager));
        await client.GetPageAsync("/Products");
        await client.GetPageAsync("/Categories/Create");
    }
}
