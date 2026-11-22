using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Localization;
using HoneyHouse.Admin.Models;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

[AllowAnonymous]
public sealed class AccountController(UserManager<ApplicationUser> users, IText t, ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect(SafeReturnUrl(returnUrl));
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Email)) ModelState.AddModelError(nameof(model.Email), t["required"]);
        if (string.IsNullOrEmpty(model.Password)) ModelState.AddModelError(nameof(model.Password), t["required"]);
        if (!ModelState.IsValid) return View(model);

        var user = await users.FindByEmailAsync(model.Email.Trim());
        string? error = null;

        if (user is null) error = "invalid_credentials";
        else if (!user.IsActive) error = "account_disabled";
        else if (await users.IsLockedOutAsync(user)) error = "account_locked";
        else if (!await users.CheckPasswordAsync(user, model.Password))
        {
            await users.AccessFailedAsync(user);
            error = await users.IsLockedOutAsync(user) ? "account_locked" : "invalid_credentials";
        }

        IList<string> roles = [];
        if (error is null)
        {
            roles = await users.GetRolesAsync(user!);
            // Only reached with a correct password, so this reveals nothing to someone guessing.
            if (!roles.Contains(Roles.Admin) && !roles.Contains(Roles.Manager)) error = "login_not_staff";
        }

        if (error is not null)
        {
            logger.LogWarning("Dashboard sign-in failed for {Email}: {Reason}", model.Email, error);
            ModelState.AddModelError("", t[error]);
            return View(model);
        }

        await users.ResetAccessFailedCountAsync(user!);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            StaffPrincipal.Create(user!, roles),
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        logger.LogInformation("Staff {UserId} signed in to the dashboard", user!.Id);
        return LocalRedirect(SafeReturnUrl(model.ReturnUrl));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
}
