using System.Security.Claims;
using HoneyHouse.Application.Interfaces.Services;

namespace HoneyHouse.Admin.Common;

public sealed class AdminCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}

public static class StaffClaims
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("No signed-in user.");

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue("full_name") ?? user.Identity?.Name ?? "";
}
