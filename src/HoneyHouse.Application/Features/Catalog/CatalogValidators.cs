using FluentValidation;
using HoneyHouse.Application.Common;

namespace HoneyHouse.Application.Features.Catalog;

public sealed class CategoryUpsertRequestValidator : AbstractValidator<CategoryUpsertRequest>
{
    public CategoryUpsertRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).MaximumLength(1000);
        RuleFor(x => x.Slug!).MaximumLength(160).Must(SlugHelper.IsValid).When(x => !string.IsNullOrEmpty(x.Slug));
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public sealed class VariantUpsertRequestValidator : AbstractValidator<VariantUpsertRequest>
{
    public VariantUpsertRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9-_]+$");
        RuleFor(x => x.WeightGrams).GreaterThan(0);
        RuleFor(x => x.LabelAr).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LabelEn).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.CompareAtPrice).GreaterThan(x => x.Price).When(x => x.CompareAtPrice.HasValue);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(4000);
        RuleFor(x => x.DescriptionEn).MaximumLength(4000);
        RuleFor(x => x.Origin).MaximumLength(100);
        RuleFor(x => x.Slug!).MaximumLength(220).Must(SlugHelper.IsValid).When(x => !string.IsNullOrEmpty(x.Slug));
        RuleFor(x => x.Variants).NotEmpty().WithErrorCode("product_needs_variant");
        RuleForEach(x => x.Variants).SetValidator(new VariantUpsertRequestValidator());
        RuleFor(x => x.Variants)
            .Must(v => v.Select(x => x.Sku.ToUpperInvariant()).Distinct().Count() == v.Count)
            .When(x => x.Variants is { Count: > 0 })
            .WithMessage("Duplicate SKU in request.");
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(4000);
        RuleFor(x => x.DescriptionEn).MaximumLength(4000);
        RuleFor(x => x.Origin).MaximumLength(100);
        RuleFor(x => x.Slug!).MaximumLength(220).Must(SlugHelper.IsValid).When(x => !string.IsNullOrEmpty(x.Slug));
    }
}
