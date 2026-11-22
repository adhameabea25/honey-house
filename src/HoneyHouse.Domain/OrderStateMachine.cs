using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Domain;

/// <summary>Allowed order status transitions.</summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.OutForDelivery, OrderStatus.Delivered, OrderStatus.Returned],
        [OrderStatus.OutForDelivery] = [OrderStatus.Delivered, OrderStatus.Returned],
        [OrderStatus.Delivered] = [OrderStatus.Returned],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Returned] = []
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>Customers may cancel only before the order is shipped.</summary>
    public static bool CanCustomerCancel(OrderStatus status) =>
        status is OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Processing;
}
