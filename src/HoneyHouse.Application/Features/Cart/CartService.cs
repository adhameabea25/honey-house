using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Entities;
using CartEntity = HoneyHouse.Domain.Entities.Cart;

namespace HoneyHouse.Application.Features.Cart;

public interface ICartService
{
    Task<CartDto> GetAsync(string userId, CancellationToken ct = default);
    Task<Result<CartDto>> AddItemAsync(string userId, AddCartItemRequest request, CancellationToken ct = default);
    Task<Result<CartDto>> UpdateItemAsync(string userId, int itemId, UpdateCartItemRequest request, CancellationToken ct = default);
    Task<Result<CartDto>> RemoveItemAsync(string userId, int itemId, CancellationToken ct = default);
    Task ClearAsync(string userId, CancellationToken ct = default);
}

public sealed class CartService(ICartRepository carts, IProductRepository products, IUnitOfWork uow, ILanguageContext lang) : ICartService
{
    public const int MaxQuantityPerItem = ProductService.MaxQuantityPerItem;

    public async Task<CartDto> GetAsync(string userId, CancellationToken ct = default) =>
        ToDto(await carts.GetWithItemsAsync(userId, ct));

    public async Task<Result<CartDto>> AddItemAsync(string userId, AddCartItemRequest r, CancellationToken ct = default)
    {
        var variant = await products.GetVariantAsync(r.VariantId, ct);
        if (variant is null || !IsSellable(variant)) return Error.Business("variant_unavailable");

        var cart = await carts.GetWithItemsAsync(userId, ct) ?? await CreateCartAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.VariantId == r.VariantId);
        var newQuantity = (item?.Quantity ?? 0) + r.Quantity;

        var check = CheckQuantity(variant, newQuantity);
        if (check.IsFailure) return Result<CartDto>.Failure(check.Errors);

        if (item is null) cart.Items.Add(new CartItem { VariantId = variant.Id, Variant = variant, Quantity = newQuantity });
        else item.Quantity = newQuantity;

        await uow.SaveChangesAsync(ct);
        return ToDto(await carts.GetWithItemsAsync(userId, ct));
    }

    public async Task<Result<CartDto>> UpdateItemAsync(string userId, int itemId, UpdateCartItemRequest r, CancellationToken ct = default)
    {
        var cart = await carts.GetWithItemsAsync(userId, ct);
        var item = cart?.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return Error.NotFound("cart_item_not_found");
        if (!IsSellable(item.Variant)) return Error.Business("variant_unavailable");

        var check = CheckQuantity(item.Variant, r.Quantity);
        if (check.IsFailure) return Result<CartDto>.Failure(check.Errors);

        item.Quantity = r.Quantity;
        await uow.SaveChangesAsync(ct);
        return ToDto(cart);
    }

    public async Task<Result<CartDto>> RemoveItemAsync(string userId, int itemId, CancellationToken ct = default)
    {
        var cart = await carts.GetWithItemsAsync(userId, ct);
        var item = cart?.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return Error.NotFound("cart_item_not_found");

        cart!.Items.Remove(item);
        uow.Repository<CartItem>().Remove(item);
        await uow.SaveChangesAsync(ct);
        return ToDto(cart);
    }

    public async Task ClearAsync(string userId, CancellationToken ct = default)
    {
        var cart = await carts.GetWithItemsAsync(userId, ct);
        if (cart is null || cart.Items.Count == 0) return;
        foreach (var item in cart.Items.ToList()) uow.Repository<CartItem>().Remove(item);
        cart.Items.Clear();
        await uow.SaveChangesAsync(ct);
    }

    /// <summary>Product and variant are active and the product's category is active.</summary>
    public static bool IsSellable(ProductVariant v) =>
        v.IsActive && v.Product is { IsActive: true, IsDeleted: false } p && (p.Category is null || p.Category.IsActive);

    public static Result CheckQuantity(ProductVariant variant, int quantity)
    {
        if (quantity > MaxQuantityPerItem) return Error.Business("max_quantity_exceeded", MaxQuantityPerItem);
        if (quantity > variant.StockQuantity) return Error.Business("insufficient_stock", variant.StockQuantity);
        return Result.Success();
    }

    private async Task<CartEntity> CreateCartAsync(string userId, CancellationToken ct)
    {
        var cart = new CartEntity { UserId = userId };
        await carts.AddAsync(cart, ct);
        return cart;
    }

    private CartDto ToDto(CartEntity? cart)
    {
        if (cart is null) return new CartDto([], 0, 0, ProductService.Currency, false);

        var items = cart.Items.OrderBy(i => i.Id).Select(i =>
        {
            var v = i.Variant;
            var p = v.Product;
            var available = IsSellable(v) && v.StockQuantity >= i.Quantity;
            return new CartItemDto(
                i.Id, v.Id, v.ProductId,
                p is null ? v.Sku : lang.Pick(p.NameAr, p.NameEn),
                p?.Slug ?? string.Empty,
                lang.Pick(v.LabelAr, v.LabelEn), v.Sku,
                p?.Images.OrderByDescending(x => x.IsMain).ThenBy(x => x.SortOrder).Select(x => x.Url).FirstOrDefault(),
                v.Price, v.CompareAtPrice, i.Quantity, v.Price * i.Quantity, available,
                IsSellable(v) ? Math.Min(v.StockQuantity, MaxQuantityPerItem) : 0);
        }).ToList();

        var sellable = items.Where(i => i.IsAvailable).ToList();
        return new CartDto(items, sellable.Sum(i => i.Quantity), sellable.Sum(i => i.LineTotal), ProductService.Currency,
            items.Any(i => !i.IsAvailable));
    }
}
