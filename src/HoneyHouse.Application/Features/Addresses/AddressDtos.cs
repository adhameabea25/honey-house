using FluentValidation;
using HoneyHouse.Application.Common;

namespace HoneyHouse.Application.Features.Addresses;

public sealed record GovernorateDto(int Id, string Name, decimal ShippingFee, string Currency);
public sealed record AdminGovernorateDto(int Id, string NameAr, string NameEn, decimal ShippingFee, bool IsActive);
public sealed record UpdateGovernorateRequest(decimal ShippingFee, bool IsActive);

public sealed record AddressDto(int Id, int GovernorateId, string GovernorateName, decimal ShippingFee, string RecipientName, string Phone,
    string City, string? Area, string Street, string? Building, string? Floor, string? Apartment, string? Landmark, bool IsDefault);

public sealed record AddressUpsertRequest(int GovernorateId, string RecipientName, string Phone, string City, string? Area, string Street,
    string? Building, string? Floor, string? Apartment, string? Landmark, bool IsDefault = false);

public sealed class AddressUpsertRequestValidator : AbstractValidator<AddressUpsertRequest>
{
    public AddressUpsertRequestValidator()
    {
        RuleFor(x => x.GovernorateId).GreaterThan(0);
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty().EgyptianMobile();
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Area).MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Building).MaximumLength(50);
        RuleFor(x => x.Floor).MaximumLength(20);
        RuleFor(x => x.Apartment).MaximumLength(20);
        RuleFor(x => x.Landmark).MaximumLength(250);
    }
}

public sealed class UpdateGovernorateRequestValidator : AbstractValidator<UpdateGovernorateRequest>
{
    public UpdateGovernorateRequestValidator() => RuleFor(x => x.ShippingFee).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
}
