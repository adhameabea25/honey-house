using HoneyHouse.API.Common;
using HoneyHouse.Application.Features.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers;

[Authorize]
[Route("api/v1/coupons")]
public class CouponsController(ICouponService coupons) : ApiControllerBase
{
    /// <summary>Check a coupon code against the current cart and preview the discount.</summary>
    [HttpPost("validate")]
    [ProducesResponseType<CouponValidationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Validate(ValidateCouponRequest request, CancellationToken ct) =>
        FromResult(await coupons.ValidateForCartAsync(User.GetUserId(), request, ct));
}
