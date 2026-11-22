using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Models;
using HoneyHouse.Application.Features.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

[Authorize(Policy = AdminServiceExtensions.AdminPolicy)]
public sealed class CouponsController(ICouponService coupons, TimeProvider clock) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, bool? isActive, int page = 1, CancellationToken ct = default)
    {
        var result = await coupons.SearchAsync(new CouponQuery(search, isActive, page, 25), ct);
        return View(new CouponListViewModel { Result = result, Search = search, IsActive = isActive, NowUtc = clock.GetUtcNow().UtcDateTime });
    }

    public IActionResult Create() => View("Form", new CouponForm());

    [HttpPost]
    public async Task<IActionResult> Create(CouponForm form, CancellationToken ct)
    {
        var request = form.ToRequest();
        if (!await ValidateAsync(request)) return View("Form", form);
        var result = await coupons.CreateAsync(request, ct);
        if (result.IsFailure) { AddErrors(result.Errors); return View("Form", form); }
        Success();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var coupon = await coupons.GetAsync(id, ct);
        return coupon.IsFailure ? NotFound() : View("Form", CouponForm.From(coupon.Value));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CouponForm form, CancellationToken ct)
    {
        var existing = await coupons.GetAsync(id, ct);
        if (existing.IsFailure) return NotFound();
        form.Id = id;
        form.UsedCount = existing.Value.UsedCount;

        var request = form.ToRequest();
        if (!await ValidateAsync(request)) return View("Form", form);
        var result = await coupons.UpdateAsync(id, request, ct);
        if (result.IsFailure) { AddErrors(result.Errors); return View("Form", form); }
        Success();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await coupons.DeleteAsync(id, ct);
        if (result.IsFailure) Failure(result.Errors[0]); else Success("deleted");
        return RedirectToAction(nameof(Index));
    }
}
