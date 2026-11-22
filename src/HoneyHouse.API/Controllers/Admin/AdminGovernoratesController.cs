using HoneyHouse.API.Common;
using HoneyHouse.Application.Features.Addresses;
using HoneyHouse.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers.Admin;

/// <summary>Delivery fee management (Admin).</summary>
[Authorize(Roles = Roles.Admin)]
[Route("api/v1/admin/governorates")]
public class AdminGovernoratesController(IAddressService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminGovernorateDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await service.GetGovernoratesForAdminAsync(ct));

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminGovernorateDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, UpdateGovernorateRequest request, CancellationToken ct) =>
        FromResult(await service.UpdateGovernorateAsync(id, request, ct));
}
