using HoneyHouse.API.Common;
using HoneyHouse.Application.Features.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Controllers;

/// <summary>Egyptian governorates and their delivery fees.</summary>
[Route("api/v1/governorates")]
public class GovernoratesController(IAddressService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GovernorateDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await service.GetGovernoratesAsync(ct));
}

/// <summary>The signed-in customer's delivery addresses.</summary>
[Authorize]
[Route("api/v1/addresses")]
public class AddressesController(IAddressService service) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AddressDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct) => Ok(await service.GetMineAsync(User.GetUserId(), ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => FromResult(await service.GetAsync(User.GetUserId(), id, ct));

    [HttpPost]
    [ProducesResponseType<AddressDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(AddressUpsertRequest request, CancellationToken ct) =>
        CreatedFromResult(await service.CreateAsync(User.GetUserId(), request, ct), nameof(Get), a => new { id = a.Id });

    [HttpPut("{id:int}")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, AddressUpsertRequest request, CancellationToken ct) =>
        FromResult(await service.UpdateAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) => FromResult(await service.DeleteAsync(User.GetUserId(), id, ct));

    [HttpPut("{id:int}/default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetDefault(int id, CancellationToken ct) => FromResult(await service.SetDefaultAsync(User.GetUserId(), id, ct));
}
