using System.Security.Cryptography;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Application.Features.Coupons;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Application.Localization;
using HoneyHouse.Domain;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HoneyHouse.Application.Features.Orders;

public interface IOrderService
{
    Task<Result<CheckoutSummaryDto>> PreviewAsync(string userId, CheckoutPreviewRequest request, CancellationToken ct = default);
    Task<Result<OrderPlacedDto>> PlaceOrderAsync(string userId, PlaceOrderRequest request, CancellationToken ct = default);
    Task<PagedResult<OrderSummaryDto>> GetMyOrdersAsync(string userId, OrderQuery query, CancellationToken ct = default);
    Task<Result<OrderDetailDto>> GetMyOrderAsync(string userId, int orderId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OrderStatusEntryDto>>> GetTrackingAsync(string userId, int orderId, CancellationToken ct = default);
    Task<Result<OrderDetailDto>> CancelMyOrderAsync(string userId, int orderId, CancelOrderRequest request, CancellationToken ct = default);

    Task<PagedResult<AdminOrderListItemDto>> SearchAsync(AdminOrderQuery query, CancellationToken ct = default);
    Task<Result<AdminOrderDetailDto>> GetForAdminAsync(int orderId, CancellationToken ct = default);
    Task<Result<AdminOrderDetailDto>> UpdateStatusAsync(int orderId, UpdateOrderStatusRequest request, CancellationToken ct = default);
    Task<Result<AdminOrderDetailDto>> MarkPaidAsync(int orderId, CancellationToken ct = default);

    /// <summary>Cancels an order and returns its stock (used by customers, admins and the unpaid-order expiry job).</summary>
    Task<Result> CancelInternalAsync(Order order, string? note, string? changedByUserId, CancellationToken ct = default);
}

public sealed class OrderService(
    ICartRepository carts,
    IAddressRepository addresses,
    IOrderRepository orders,
    ICouponService coupons,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILocalizer localizer,
    TimeProvider clock,
    IEnumerable<IOrderPaymentHandler> paymentHandlers,
    ILogger<OrderService> logger) : IOrderService
{
    public static readonly TimeSpan OnlinePaymentWindow = TimeSpan.FromMinutes(30);
    private readonly OrderMapper _map = new(localizer);
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private string Pick(string ar, string en) => localizer.Language == "en" ? en : ar;

    // ---------------- Checkout ----------------

    public async Task<Result<CheckoutSummaryDto>> PreviewAsync(string userId, CheckoutPreviewRequest r, CancellationToken ct = default)
    {
        var cart = await carts.GetWithItemsAsync(userId, ct);
        if (cart is null || cart.Items.Count == 0) return Error.Business("cart_empty");

        var address = await addresses.GetForUserAsync(userId, r.AddressId, ct);
        if (address is null) return Error.Validation("address_not_found", "addressId");

        var lines = cart.Items.OrderBy(i => i.Id).ToList();
        var problems = StockProblems(lines);
        var subtotal = lines.Where(i => CartService.IsSellable(i.Variant)).Sum(i => i.Variant.Price * i.Quantity);
        var shipping = address.Governorate.IsActive ? address.Governorate.ShippingFee : 0;

        decimal discount = 0;
        string? couponError = null, couponErrorCode = null;
        if (!string.IsNullOrWhiteSpace(r.CouponCode))
        {
            var eval = await coupons.EvaluateAsync(userId, r.CouponCode, subtotal, ct);
            if (eval.IsSuccess) discount = eval.Value.Discount;
            else
            {
                couponErrorCode = eval.FirstError!.Code;
                couponError = localizer[couponErrorCode, eval.FirstError.Args ?? []];
            }
        }

        string? blocking = !address.Governorate.IsActive ? localizer["governorate_not_found"]
            : problems.Count > 0 ? localizer["order_has_unavailable_items", string.Join(", ", problems)]
            : null;

        return new CheckoutSummaryDto(
            lines.Select(i => new CheckoutLineDto(i.VariantId, i.Variant.Product is { } p ? Pick(p.NameAr, p.NameEn) : i.Variant.Sku,
                Pick(i.Variant.LabelAr, i.Variant.LabelEn), i.Variant.Price, i.Quantity, i.Variant.Price * i.Quantity)).ToList(),
            subtotal, discount, shipping, subtotal - discount + shipping, "EGP",
            couponError is null ? r.CouponCode?.Trim().ToUpperInvariant() : null, couponError, couponErrorCode,
            blocking is null, blocking);
    }

    public Task<Result<OrderPlacedDto>> PlaceOrderAsync(string userId, PlaceOrderRequest r, CancellationToken ct = default) =>
        uow.ExecuteInTransactionAsync(token => PlaceOrderCoreAsync(userId, r, token), result => result.IsSuccess, ct);

    private async Task<Result<OrderPlacedDto>> PlaceOrderCoreAsync(string userId, PlaceOrderRequest r, CancellationToken ct)
    {
        var handler = paymentHandlers.FirstOrDefault(h => h.Method == r.PaymentMethod);
        if (handler is null) return Error.Validation("payment_method_invalid", "paymentMethod");

        var cart = await carts.GetWithItemsAsync(userId, ct);
        if (cart is null || cart.Items.Count == 0) return Error.Business("cart_empty");

        var address = await addresses.GetForUserAsync(userId, r.AddressId, ct);
        if (address is null) return Error.Validation("address_not_found", "addressId");
        if (!address.Governorate.IsActive) return Error.Validation("governorate_not_found", "addressId");

        var lines = cart.Items.OrderBy(i => i.Id).ToList();
        var problems = StockProblems(lines);
        if (problems.Count > 0) return Error.Business("order_has_unavailable_items", string.Join(", ", problems));

        var subtotal = lines.Sum(i => i.Variant.Price * i.Quantity);
        CouponEvaluation? coupon = null;
        if (!string.IsNullOrWhiteSpace(r.CouponCode))
        {
            var eval = await coupons.EvaluateAsync(userId, r.CouponCode, subtotal, ct);
            if (eval.IsFailure) return Result<OrderPlacedDto>.Failure(eval.Errors);
            coupon = eval.Value;
        }

        var discount = coupon?.Discount ?? 0;
        var shipping = address.Governorate.ShippingFee;
        var order = new Order
        {
            OrderNumber = NewOrderNumber(),
            UserId = userId,
            Status = OrderStatus.Pending,
            PaymentMethod = r.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            Subtotal = subtotal,
            DiscountAmount = discount,
            ShippingFee = shipping,
            Total = subtotal - discount + shipping,
            CouponId = coupon?.Coupon.Id,
            CouponCode = coupon?.Coupon.Code,
            ShipRecipientName = address.RecipientName,
            ShipPhone = address.Phone,
            ShipGovernorateId = address.GovernorateId,
            ShipGovernorateName = address.Governorate.NameAr,
            ShipCity = address.City,
            ShipAddressLine = FormatAddressLine(address),
            ShipLandmark = address.Landmark,
            CustomerNotes = r.Notes?.Trim(),
            PaymentDueAt = r.PaymentMethod == PaymentMethod.Online ? Now.Add(OnlinePaymentWindow) : null,
            Items = lines.Select(i => new OrderItem
            {
                VariantId = i.VariantId,
                ProductId = i.Variant.ProductId,
                ProductNameAr = i.Variant.Product.NameAr,
                ProductNameEn = i.Variant.Product.NameEn,
                VariantLabelAr = i.Variant.LabelAr,
                VariantLabelEn = i.Variant.LabelEn,
                Sku = i.Variant.Sku,
                UnitPrice = i.Variant.Price,
                Quantity = i.Quantity,
                LineTotal = i.Variant.Price * i.Quantity
            }).ToList(),
            StatusHistory = [new OrderStatusHistory { FromStatus = null, ToStatus = OrderStatus.Pending, ChangedByUserId = userId }]
        };
        await orders.AddAsync(order, ct);

        // Reserve stock. RowVersion on ProductVariant makes a concurrent sale of the same stock fail instead of overselling.
        foreach (var line in lines)
            line.Variant.StockQuantity -= line.Quantity;

        if (coupon is not null)
        {
            coupon.Coupon.UsedCount++;
            await uow.Repository<CouponUsage>().AddAsync(new CouponUsage
            {
                Coupon = coupon.Coupon, UserId = userId, Order = order, DiscountAmount = discount
            }, ct);
        }

        foreach (var item in lines) uow.Repository<CartItem>().Remove(item);
        await uow.SaveChangesAsync(ct);

        await uow.Repository<InventoryTransaction>().AddRangeAsync(lines.Select(l => new InventoryTransaction
        {
            VariantId = l.VariantId, QuantityChange = -l.Quantity, StockAfter = l.Variant.StockQuantity,
            Type = InventoryTransactionType.Sale, OrderId = order.Id, CreatedByUserId = userId
        }), ct);
        await uow.SaveChangesAsync(ct);

        var payment = await handler.InitiateAsync(order, ct);
        if (payment.IsFailure) return Result<OrderPlacedDto>.Failure(payment.Errors);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Order {OrderNumber} placed by {UserId}: {Total} EGP via {Method}", order.OrderNumber, userId, order.Total, order.PaymentMethod);
        return new OrderPlacedDto(_map.ToDetail(order), payment.Value);
    }

    // ---------------- Customer orders ----------------

    public async Task<PagedResult<OrderSummaryDto>> GetMyOrdersAsync(string userId, OrderQuery query, CancellationToken ct = default)
    {
        var paging = new PagedRequest { Page = query.Page, PageSize = query.PageSize };
        var (items, total) = await orders.SearchAsync(new OrderSearchCriteria(userId, query.Status, null, null, null, null, null, paging.Skip, paging.PageSize), ct);
        return new PagedResult<OrderSummaryDto>(items.Select(_map.ToSummary).ToList(), paging.Page, paging.PageSize, total);
    }

    public async Task<Result<OrderDetailDto>> GetMyOrderAsync(string userId, int orderId, CancellationToken ct = default)
    {
        var order = await orders.GetDetailsAsync(orderId, ct);
        return order is null || order.UserId != userId ? Error.NotFound("order_not_found") : _map.ToDetail(order);
    }

    public async Task<Result<IReadOnlyList<OrderStatusEntryDto>>> GetTrackingAsync(string userId, int orderId, CancellationToken ct = default)
    {
        var order = await GetMyOrderAsync(userId, orderId, ct);
        return order.IsSuccess ? Result<IReadOnlyList<OrderStatusEntryDto>>.Success(order.Value.History)
            : Result<IReadOnlyList<OrderStatusEntryDto>>.Failure(order.Errors);
    }

    public async Task<Result<OrderDetailDto>> CancelMyOrderAsync(string userId, int orderId, CancelOrderRequest r, CancellationToken ct = default)
    {
        var order = await orders.GetDetailsAsync(orderId, ct);
        if (order is null || order.UserId != userId) return Error.NotFound("order_not_found");
        if (!OrderStateMachine.CanCustomerCancel(order.Status)) return Error.Business("order_cannot_cancel");

        var result = await uow.ExecuteInTransactionAsync(t => CancelInternalAsync(order, r.Reason ?? "Cancelled by customer", userId, t), x => x.IsSuccess, ct);
        return result.IsSuccess ? _map.ToDetail(order) : Result<OrderDetailDto>.Failure(result.Errors);
    }

    public async Task<Result> CancelInternalAsync(Order order, string? note, string? changedByUserId, CancellationToken ct = default)
    {
        if (!OrderStateMachine.CanTransition(order.Status, OrderStatus.Cancelled))
            return Error.Business("order_invalid_transition", _map.StatusName(order.Status), _map.StatusName(OrderStatus.Cancelled));

        await ReturnStockAsync(order, InventoryTransactionType.Cancellation, changedByUserId, ct);
        await ReleaseCouponAsync(order, ct);

        var paidNote = order.PaymentStatus == PaymentStatus.Paid ? " [refund required]" : string.Empty;
        AddHistory(order, OrderStatus.Cancelled, (note ?? string.Empty) + paidNote, changedByUserId);
        order.CancelledAt = Now;
        if (order.PaymentStatus == PaymentStatus.Pending) order.PaymentStatus = PaymentStatus.Failed;
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---------------- Admin ----------------

    public async Task<PagedResult<AdminOrderListItemDto>> SearchAsync(AdminOrderQuery q, CancellationToken ct = default)
    {
        var paging = new PagedRequest { Page = q.Page, PageSize = q.PageSize };
        var (items, total) = await orders.SearchAsync(new OrderSearchCriteria(null, q.Status, q.PaymentStatus, q.PaymentMethod,
            q.From, q.To, q.Search?.Trim(), paging.Skip, paging.PageSize), ct);
        return new PagedResult<AdminOrderListItemDto>(items.Select(o => new AdminOrderListItemDto(
            o.Id, o.OrderNumber, o.User.FullName, o.User.Email!, o.User.PhoneNumber, o.Status, o.PaymentMethod, o.PaymentStatus,
            o.Total, o.Items.Sum(i => i.Quantity), o.ShipGovernorateName, o.CreatedAt)).ToList(), paging.Page, paging.PageSize, total);
    }

    public async Task<Result<AdminOrderDetailDto>> GetForAdminAsync(int orderId, CancellationToken ct = default)
    {
        var order = await orders.GetDetailsAsync(orderId, ct);
        return order is null ? Error.NotFound("order_not_found") : ToAdminDetail(order);
    }

    public async Task<Result<AdminOrderDetailDto>> UpdateStatusAsync(int orderId, UpdateOrderStatusRequest r, CancellationToken ct = default)
    {
        var order = await orders.GetDetailsAsync(orderId, ct);
        if (order is null) return Error.NotFound("order_not_found");
        if (!OrderStateMachine.CanTransition(order.Status, r.Status))
            return Error.Business("order_invalid_transition", _map.StatusName(order.Status), _map.StatusName(r.Status));

        var result = await uow.ExecuteInTransactionAsync(async t =>
        {
            if (r.Status == OrderStatus.Cancelled)
                return await CancelInternalAsync(order, r.Note, currentUser.UserId, t);

            if (r.Status == OrderStatus.Returned)
                await ReturnStockAsync(order, InventoryTransactionType.Return, currentUser.UserId, t);

            if (r.Status == OrderStatus.Delivered)
            {
                order.DeliveredAt = Now;
                // Cash on delivery is collected by the courier on delivery.
                if (order.PaymentMethod == PaymentMethod.CashOnDelivery && order.PaymentStatus == PaymentStatus.Pending)
                    await MarkCodPaidAsync(order, t);
            }

            if (!string.IsNullOrWhiteSpace(r.TrackingNumber)) order.TrackingNumber = r.TrackingNumber.Trim();
            AddHistory(order, r.Status, r.Note, currentUser.UserId);
            await uow.SaveChangesAsync(t);
            return Result.Success();
        }, x => x.IsSuccess, ct);

        return result.IsSuccess ? ToAdminDetail(order) : Result<AdminOrderDetailDto>.Failure(result.Errors);
    }

    public async Task<Result<AdminOrderDetailDto>> MarkPaidAsync(int orderId, CancellationToken ct = default)
    {
        var order = await orders.GetDetailsAsync(orderId, ct);
        if (order is null) return Error.NotFound("order_not_found");
        if (order.PaymentStatus == PaymentStatus.Paid) return Error.Conflict("order_already_paid");
        if (order.Status == OrderStatus.Cancelled)
            return Error.Business("order_invalid_transition", _map.StatusName(order.Status), localizer["payment_Paid"]);

        await MarkCodPaidAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        return ToAdminDetail(order);
    }

    // ---------------- Helpers ----------------

    private Task MarkCodPaidAsync(Order order, CancellationToken ct)
    {
        order.PaymentStatus = PaymentStatus.Paid;
        var payment = order.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Pending);
        if (payment is not null) payment.Status = PaymentStatus.Paid;
        else order.Payments.Add(new Payment { Provider = order.PaymentMethod == PaymentMethod.CashOnDelivery ? PaymentProvider.CashOnDelivery : PaymentProvider.Paymob,
            Amount = order.Total, Status = PaymentStatus.Paid });
        return Task.CompletedTask;
    }

    private async Task ReturnStockAsync(Order order, InventoryTransactionType type, string? userId, CancellationToken ct)
    {
        var variants = uow.Repository<ProductVariant>();
        foreach (var item in order.Items)
        {
            var variant = await variants.GetByIdAsync(item.VariantId, ct);
            if (variant is null) continue;
            variant.StockQuantity += item.Quantity;
            await uow.Repository<InventoryTransaction>().AddAsync(new InventoryTransaction
            {
                VariantId = variant.Id, QuantityChange = item.Quantity, StockAfter = variant.StockQuantity,
                Type = type, OrderId = order.Id, CreatedByUserId = userId
            }, ct);
        }
    }

    private async Task ReleaseCouponAsync(Order order, CancellationToken ct)
    {
        if (order.CouponId is null) return;
        var usage = await uow.Repository<CouponUsage>().FirstOrDefaultAsync(u => u.OrderId == order.Id, ct);
        if (usage is not null) uow.Repository<CouponUsage>().Remove(usage);
        var coupon = await uow.Repository<Coupon>().GetByIdAsync(order.CouponId.Value, ct);
        if (coupon is { UsedCount: > 0 }) coupon.UsedCount--;
    }

    private void AddHistory(Order order, OrderStatus to, string? note, string? userId)
    {
        order.StatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id, FromStatus = order.Status, ToStatus = to, Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            ChangedByUserId = userId, CreatedAt = Now
        });
        order.Status = to;
    }

    private List<string> StockProblems(IEnumerable<CartItem> lines) =>
        lines.Where(i => !CartService.IsSellable(i.Variant) || i.Variant.StockQuantity < i.Quantity)
            .Select(i => i.Variant.Product is { } p ? $"{Pick(p.NameAr, p.NameEn)} ({Pick(i.Variant.LabelAr, i.Variant.LabelEn)})" : i.Variant.Sku)
            .ToList();

    private static string FormatAddressLine(Address a) =>
        string.Join("، ", new[]
        {
            a.Street,
            a.Building is null ? null : $"عمارة {a.Building}",
            a.Floor is null ? null : $"الدور {a.Floor}",
            a.Apartment is null ? null : $"شقة {a.Apartment}",
            a.Area
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private string NewOrderNumber()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var suffix = string.Concat(Enumerable.Range(0, 6).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]));
        return $"HH-{Now:yyMMdd}-{suffix}";
    }

    private AdminOrderDetailDto ToAdminDetail(Order o) => new(
        _map.ToDetail(o), o.UserId, o.User?.FullName ?? string.Empty, o.User?.Email ?? string.Empty, o.User?.PhoneNumber,
        o.Payments.OrderBy(p => p.Id).Select(p => new PaymentDto(p.Id, p.Provider, p.Status, p.Amount, p.ProviderTransactionId, p.FailureReason, p.CreatedAt)).ToList(),
        Enum.GetValues<OrderStatus>().Where(s => OrderStateMachine.CanTransition(o.Status, s)).ToList());
}
