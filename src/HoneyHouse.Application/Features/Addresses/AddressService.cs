using HoneyHouse.Application.Common;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Features.Addresses;

public interface IAddressService
{
    Task<IReadOnlyList<GovernorateDto>> GetGovernoratesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminGovernorateDto>> GetGovernoratesForAdminAsync(CancellationToken ct = default);
    Task<Result<AdminGovernorateDto>> UpdateGovernorateAsync(int id, UpdateGovernorateRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<AddressDto>> GetMineAsync(string userId, CancellationToken ct = default);
    Task<Result<AddressDto>> GetAsync(string userId, int id, CancellationToken ct = default);
    Task<Result<AddressDto>> CreateAsync(string userId, AddressUpsertRequest request, CancellationToken ct = default);
    Task<Result<AddressDto>> UpdateAsync(string userId, int id, AddressUpsertRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string userId, int id, CancellationToken ct = default);
    Task<Result> SetDefaultAsync(string userId, int id, CancellationToken ct = default);
}

public sealed class AddressService(IAddressRepository addresses, IUnitOfWork uow, ILanguageContext lang) : IAddressService
{
    public const int MaxAddressesPerUser = 10;
    private IGenericRepository<Governorate> Governorates => uow.Repository<Governorate>();

    public async Task<IReadOnlyList<GovernorateDto>> GetGovernoratesAsync(CancellationToken ct = default) =>
        (await Governorates.ListAsync(g => g.IsActive, ct))
            .OrderBy(g => g.Id)
            .Select(g => new GovernorateDto(g.Id, lang.Pick(g.NameAr, g.NameEn), g.ShippingFee, "EGP"))
            .ToList();

    public async Task<IReadOnlyList<AdminGovernorateDto>> GetGovernoratesForAdminAsync(CancellationToken ct = default) =>
        (await Governorates.ListAsync(null, ct)).OrderBy(g => g.Id).Select(ToAdmin).ToList();

    public async Task<Result<AdminGovernorateDto>> UpdateGovernorateAsync(int id, UpdateGovernorateRequest r, CancellationToken ct = default)
    {
        var g = await Governorates.GetByIdAsync(id, ct);
        if (g is null) return Error.NotFound("governorate_not_found");
        g.ShippingFee = r.ShippingFee;
        g.IsActive = r.IsActive;
        await uow.SaveChangesAsync(ct);
        return ToAdmin(g);
    }

    public async Task<IReadOnlyList<AddressDto>> GetMineAsync(string userId, CancellationToken ct = default) =>
        (await addresses.GetForUserAsync(userId, ct)).Select(ToDto).ToList();

    public async Task<Result<AddressDto>> GetAsync(string userId, int id, CancellationToken ct = default) =>
        await addresses.GetForUserAsync(userId, id, ct) is { } a ? ToDto(a) : Error.NotFound("address_not_found");

    public async Task<Result<AddressDto>> CreateAsync(string userId, AddressUpsertRequest r, CancellationToken ct = default)
    {
        var existing = await addresses.GetForUserAsync(userId, ct);
        if (existing.Count >= MaxAddressesPerUser) return Error.Business("address_limit", MaxAddressesPerUser);

        var governorate = await Governorates.GetByIdAsync(r.GovernorateId, ct);
        if (governorate is not { IsActive: true }) return Error.Validation("governorate_not_found", "governorateId");

        var address = new Address { UserId = userId, Governorate = governorate };
        Apply(address, r);
        address.IsDefault = r.IsDefault || existing.Count == 0;
        if (address.IsDefault) foreach (var a in existing) a.IsDefault = false;

        await addresses.AddAsync(address, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(address);
    }

    public async Task<Result<AddressDto>> UpdateAsync(string userId, int id, AddressUpsertRequest r, CancellationToken ct = default)
    {
        var all = await addresses.GetForUserAsync(userId, ct);
        var address = all.FirstOrDefault(a => a.Id == id);
        if (address is null) return Error.NotFound("address_not_found");

        if (address.GovernorateId != r.GovernorateId)
        {
            var governorate = await Governorates.GetByIdAsync(r.GovernorateId, ct);
            if (governorate is not { IsActive: true }) return Error.Validation("governorate_not_found", "governorateId");
            address.Governorate = governorate;
        }
        Apply(address, r);
        if (r.IsDefault)
            foreach (var a in all) a.IsDefault = a.Id == id;

        await uow.SaveChangesAsync(ct);
        return ToDto(address);
    }

    public async Task<Result> DeleteAsync(string userId, int id, CancellationToken ct = default)
    {
        var all = await addresses.GetForUserAsync(userId, ct);
        var address = all.FirstOrDefault(a => a.Id == id);
        if (address is null) return Error.NotFound("address_not_found");

        addresses.Remove(address);
        if (address.IsDefault && all.FirstOrDefault(a => a.Id != id) is { } next) next.IsDefault = true;
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetDefaultAsync(string userId, int id, CancellationToken ct = default)
    {
        var all = await addresses.GetForUserAsync(userId, ct);
        if (all.All(a => a.Id != id)) return Error.NotFound("address_not_found");
        foreach (var a in all) a.IsDefault = a.Id == id;
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static void Apply(Address a, AddressUpsertRequest r)
    {
        a.GovernorateId = r.GovernorateId;
        a.RecipientName = r.RecipientName.Trim();
        a.Phone = r.Phone.Trim();
        a.City = r.City.Trim();
        a.Area = r.Area?.Trim();
        a.Street = r.Street.Trim();
        a.Building = r.Building?.Trim();
        a.Floor = r.Floor?.Trim();
        a.Apartment = r.Apartment?.Trim();
        a.Landmark = r.Landmark?.Trim();
    }

    private AddressDto ToDto(Address a) => new(a.Id, a.GovernorateId, lang.Pick(a.Governorate.NameAr, a.Governorate.NameEn),
        a.Governorate.ShippingFee, a.RecipientName, a.Phone, a.City, a.Area, a.Street, a.Building, a.Floor, a.Apartment, a.Landmark, a.IsDefault);

    private static AdminGovernorateDto ToAdmin(Governorate g) => new(g.Id, g.NameAr, g.NameEn, g.ShippingFee, g.IsActive);
}
