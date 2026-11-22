using HoneyHouse.API.Common;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers;

/// <summary>Checkout and the signed-in customer's orders.</summary>
[Authorize]
[Route("api/v1/orders")]
public class OrdersController(IOrderService orders) : ApiControllerBase
{
    /// <summary>Totals for the current cart with the chosen address and coupon (nothing is saved).</summary>
    [HttpPost("checkout/preview")]
    [ProducesResponseType<CheckoutSummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(CheckoutPreviewRequest request, CancellationToken ct) =>
        FromResult(await orders.PreviewAsync(User.GetUserId(), request, ct));

    /// <summary>Place an order from the cart. For online payment the response includes a paymentUrl.</summary>
    [HttpPost]
    [ProducesResponseType<OrderPlacedDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Place(PlaceOrderRequest request, CancellationToken ct)
    {
        var result = await orders.PlaceOrderAsync(User.GetUserId(), request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value.Order.Id }, result.Value) : Problem(result.Errors);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<OrderSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] OrderQuery query, CancellationToken ct) =>
        Ok(await orders.GetMyOrdersAsync(User.GetUserId(), query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => FromResult(await orders.GetMyOrderAsync(User.GetUserId(), id, ct));

    /// <summary>Status timeline of the order.</summary>
    [HttpGet("{id:int}/tracking")]
    [ProducesResponseType<IReadOnlyList<OrderStatusEntryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Tracking(int id, CancellationToken ct) => FromResult(await orders.GetTrackingAsync(User.GetUserId(), id, ct));

    /// <summary>Cancel an order that has not been shipped yet. Stock is returned.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<OrderDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(int id, CancelOrderRequest request, CancellationToken ct) =>
        FromResult(await orders.CancelMyOrderAsync(User.GetUserId(), id, request, ct));
}
