using HoneyHouse.Application.Common;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Application.Features.Orders;

/// <summary>Starts payment for a newly placed order. Returns a URL the customer must open to pay, or null when nothing is needed now.</summary>
public interface IOrderPaymentHandler
{
    PaymentMethod Method { get; }
    Task<Result<string?>> InitiateAsync(Order order, CancellationToken ct = default);
}

/// <summary>Cash on delivery: a pending payment collected by the courier.</summary>
public sealed class CashOnDeliveryPaymentHandler : IOrderPaymentHandler
{
    public PaymentMethod Method => PaymentMethod.CashOnDelivery;

    public Task<Result<string?>> InitiateAsync(Order order, CancellationToken ct = default)
    {
        order.Payments.Add(new Payment { Provider = PaymentProvider.CashOnDelivery, Amount = order.Total, Status = PaymentStatus.Pending });
        return Task.FromResult(Result<string?>.Success(null));
    }
}
