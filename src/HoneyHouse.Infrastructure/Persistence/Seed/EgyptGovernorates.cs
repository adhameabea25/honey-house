using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Infrastructure.Persistence.Seed;

/// <summary>The 27 Egyptian governorates with default shipping fees in EGP (editable by admins).</summary>
public static class EgyptGovernorates
{
    private static readonly (string Ar, string En, decimal Fee)[] Data =
    [
        ("القاهرة", "Cairo", 50), ("الجيزة", "Giza", 50), ("الإسكندرية", "Alexandria", 60),
        ("القليوبية", "Qalyubia", 55), ("الشرقية", "Sharqia", 65), ("الدقهلية", "Dakahlia", 65),
        ("الغربية", "Gharbia", 65), ("المنوفية", "Monufia", 65), ("البحيرة", "Beheira", 65),
        ("كفر الشيخ", "Kafr El Sheikh", 70), ("دمياط", "Damietta", 70), ("بورسعيد", "Port Said", 70),
        ("الإسماعيلية", "Ismailia", 70), ("السويس", "Suez", 70), ("الفيوم", "Faiyum", 75),
        ("بني سويف", "Beni Suef", 75), ("المنيا", "Minya", 80), ("أسيوط", "Asyut", 85),
        ("سوهاج", "Sohag", 85), ("قنا", "Qena", 90), ("الأقصر", "Luxor", 90),
        ("أسوان", "Aswan", 95), ("البحر الأحمر", "Red Sea", 100), ("الوادي الجديد", "New Valley", 110),
        ("مطروح", "Matrouh", 100), ("شمال سيناء", "North Sinai", 110), ("جنوب سيناء", "South Sinai", 110)
    ];

    public static IEnumerable<Governorate> All() =>
        Data.Select(g => new Governorate { NameAr = g.Ar, NameEn = g.En, ShippingFee = g.Fee, IsActive = true });
}
