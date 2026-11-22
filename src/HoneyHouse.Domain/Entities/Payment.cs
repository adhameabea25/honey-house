using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Domain.Entities;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public PaymentProvider Provider { get; set; }
    public string? ProviderOrderId { get; set; }
    public string? ProviderTransactionId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? FailureReason { get; set; }
    public string? RawPayload { get; set; }
}
