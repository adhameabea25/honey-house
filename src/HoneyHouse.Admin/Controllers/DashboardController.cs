using HoneyHouse.Admin.Common;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

public sealed class DashboardController : AdminControllerBase
{
    public IActionResult Index() => View();
}
