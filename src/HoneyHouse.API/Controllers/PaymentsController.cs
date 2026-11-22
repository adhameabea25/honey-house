using HoneyHouse.API.Common;
using HoneyHouse.Application.Features.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers;

[Route("api/v1/payments")]
public class PaymentsController(IPaymentService payments) : ApiControllerBase
{
    /// <summary>Start a new online payment attempt for an unpaid order (extends the 30-minute window).</summary>
    [Authorize]
    [HttpPost("{orderId:int}/retry")]
    [ProducesResponseType<PaymentSessionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Retry(int orderId, CancellationToken ct) =>
        FromResult(await payments.RetryAsync(User.GetUserId(), orderId, ct));

    /// <summary>Paymob transaction-processed callback. Verified with the HMAC query parameter.</summary>
    [AllowAnonymous]
    [HttpPost("paymob/webhook")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PaymobWebhook([FromQuery] string? hmac, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        return await payments.HandleWebhookAsync(body, hmac, ct) ? Ok() : Unauthorized();
    }
}
