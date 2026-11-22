using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Models;
using HoneyHouse.Application.Features.Reviews;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

public sealed class ReviewsController(IReviewService reviews) : AdminControllerBase
{
    /// <param name="status">pending | approved | all</param>
    public async Task<IActionResult> Index(string status = "pending", int page = 1, CancellationToken ct = default)
    {
        if (status is not ("pending" or "approved" or "all")) status = "pending";
        var result = await reviews.SearchAsync(new AdminReviewQuery(status, null, page, 25), ct);
        return View(new ReviewListViewModel { Result = result, Status = status });
    }

    [HttpPost]
    public async Task<IActionResult> Moderate(int id, bool approved, string? returnUrl, CancellationToken ct)
    {
        var result = await reviews.ModerateAsync(id, approved, ct);
        if (result.IsFailure) Failure(result.Errors[0]); else Success(approved ? "review_approved" : "review_hidden");
        return Back(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? returnUrl, CancellationToken ct)
    {
        var result = await reviews.AdminDeleteAsync(id, ct);
        if (result.IsFailure) Failure(result.Errors[0]); else Success("deleted");
        return Back(returnUrl);
    }

    private IActionResult Back(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
}
