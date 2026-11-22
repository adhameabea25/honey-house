using FluentValidation;

namespace HoneyHouse.Application.Features.Cart;

public sealed record CartItemDto(
    int Id, int VariantId, int ProductId, string ProductName, string ProductSlug, string VariantLabel, string Sku, string? ImageUrl,
    decimal UnitPrice, decimal? CompareAtPrice, int Quantity, decimal LineTotal, bool IsAvailable, int AvailableQuantity);

public sealed record CartDto(IReadOnlyList<CartItemDto> Items, int ItemCount, decimal Subtotal, string Currency, bool HasUnavailableItems);

public sealed record AddCartItemRequest(int VariantId, int Quantity = 1);
public sealed record UpdateCartItemRequest(int Quantity);

public sealed class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.VariantId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator() => RuleFor(x => x.Quantity).GreaterThan(0);
}
