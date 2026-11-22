using HoneyHouse.API.Common;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Analytics;
using HoneyHouse.Application.Features.Users;
using HoneyHouse.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers.Admin;

/// <summary>Customer and staff accounts (Admin).</summary>
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/users")]
public class AdminUsersController(IUserAdminService users) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminUserDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] UserQuery query, CancellationToken ct) => Ok(await users.SearchAsync(query, ct));

    [HttpGet("{id}")]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string id, CancellationToken ct) => FromResult(await users.GetAsync(id, ct));

    /// <summary>Create a staff account (e.g. Manager).</summary>
    [HttpPost]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStaff(CreateStaffRequest request, CancellationToken ct) =>
        CreatedFromResult(await users.CreateStaffAsync(request, ct), nameof(Get), u => new { id = u.Id });

    [HttpPut("{id}/roles")]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRoles(string id, UpdateRolesRequest request, CancellationToken ct) =>
        FromResult(await users.UpdateRolesAsync(User.GetUserId(), id, request, ct));

    /// <summary>Enable or disable an account. Disabling signs the user out everywhere.</summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType<AdminUserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(string id, UpdateUserStatusRequest request, CancellationToken ct) =>
        FromResult(await users.UpdateStatusAsync(User.GetUserId(), id, request, ct));
}

/// <summary>Sales analytics for the dashboard (Admin). Dates are UTC; daily/monthly buckets use Cairo local time.</summary>
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/analytics")]
public class AdminAnalyticsController(IAnalyticsService analytics) : ApiControllerBase
{
    /// <summary>Revenue, orders, average order value, items sold, new customers, etc. Defaults to the last 30 days.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<SalesSummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary([FromQuery] AnalyticsQuery query, CancellationToken ct) => FromResult(await analytics.SummaryAsync(query, ct));

    /// <summary>Revenue over time. groupBy: day (default) | month.</summary>
    [HttpGet("sales")]
    [ProducesResponseType<IReadOnlyList<SalesPointDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Sales([FromQuery] AnalyticsQuery query, [FromQuery] string? groupBy, CancellationToken ct) =>
        FromResult(await analytics.SalesAsync(query, groupBy, ct));

    [HttpGet("top-products")]
    [ProducesResponseType<IReadOnlyList<TopProductDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TopProducts([FromQuery] AnalyticsQuery query, [FromQuery] int limit = 10, CancellationToken ct = default) =>
        FromResult(await analytics.TopProductsAsync(query, limit, ct));

    [HttpGet("orders-by-status")]
    [ProducesResponseType<IReadOnlyList<StatusCountDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> OrdersByStatus(CancellationToken ct) => Ok(await analytics.OrdersByStatusAsync(ct));

    [HttpGet("sales-by-governorate")]
    [ProducesResponseType<IReadOnlyList<GovernorateSalesDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SalesByGovernorate([FromQuery] AnalyticsQuery query, CancellationToken ct) =>
        FromResult(await analytics.SalesByGovernorateAsync(query, ct));
}
