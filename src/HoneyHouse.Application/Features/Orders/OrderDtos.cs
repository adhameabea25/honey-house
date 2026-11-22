using FluentValidation;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Application.Features.Orders;

public sealed record CheckoutPreviewRequest(int AddressId, string? CouponCode);
public sealed record PlaceOrderRequest(int AddressId, PaymentMethod PaymentMethod, string? CouponCode, string? Notes);
public sealed record CancelOrderRequest(string? Reason);
public sealed record UpdateOrderStatusRequest(OrderStatus Status, string? Note, string? TrackingNumber);

public sealed record CheckoutLineDto(int VariantId, string ProductName, string VariantLabel, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record CheckoutSummaryDto(
    IReadOnlyList<CheckoutLineDto> Items, decimal Subtotal, decimal DiscountAmount, decimal ShippingFee, decimal Total,
    string Currency, string? CouponCode, string? CouponError, string? CouponErrorCode, bool CanPlaceOrder, string? BlockingReason);

public sealed record OrderItemDto(int Id, int ProductId, int VariantId, string ProductName, string VariantLabel, string Sku,
    decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record ShippingAddressDto(string RecipientName, string Phone, string Governorate, string City, string AddressLine, string? Landmark);

public sealed record OrderStatusEntryDto(OrderStatus Status, string StatusName, string? Note, DateTime ChangedAt);

public sealed record OrderSummaryDto(int Id, string OrderNumber, OrderStatus Status, string StatusName, PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus, string PaymentStatusName, decimal Total, string Currency, int ItemCount, DateTime CreatedAt);

public sealed record OrderDetailDto(
    int Id, string OrderNumber, OrderStatus Status, string StatusName, PaymentMethod PaymentMethod, PaymentStatus PaymentStatus,
    string PaymentStatusName, decimal Subtotal, decimal DiscountAmount, decimal ShippingFee, decimal Total, string Currency,
    string? CouponCode, ShippingAddressDto ShippingAddress, string? Notes, string? TrackingNumber, IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusEntryDto> History, bool CanCancel, DateTime CreatedAt, DateTime? DeliveredAt, DateTime? CancelledAt);

public sealed record OrderPlacedDto(OrderDetailDto Order, string? PaymentUrl);

public sealed record OrderQuery(OrderStatus? Status = null, int Page = 1, int PageSize = 20);

public sealed record AdminOrderQuery(
    OrderStatus? Status = null, PaymentStatus? PaymentStatus = null, PaymentMethod? PaymentMethod = null,
    DateTime? From = null, DateTime? To = null, string? Search = null, int Page = 1, int PageSize = 20);

public sealed record AdminOrderListItemDto(int Id, string OrderNumber, string CustomerName, string CustomerEmail, string? CustomerPhone,
    OrderStatus Status, PaymentMethod PaymentMethod, PaymentStatus PaymentStatus, decimal Total, int ItemCount, string Governorate, DateTime CreatedAt);

public sealed record PaymentDto(int Id, PaymentProvider Provider, PaymentStatus Status, decimal Amount, string? ProviderTransactionId,
    string? FailureReason, DateTime CreatedAt);

public sealed record AdminOrderDetailDto(OrderDetailDto Order, string CustomerId, string CustomerName, string CustomerEmail,
    string? CustomerPhone, IReadOnlyList<PaymentDto> Payments, IReadOnlyList<OrderStatus> AllowedNextStatuses);

public sealed class CheckoutPreviewRequestValidator : AbstractValidator<CheckoutPreviewRequest>
{
    public CheckoutPreviewRequestValidator()
    {
        RuleFor(x => x.AddressId).GreaterThan(0);
        RuleFor(x => x.CouponCode).MaximumLength(50);
    }
}

public sealed class PlaceOrderRequestValidator : AbstractValidator<PlaceOrderRequest>
{
    public PlaceOrderRequestValidator()
    {
        RuleFor(x => x.AddressId).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.CouponCode).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator() => RuleFor(x => x.Reason).MaximumLength(500);
}

public sealed class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x.TrackingNumber).MaximumLength(100);
    }
}
