using System.Globalization;
using HoneyHouse.Admin.Localization;
using HoneyHouse.Application.Features.Analytics;

namespace HoneyHouse.Admin.Common;

/// <summary>Display helpers: EGP amounts, Cairo local dates, status badges. Numbers use Latin digits in both languages.</summary>
public static class Fmt
{
    private static readonly CultureInfo Num = CultureInfo.InvariantCulture;

    public static string Money(decimal amount, IText t) => $"{amount.ToString("#,0.##", Num)} {t["currency"]}";
    public static string Number(decimal value) => value.ToString("#,0.##", Num);

    public static System.DateTime ToLocal(System.DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.SpecifyKind(utc, DateTimeKind.Utc), AnalyticsService.StoreTimeZone);

    public static string Date(System.DateTime utc) => ToLocal(utc).ToString("yyyy-MM-dd", Num);
    public static string DateTime(System.DateTime utc) => ToLocal(utc).ToString("yyyy-MM-dd HH:mm", Num);

    /// <summary>Store-local calendar date (start of day) to UTC.</summary>
    public static System.DateTime LocalDateToUtc(DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), AnalyticsService.StoreTimeZone);

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(ToLocal(clock.GetUtcNow().UtcDateTime));

    public static string StatusBadge(HoneyHouse.Domain.Enums.OrderStatus s) => s switch
    {
        HoneyHouse.Domain.Enums.OrderStatus.Pending => "bg-warning text-dark",
        HoneyHouse.Domain.Enums.OrderStatus.Confirmed => "bg-info text-dark",
        HoneyHouse.Domain.Enums.OrderStatus.Processing => "bg-primary",
        HoneyHouse.Domain.Enums.OrderStatus.Shipped or HoneyHouse.Domain.Enums.OrderStatus.OutForDelivery => "bg-secondary",
        HoneyHouse.Domain.Enums.OrderStatus.Delivered => "bg-success",
        _ => "bg-danger"
    };

    public static string PaymentBadge(HoneyHouse.Domain.Enums.PaymentStatus s) => s switch
    {
        HoneyHouse.Domain.Enums.PaymentStatus.Paid => "bg-success",
        HoneyHouse.Domain.Enums.PaymentStatus.Pending => "bg-warning text-dark",
        HoneyHouse.Domain.Enums.PaymentStatus.Refunded => "bg-secondary",
        _ => "bg-danger"
    };
}
