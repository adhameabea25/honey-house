using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Models;
using HoneyHouse.Application.Features.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

[Authorize(Policy = AdminServiceExtensions.AdminPolicy)]
public sealed class GovernoratesController(IAddressService addresses) : AdminControllerBase
{
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new GovernoratesViewModel { Items = await addresses.GetGovernoratesForAdminAsync(ct) });

    [HttpPost]
    public async Task<IActionResult> Update(int id, GovernorateRowForm form, CancellationToken ct)
    {
        var request = new UpdateGovernorateRequest(form.ShippingFee, form.IsActive);
        if (!ModelState.IsValid || !await ValidateAsync(request))
        {
            TempData[FlashError] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct());
            return RedirectToAction(nameof(Index));
        }
        var result = await addresses.UpdateGovernorateAsync(id, request, ct);
        if (result.IsFailure)
        {
            if (result.Errors[0].Type == Application.Common.ErrorType.NotFound) return NotFound();
            Failure(result.Errors[0]);
        }
        else TempData[FlashSuccess] = T["governorate_saved", T.IsArabic ? result.Value.NameAr : result.Value.NameEn];
        return Redirect(Url.Action(nameof(Index))! + $"#gov-{id}");
    }
}
