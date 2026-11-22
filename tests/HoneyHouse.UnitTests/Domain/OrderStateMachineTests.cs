using HoneyHouse.Domain;
using HoneyHouse.Domain.Enums;
using Shouldly;

namespace HoneyHouse.UnitTests.Domain;

public class OrderStateMachineTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Processing)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Returned)]
    public void Allowed_transitions(OrderStatus from, OrderStatus to) =>
        OrderStateMachine.CanTransition(from, to).ShouldBeTrue();

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Pending)]
    public void Forbidden_transitions(OrderStatus from, OrderStatus to) =>
        OrderStateMachine.CanTransition(from, to).ShouldBeFalse();

    [Fact]
    public void Customer_cannot_cancel_after_shipping()
    {
        OrderStateMachine.CanCustomerCancel(OrderStatus.Processing).ShouldBeTrue();
        OrderStateMachine.CanCustomerCancel(OrderStatus.Shipped).ShouldBeFalse();
    }
}
