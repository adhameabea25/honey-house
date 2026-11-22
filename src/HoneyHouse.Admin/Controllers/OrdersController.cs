using System.Globalization;
using System.Text;
using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Models;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

public sealed class OrdersController(IOrderService orders) : AdminControllerBase
{
    private const int PageSize = 25;
    private const int ExportLimit = 5000;

    public async Task<IActionResult> Index(OrderFilter filter, CancellationToken ct)
    {
        var result = await orders.SearchAsync(ToQuery(filter, filter.Page, PageSize), ct);
        return View(new OrderListViewModel { Result = result, Filter = filter });
    }

    /// <summary>Downloads the filtered orders as CSV (UTF-8 with BOM so Excel shows Arabic correctly).</summary>
    public async Task<IActionResult> Export(OrderFilter filter, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", new[] { "order_number", "date", "customer", "email", "phone", "governorate", "status",
            "payment_method", "payment_status", "items", "total" }.Select(k => Csv(T[$"csv_{k}"]))));

        var page = 1;
        var written = 0;
        while (written < ExportLimit)
        {
            var batch = await orders.SearchAsync(ToQuery(filter, page++, PagedRequest.MaxPageSize), ct);
            foreach (var o in batch.Items)
            {
                sb.AppendLine(string.Join(",",
                    Csv(o.OrderNumber), Csv(Fmt.DateTime(o.CreatedAt)), Csv(o.CustomerName), Csv(o.CustomerEmail), Csv(o.CustomerPhone),
                    Csv(o.Governorate), Csv(T[$"status_{o.Status}"]), Csv(T[$"method_{o.PaymentMethod}"]), Csv(T[$"payment_{o.PaymentStatus}"]),
                    o.ItemCount.ToString(CultureInfo.InvariantCulture), o.Total.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            written += batch.Items.Count;
            if (!batch.HasNext) break;
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"orders-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var detail = await orders.GetForAdminAsync(id, ct);
        if (detail.IsFailure) return NotFound();
        return View(new OrderDetailsViewModel
        {
            Detail = detail.Value,
            Form = new StatusChangeForm { Status = detail.Value.AllowedNextStatuses.FirstOrDefault(), TrackingNumber = detail.Value.Order.TrackingNumber }
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, [Bind(Prefix = "Form")] StatusChangeForm form, CancellationToken ct)
    {
        var request = form.ToRequest();
        if (await ValidateAsync(request, "Form"))
        {
            try
            {
                var result = await orders.UpdateStatusAsync(id, request, ct);
                if (result.IsSuccess)
                {
                    TempData[FlashSuccess] = T["order_status_changed", T[$"status_{form.Status}"]];
                    return RedirectToAction(nameof(Details), new { id });
                }
                if (result.Errors[0].Type == ErrorType.NotFound) return NotFound();
                AddErrors(result.Errors);
            }
            catch (ConcurrencyConflictException)
            {
                ModelState.AddModelError("", T["concurrency_conflict"]);
            }
        }

        var detail = await orders.GetForAdminAsync(id, ct);
        if (detail.IsFailure) return NotFound();
        return View(nameof(Details), new OrderDetailsViewModel { Detail = detail.Value, Form = form });
    }

    [HttpPost]
    public async Task<IActionResult> MarkPaid(int id, CancellationToken ct)
    {
        var result = await orders.MarkPaidAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.Errors[0].Type == ErrorType.NotFound) return NotFound();
            Failure(result.Errors[0]);
        }
        else Success("order_marked_paid");
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Printable invoice / packing slip.</summary>
    public async Task<IActionResult> Print(int id, CancellationToken ct)
    {
        var detail = await orders.GetForAdminAsync(id, ct);
        return detail.IsFailure ? NotFound() : View(detail.Value);
    }

    private static AdminOrderQuery ToQuery(OrderFilter f, int page, int pageSize) => new(
        f.Status, f.PaymentStatus, f.PaymentMethod,
        f.From is { } from ? Fmt.LocalDateToUtc(from) : null,
        f.To is { } to ? Fmt.LocalDateToUtc(to.AddDays(1)) : null,
        string.IsNullOrWhiteSpace(f.Search) ? null : f.Search.Trim(), page, pageSize);

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        // Neutralise spreadsheet formulas and quote everything
        if ("=+-@\t\r".Contains(value[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
