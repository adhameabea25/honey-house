using HoneyHouse.API.Common;
using HoneyHouse.Application.Features.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers;

/// <summary>The signed-in customer's shopping cart (synced across devices).</summary>
[Authorize]
[Route("api/v1/cart")]
public class CartController(ICartService cart) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await cart.GetAsync(User.GetUserId(), ct));

    /// <summary>Add a weight option to the cart (quantities are merged).</summary>
    [HttpPost("items")]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddItem(AddCartItemRequest request, CancellationToken ct) =>
        FromResult(await cart.AddItemAsync(User.GetUserId(), request, ct));

    [HttpPut("items/{itemId:int}")]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(int itemId, UpdateCartItemRequest request, CancellationToken ct) =>
        FromResult(await cart.UpdateItemAsync(User.GetUserId(), itemId, request, ct));

    [HttpDelete("items/{itemId:int}")]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(int itemId, CancellationToken ct) =>
        FromResult(await cart.RemoveItemAsync(User.GetUserId(), itemId, ct));

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await cart.ClearAsync(User.GetUserId(), ct);
        return NoContent();
    }
}
