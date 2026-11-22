using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Admin.Models;

public sealed class OrderFilter
{
    public OrderStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class OrderListViewModel
{
    public required PagedResult<AdminOrderListItemDto> Result { get; init; }
    public required OrderFilter Filter { get; init; }
}

public sealed class StatusChangeForm
{
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public string? TrackingNumber { get; set; }

    public UpdateOrderStatusRequest ToRequest() => new(Status,
        string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(), string.IsNullOrWhiteSpace(TrackingNumber) ? null : TrackingNumber.Trim());
}

public sealed class OrderDetailsViewModel
{
    public required AdminOrderDetailDto Detail { get; init; }
    public StatusChangeForm Form { get; set; } = new();
}
