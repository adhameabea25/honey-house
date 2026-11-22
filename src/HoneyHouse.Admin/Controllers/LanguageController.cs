using HoneyHouse.Admin.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

[AllowAnonymous]
public sealed class LanguageController : Controller
{
    [HttpPost]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (!AdminServiceExtensions.SupportedCultures.Contains(culture)) culture = "ar";
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, HttpOnly = true, SameSite = SameSiteMode.Lax });
        return LocalRedirect(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
