using System.Net.Http.Json;
using HoneyHouse.Application.Features.Addresses;
using HoneyHouse.Application.Features.Catalog;

namespace HoneyHouse.IntegrationTests.Infrastructure;

public static class TestData
{
    public static string Sku() => "T-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    public static string Slug(string prefix = "p") => $"{prefix}-{Guid.NewGuid():N}"[..20];

    /// <summary>Creates an active product (category 1) with a 250 g and a 1 kg option.</summary>
    public static async Task<AdminProductDto> CreateProductAsync(this HttpClient admin, decimal price = 100, int stock = 10, int stock1Kg = 5)
    {
        var res = await admin.PostAsJsonAsync("/api/v1/admin/products", new CreateProductRequest(
            1, "عسل اختبار", "Test Honey", null, null, Slug(), null, true, false,
            [
                new VariantUpsertRequest(Sku(), 250, "250 جرام", "250 g", price, null, stock),
                new VariantUpsertRequest(Sku(), 1000, "1 كيلو", "1 kg", price * 3, null, stock1Kg)
            ]));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AdminProductDto>(HttpExtensions.Json))!;
    }

    public static AddressUpsertRequest Address(int governorateId = 1, bool isDefault = false) =>
        new(governorateId, "محمد أحمد", "01012345678", "مدينة نصر", "الحي السابع", "شارع عباس العقاد", "12", "3", "5", "بجوار المسجد", isDefault);

    public static async Task<AddressDto> CreateAddressAsync(this HttpClient customer, int governorateId = 1)
    {
        var res = await customer.PostAsJsonAsync("/api/v1/addresses", Address(governorateId));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<AddressDto>(HttpExtensions.Json))!;
    }
}
