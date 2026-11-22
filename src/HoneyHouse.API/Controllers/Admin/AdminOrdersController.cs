using HoneyHouse.API.Common;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Inventory;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers.Admin;

/// <summary>Order management (Admin, Manager).</summary>
[Authorize(Roles = Roles.AdminOrManager)]
[Route("api/v1/admin/orders")]
public class AdminOrdersController(IOrderService orders) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminOrderListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] AdminOrderQuery query, CancellationToken ct) => Ok(await orders.SearchAsync(query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminOrderDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => FromResult(await orders.GetForAdminAsync(id, ct));

    /// <summary>Move the order to the next status. Cancelling or returning restores stock.</summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType<AdminOrderDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct) =>
        FromResult(await orders.UpdateStatusAsync(id, request, ct));

    /// <summary>Record that a cash-on-delivery payment was collected.</summary>
    [HttpPost("{id:int}/mark-paid")]
    [ProducesResponseType<AdminOrderDetailDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken ct) => FromResult(await orders.MarkPaidAsync(id, ct));
}

/// <summary>Stock levels and audited adjustments (Admin, Manager).</summary>
[Authorize(Roles = Roles.AdminOrManager)]
[Route("api/v1/admin/inventory")]
public class AdminInventoryController(IInventoryService inventory) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<InventoryItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] InventoryQuery query, CancellationToken ct) => Ok(await inventory.SearchAsync(query, ct));

    /// <summary>Add (Restock) or correct (Adjustment, may be negative) the stock of a weight option.</summary>
    [HttpPost("{variantId:int}/adjust")]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Adjust(int variantId, AdjustStockRequest request, CancellationToken ct) =>
        FromResult(await inventory.AdjustAsync(variantId, request, ct));

    [HttpGet("{variantId:int}/transactions")]
    [ProducesResponseType<PagedResult<InventoryTransactionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Transactions(int variantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        FromResult(await inventory.GetTransactionsAsync(variantId, page, pageSize, ct));
}
