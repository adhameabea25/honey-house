using HoneyHouse.Application.Localization;
using HoneyHouse.Domain;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Application.Features.Orders;

public sealed class OrderMapper(ILocalizer l)
{
    public string StatusName(OrderStatus s) => l[$"status_{s}"];
    public string PaymentStatusName(PaymentStatus s) => l[$"payment_{s}"];
    private string Pick(string ar, string en) => l.Language == "en" ? en : ar;

    public OrderSummaryDto ToSummary(Order o) => new(o.Id, o.OrderNumber, o.Status, StatusName(o.Status), o.PaymentMethod,
        o.PaymentStatus, PaymentStatusName(o.PaymentStatus), o.Total, "EGP", o.Items.Sum(i => i.Quantity), o.CreatedAt);

    public OrderDetailDto ToDetail(Order o) => new(
        o.Id, o.OrderNumber, o.Status, StatusName(o.Status), o.PaymentMethod, o.PaymentStatus, PaymentStatusName(o.PaymentStatus),
        o.Subtotal, o.DiscountAmount, o.ShippingFee, o.Total, "EGP", o.CouponCode,
        new ShippingAddressDto(o.ShipRecipientName, o.ShipPhone, o.ShipGovernorateName, o.ShipCity, o.ShipAddressLine, o.ShipLandmark),
        o.CustomerNotes, o.TrackingNumber,
        o.Items.OrderBy(i => i.Id).Select(i => new OrderItemDto(i.Id, i.ProductId, i.VariantId, Pick(i.ProductNameAr, i.ProductNameEn),
            Pick(i.VariantLabelAr, i.VariantLabelEn), i.Sku, i.UnitPrice, i.Quantity, i.LineTotal)).ToList(),
        o.StatusHistory.OrderBy(h => h.CreatedAt).ThenBy(h => h.Id)
            .Select(h => new OrderStatusEntryDto(h.ToStatus, StatusName(h.ToStatus), h.Note, h.CreatedAt)).ToList(),
        OrderStateMachine.CanCustomerCancel(o.Status), o.CreatedAt, o.DeliveredAt, o.CancelledAt);
}
