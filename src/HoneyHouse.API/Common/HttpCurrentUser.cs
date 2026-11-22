using System.Security.Claims;
using HoneyHouse.Application.Interfaces.Services;

namespace HoneyHouse.API.Common;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id of the authenticated user. Only call from [Authorize] endpoints.</summary>
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
        ?? throw new InvalidOperationException("No authenticated user.");
}
