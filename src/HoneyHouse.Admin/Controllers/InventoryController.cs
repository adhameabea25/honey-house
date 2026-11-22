using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Models;
using HoneyHouse.Application.Features.Inventory;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Admin.Controllers;

public sealed class InventoryController(IInventoryService inventory, UserManager<ApplicationUser> users, IUnitOfWork uow) : AdminControllerBase
{
    private const int PageSize = 30;

    public async Task<IActionResult> Index(string? search, bool lowStockOnly, int page = 1, CancellationToken ct = default)
    {
        var result = await inventory.SearchAsync(new InventoryQuery(search, lowStockOnly, page, PageSize), ct);
        return View(new InventoryListViewModel { Result = result, Search = search, LowStockOnly = lowStockOnly });
    }

    public async Task<IActionResult> Variant(int id, int page = 1, CancellationToken ct = default)
    {
        var model = await LoadAsync(id, page, ct);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Variant(int id, [Bind(Prefix = "Form")] StockAdjustForm form, CancellationToken ct)
    {
        var request = form.ToRequest();
        if (await ValidateAsync(request, "Form"))
        {
            try
            {
                var result = await inventory.AdjustAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    Success("stock_updated");
                    return RedirectToAction(nameof(Variant), new { id });
                }
                if (result.Errors[0].Type == Application.Common.ErrorType.NotFound) return NotFound();
                AddErrors(result.Errors);
            }
            catch (ConcurrencyConflictException)
            {
                // A sale changed this stock at the same moment: show fresh numbers and ask to retry.
                ModelState.AddModelError("", T["concurrency_conflict"]);
            }
        }

        var model = await LoadAsync(id, 1, ct);
        if (model is null) return NotFound();
        model.Form = form;
        return View(model);
    }

    private async Task<InventoryVariantViewModel?> LoadAsync(int id, int page, CancellationToken ct)
    {
        var item = await inventory.GetAsync(id, ct);
        if (item.IsFailure) return null;
        var history = (await inventory.GetTransactionsAsync(id, page, 20, ct)).Value;

        var userIds = history.Items.Select(t => t.CreatedByUserId).OfType<string>().Distinct().ToList();
        var names = await users.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.FullName }).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var orderIds = history.Items.Select(t => t.OrderId).OfType<int>().Distinct().ToList();
        var orders = orderIds.Count == 0 ? [] : (await uow.Repository<Order>().ListAsync(o => orderIds.Contains(o.Id), ct))
            .ToDictionary(o => o.Id, o => o.OrderNumber);

        return new InventoryVariantViewModel { Item = item.Value, History = history, UserNames = names, OrderNumbers = orders };
    }
}
