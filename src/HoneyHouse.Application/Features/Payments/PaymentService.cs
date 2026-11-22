using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HoneyHouse.Application.Features.Payments;

public sealed record PaymentSessionDto(int OrderId, string OrderNumber, string PaymentUrl, DateTime PaymentDueAt);

public interface IPaymentService
{
    Task<Result<PaymentSessionDto>> RetryAsync(string userId, int orderId, CancellationToken ct = default);
    /// <summary>Returns false when the notification signature is invalid.</summary>
    Task<bool> HandleWebhookAsync(string rawBody, string? signature, CancellationToken ct = default);
    Task<int> ExpireUnpaidOrdersAsync(CancellationToken ct = default);
}

/// <summary>Online payment through the configured gateway (Paymob).</summary>
public sealed class OnlinePaymentHandler(IPaymentGateway gateway, IUnitOfWork uow, ILogger<OnlinePaymentHandler> logger) : IOrderPaymentHandler
{
    public PaymentMethod Method => PaymentMethod.Online;

    public async Task<Result<string?>> InitiateAsync(Order order, CancellationToken ct = default)
    {
        if (!gateway.IsEnabled) return Error.Business("online_payment_unavailable");
        var user = await uow.Repository<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == order.UserId, ct);
        try
        {
            var session = await gateway.CreateSessionAsync(order,
                new PaymentCustomer(order.ShipRecipientName, user?.Email ?? string.Empty, order.ShipPhone), ct);
            order.Payments.Add(new Payment
            {
                Provider = PaymentProvider.Paymob, ProviderOrderId = session.ProviderOrderId, Amount = order.Total, Status = PaymentStatus.Pending
            });
            return session.PaymentUrl;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Payment gateway failed for order {OrderNumber}", order.OrderNumber);
            return Error.Business("payment_gateway_error");
        }
    }
}

public sealed class PaymentService(
    IPaymentGateway gateway,
    IOrderRepository orders,
    IOrderService orderService,
    IUnitOfWork uow,
    TimeProvider clock,
    ILogger<PaymentService> logger) : IPaymentService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<Result<PaymentSessionDto>> RetryAsync(string userId, int orderId, CancellationToken ct = default)
    {
        var order = await orders.GetDetailsAsync(orderId, ct);
        if (order is null || order.UserId != userId) return Error.NotFound("order_not_found");
        if (order.PaymentMethod != PaymentMethod.Online || order.PaymentStatus == PaymentStatus.Paid || order.Status != OrderStatus.Pending)
            return Error.Business("payment_not_retryable");
        if (!gateway.IsEnabled) return Error.Business("online_payment_unavailable");

        PaymentSession session;
        try
        {
            session = await gateway.CreateSessionAsync(order, new PaymentCustomer(order.ShipRecipientName, order.User.Email ?? "", order.ShipPhone), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Payment retry failed for order {OrderNumber}", order.OrderNumber);
            return Error.Business("payment_gateway_error");
        }

        foreach (var old in order.Payments.Where(p => p.Status == PaymentStatus.Pending)) old.Status = PaymentStatus.Failed;
        order.Payments.Add(new Payment { Provider = PaymentProvider.Paymob, ProviderOrderId = session.ProviderOrderId, Amount = order.Total });
        order.PaymentStatus = PaymentStatus.Pending;
        order.PaymentDueAt = Now.Add(OrderService.OnlinePaymentWindow);
        await uow.SaveChangesAsync(ct);
        return new PaymentSessionDto(order.Id, order.OrderNumber, session.PaymentUrl, order.PaymentDueAt.Value);
    }

    public async Task<bool> HandleWebhookAsync(string rawBody, string? signature, CancellationToken ct = default)
    {
        var n = gateway.ParseWebhook(rawBody, signature);
        if (n is null) return false;

        var payment = n.ProviderOrderId is null ? null
            : await uow.Repository<Payment>().FirstOrDefaultAsync(p => p.ProviderOrderId == n.ProviderOrderId, ct);
        var order = payment is not null
            ? await orders.GetDetailsAsync(payment.OrderId, ct)
            : n.MerchantReference is null ? null : await orders.GetDetailsByNumberAsync(n.MerchantReference, ct);

        if (order is null)
        {
            logger.LogWarning("Payment notification for unknown order {ProviderOrderId}/{Reference}", n.ProviderOrderId, n.MerchantReference);
            return true; // acknowledged; nothing to do
        }
        payment ??= order.Payments.Where(p => p.Provider == PaymentProvider.Paymob).OrderByDescending(p => p.Id).FirstOrDefault();
        if (payment is null)
        {
            payment = new Payment { Provider = PaymentProvider.Paymob, ProviderOrderId = n.ProviderOrderId, Amount = n.Amount };
            order.Payments.Add(payment);
        }
        else payment = order.Payments.First(p => p.Id == payment.Id);

        // Idempotent: a payment that already succeeded is never downgraded.
        if (payment.Status == PaymentStatus.Paid || n.Pending)
        {
            await uow.SaveChangesAsync(ct);
            return true;
        }

        payment.ProviderTransactionId = n.TransactionId;
        payment.RawPayload = n.RawPayload.Length > 8000 ? n.RawPayload[..8000] : n.RawPayload;

        if (n.Success && n.Amount >= order.Total)
        {
            payment.Status = PaymentStatus.Paid;
            payment.FailureReason = null;
            if (order.Status == OrderStatus.Cancelled)
            {
                // Paid after the order expired: keep the money recorded so an admin can refund it.
                order.PaymentStatus = PaymentStatus.Paid;
                order.StatusHistory.Add(new OrderStatusHistory { FromStatus = order.Status, ToStatus = order.Status, Note = "Online payment received after cancellation [refund required]" });
                logger.LogWarning("Payment received for cancelled order {OrderNumber}", order.OrderNumber);
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Paid;
                order.PaymentDueAt = null;
                if (OrderStateMachine.CanTransition(order.Status, OrderStatus.Confirmed) && order.Status == OrderStatus.Pending)
                {
                    order.StatusHistory.Add(new OrderStatusHistory { FromStatus = order.Status, ToStatus = OrderStatus.Confirmed, Note = "Paid online" });
                    order.Status = OrderStatus.Confirmed;
                }
            }
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = n.Success ? $"Amount mismatch: {n.Amount} < {order.Total}" : n.FailureReason ?? "Declined";
            if (order.PaymentStatus != PaymentStatus.Paid) order.PaymentStatus = PaymentStatus.Failed;
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Payment {Status} for order {OrderNumber} (tx {TransactionId})", payment.Status, order.OrderNumber, n.TransactionId);
        return true;
    }

    public async Task<int> ExpireUnpaidOrdersAsync(CancellationToken ct = default)
    {
        var now = Now;
        var expired = await uow.Repository<Order>().ListAsync(o =>
            o.PaymentMethod == PaymentMethod.Online && o.Status == OrderStatus.Pending && o.PaymentStatus != PaymentStatus.Paid
            && o.PaymentDueAt != null && o.PaymentDueAt < now, ct);

        var count = 0;
        foreach (var stub in expired)
        {
            var order = await orders.GetDetailsAsync(stub.Id, ct);
            if (order is null) continue;
            var result = await uow.ExecuteInTransactionAsync(t => orderService.CancelInternalAsync(order, "Online payment not completed in time", null, t), r => r.IsSuccess, ct);
            if (result.IsSuccess) count++;
        }
        if (count > 0) logger.LogInformation("Expired {Count} unpaid online orders", count);
        return count;
    }
}
