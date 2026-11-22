using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ErrorController : Controller
{
    [Route("Error/{code:int?}")]
    [IgnoreAntiforgeryToken]
    public IActionResult Index(int? code)
    {
        ViewData["Code"] = code ?? 500;
        return View("~/Views/Shared/Error.cshtml");
    }
}
