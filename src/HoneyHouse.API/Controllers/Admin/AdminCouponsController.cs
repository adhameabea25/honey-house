using HoneyHouse.API.Common;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Coupons;
using HoneyHouse.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers.Admin;

/// <summary>Coupon management (Admin).</summary>
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/coupons")]
public class AdminCouponsController(ICouponService coupons) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminCouponDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] CouponQuery query, CancellationToken ct) => Ok(await coupons.SearchAsync(query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminCouponDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => FromResult(await coupons.GetAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<AdminCouponDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CouponUpsertRequest request, CancellationToken ct) =>
        CreatedFromResult(await coupons.CreateAsync(request, ct), nameof(Get), c => new { id = c.Id });

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminCouponDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, CouponUpsertRequest request, CancellationToken ct) =>
        FromResult(await coupons.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) => FromResult(await coupons.DeleteAsync(id, ct));
}
