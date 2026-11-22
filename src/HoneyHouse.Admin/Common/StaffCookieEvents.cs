using System.Security.Claims;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace HoneyHouse.Admin.Common;

/// <summary>
/// Re-checks the staff account on every request so that disabling a user, removing their
/// staff role or changing their password (security stamp) ends their dashboard session at once.
/// </summary>
public sealed class StaffCookieEvents(UserManager<ApplicationUser> users) : CookieAuthenticationEvents
{
    public const string SecurityStampClaim = "security_stamp";

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = id is null ? null : await users.FindByIdAsync(id);

        var valid = user is not null
                    && user.IsActive
                    && !await users.IsLockedOutAsync(user)
                    && user.SecurityStamp == context.Principal!.FindFirstValue(SecurityStampClaim);

        if (valid)
        {
            var roles = await users.GetRolesAsync(user!);
            valid = roles.Contains(Roles.Admin) || roles.Contains(Roles.Manager);
            // Keep role claims in sync (e.g. Admin demoted to Manager)
            var current = context.Principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).Order().ToArray();
            if (valid && !current.SequenceEqual(roles.Order()))
            {
                context.ReplacePrincipal(StaffPrincipal.Create(user!, roles));
                context.ShouldRenew = true;
            }
        }

        if (!valid)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}

public static class StaffPrincipal
{
    public static ClaimsPrincipal Create(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Email ?? user.UserName ?? user.Id),
            new("full_name", user.FullName),
            new(StaffCookieEvents.SecurityStampClaim, user.SecurityStamp ?? "")
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
