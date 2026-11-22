using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Interfaces.Services;

public sealed record PaymentCustomer(string FullName, string Email, string Phone);

public sealed record PaymentSession(string ProviderOrderId, string PaymentUrl);

/// <summary>A verified, provider-neutral payment notification.</summary>
public sealed record PaymentNotification(
    string? ProviderOrderId, string? MerchantReference, string TransactionId, bool Success, bool Pending,
    decimal Amount, string Currency, string? FailureReason, string RawPayload);

public interface IPaymentGateway
{
    bool IsEnabled { get; }
    Task<PaymentSession> CreateSessionAsync(Order order, PaymentCustomer customer, CancellationToken ct = default);
    /// <summary>Returns null when the signature is invalid or the payload is not a transaction.</summary>
    PaymentNotification? ParseWebhook(string rawBody, string? signature);
}
