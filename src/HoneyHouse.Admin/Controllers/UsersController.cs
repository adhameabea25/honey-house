using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Models;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

[Authorize(Policy = AdminServiceExtensions.AdminPolicy)]
public sealed class UsersController(IUserAdminService users) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, string? role, int page = 1, CancellationToken ct = default)
    {
        var result = await users.SearchAsync(new UserQuery(search, role, page, 25), ct);
        return View(new UserListViewModel { Result = result, Search = search, Role = role });
    }

    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var user = await users.GetAsync(id, ct);
        return user.IsFailure ? NotFound() : View(new UserDetailsViewModel { User = user.Value, IsSelf = id == User.GetUserId() });
    }

    public IActionResult Create() => View(new StaffForm());

    [HttpPost]
    public async Task<IActionResult> Create(StaffForm form, CancellationToken ct)
    {
        var request = form.ToRequest();
        if (!await ValidateAsync(request)) return View(form);
        var result = await users.CreateStaffAsync(request, ct);
        if (result.IsFailure) { AddErrors(result.Errors); return View(form); }
        TempData[FlashSuccess] = T["staff_created", result.Value.FullName];
        return RedirectToAction(nameof(Details), new { id = result.Value.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Roles(string id, List<string> roles, CancellationToken ct)
    {
        var request = new UpdateRolesRequest(roles);
        if (!await ValidateAsync(request))
        {
            TempData[FlashError] = T["roles_required"];
            return RedirectToAction(nameof(Details), new { id });
        }
        var result = await users.UpdateRolesAsync(User.GetUserId(), id, request, ct);
        if (result.IsFailure)
        {
            if (result.Errors[0].Type == ErrorType.NotFound) return NotFound();
            Failure(result.Errors[0]);
        }
        else Success();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Status(string id, bool isActive, CancellationToken ct)
    {
        var result = await users.UpdateStatusAsync(User.GetUserId(), id, new UpdateUserStatusRequest(isActive), ct);
        if (result.IsFailure)
        {
            if (result.Errors[0].Type == ErrorType.NotFound) return NotFound();
            Failure(result.Errors[0]);
        }
        else Success(isActive ? "user_enabled" : "user_disabled");
        return RedirectToAction(nameof(Details), new { id });
    }
}
