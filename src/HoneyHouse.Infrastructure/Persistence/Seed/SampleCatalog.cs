using HoneyHouse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Infrastructure.Persistence.Seed;

/// <summary>Demo catalog for development environments.</summary>
public static class SampleCatalog
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Categories.IgnoreQueryFilters().AnyAsync(ct)) return;

        var honey = new Category { NameAr = "عسل طبيعي", NameEn = "Natural Honey", Slug = "natural-honey", SortOrder = 1,
            DescriptionAr = "عسل نحل مصري طبيعي 100%", DescriptionEn = "100% natural Egyptian bee honey" };
        var beeProducts = new Category { NameAr = "منتجات النحل", NameEn = "Bee Products", Slug = "bee-products", SortOrder = 2,
            DescriptionAr = "غذاء ملكات، بروبوليس، حبوب لقاح وشمع", DescriptionEn = "Royal jelly, propolis, pollen and beeswax" };
        var gifts = new Category { NameAr = "علب هدايا", NameEn = "Gift Boxes", Slug = "gift-boxes", SortOrder = 3 };
        db.Categories.AddRange(honey, beeProducts, gifts);

        db.Products.AddRange(
            Honey(honey, "عسل السدر", "Sidr Honey", "sidr-honey", "SIDR", "سيناء", true, 220, 420, 800),
            Honey(honey, "عسل البرسيم", "Clover Honey", "clover-honey", "CLOVER", "الدلتا", true, 90, 170, 320),
            Honey(honey, "عسل الموالح", "Citrus Blossom Honey", "citrus-honey", "CITRUS", "القليوبية", false, 110, 200, 380),
            Honey(honey, "عسل حبة البركة", "Black Seed Honey", "black-seed-honey", "BLACKSEED", "الفيوم", true, 180, 340, 650),
            Honey(honey, "عسل القطن", "Cotton Honey", "cotton-honey", "COTTON", "كفر الشيخ", false, 85, 160, 300),
            Single(beeProducts, "غذاء ملكات النحل", "Royal Jelly", "royal-jelly", "HH-RJ-50", 50, "50 جرام", "50 g", 350),
            Single(beeProducts, "حبوب لقاح النحل", "Bee Pollen", "bee-pollen", "HH-POLLEN-250", 250, "250 جرام", "250 g", 180),
            Single(beeProducts, "بروبوليس (صمغ النحل)", "Propolis", "propolis", "HH-PROP-30", 30, "30 مل", "30 ml", 150),
            Single(gifts, "علبة هدايا العسل الفاخرة", "Premium Honey Gift Box", "premium-gift-box", "HH-GIFT-PREM", 1500, "3 برطمانات", "3 jars", 950));

        await db.SaveChangesAsync(ct);
    }

    private static Product Honey(Category c, string ar, string en, string slug, string code, string origin, bool featured,
        decimal p250, decimal p500, decimal p1000) => new()
    {
        Category = c, NameAr = ar, NameEn = en, Slug = slug, Origin = origin, IsFeatured = featured,
        DescriptionAr = $"{ar} مصري طبيعي 100% من مناحل {origin}.",
        DescriptionEn = $"100% natural Egyptian {en.ToLowerInvariant()}.",
        Variants =
        [
            new ProductVariant { Sku = $"HH-{code}-250", WeightGrams = 250, LabelAr = "250 جرام", LabelEn = "250 g", Price = p250, StockQuantity = 50 },
            new ProductVariant { Sku = $"HH-{code}-500", WeightGrams = 500, LabelAr = "500 جرام", LabelEn = "500 g", Price = p500, StockQuantity = 40 },
            new ProductVariant { Sku = $"HH-{code}-1000", WeightGrams = 1000, LabelAr = "1 كيلو", LabelEn = "1 kg", Price = p1000, StockQuantity = 25 }
        ],
        Images = [new ProductImage { Url = $"/images/products/{slug}.jpg", IsMain = true, AltText = en }]
    };

    private static Product Single(Category c, string ar, string en, string slug, string sku, int grams, string labelAr, string labelEn, decimal price) => new()
    {
        Category = c, NameAr = ar, NameEn = en, Slug = slug,
        Variants = [new ProductVariant { Sku = sku, WeightGrams = grams, LabelAr = labelAr, LabelEn = labelEn, Price = price, StockQuantity = 30 }],
        Images = [new ProductImage { Url = $"/images/products/{slug}.jpg", IsMain = true, AltText = en }]
    };
}
