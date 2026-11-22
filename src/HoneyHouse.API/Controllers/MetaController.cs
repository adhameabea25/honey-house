using HoneyHouse.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers;

/// <summary>Store metadata used by the clients at startup.</summary>
[Route("api/v1/meta")]
public class MetaController : ApiControllerBase
{
    public sealed record StoreInfo(string Name, string Language, string Currency, string[] SupportedLanguages, string ApiVersion);

    /// <summary>Store name in the requested language, currency and supported languages.</summary>
    [HttpGet]
    [ProducesResponseType<StoreInfo>(StatusCodes.Status200OK)]
    public ActionResult<StoreInfo> Get() =>
        new StoreInfo(L["store_name"], L.Language, "EGP", ["ar", "en"], "v1");
}
