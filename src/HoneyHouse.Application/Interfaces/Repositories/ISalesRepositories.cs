using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Interfaces.Repositories;

public interface IAddressRepository : IGenericRepository<Address>
{
    Task<IReadOnlyList<Address>> GetForUserAsync(string userId, CancellationToken ct = default);
    Task<Address?> GetForUserAsync(string userId, int id, CancellationToken ct = default);
}

public interface ICartRepository : IGenericRepository<Cart>
{
    /// <summary>Cart with items, variants, products and images (tracked).</summary>
    Task<Cart?> GetWithItemsAsync(string userId, CancellationToken ct = default);
}
