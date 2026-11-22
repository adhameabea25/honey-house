using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Infrastructure.Persistence.Repositories;

public sealed class AddressRepository(AppDbContext db) : GenericRepository<Address>(db), IAddressRepository
{
    public async Task<IReadOnlyList<Address>> GetForUserAsync(string userId, CancellationToken ct = default) =>
        await Set.Include(a => a.Governorate).Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id).ToListAsync(ct);

    public Task<Address?> GetForUserAsync(string userId, int id, CancellationToken ct = default) =>
        Set.Include(a => a.Governorate).FirstOrDefaultAsync(a => a.UserId == userId && a.Id == id, ct);
}

public sealed class CartRepository(AppDbContext db) : GenericRepository<Cart>(db), ICartRepository
{
    public Task<Cart?> GetWithItemsAsync(string userId, CancellationToken ct = default) =>
        Set.Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Images)
            .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
}
