using HoneyHouse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoneyHouse.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(150).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(1000);
        b.Property(x => x.DescriptionEn).HasMaxLength(1000);
        b.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasOne(x => x.ParentCategory).WithMany(x => x.SubCategories)
            .HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(4000);
        b.Property(x => x.DescriptionEn).HasMaxLength(4000);
        b.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        b.Property(x => x.Origin).HasMaxLength(100);
        b.Property(x => x.AverageRating).HasPrecision(3, 2);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.IsActive, x.IsFeatured });
        b.HasOne(x => x.Category).WithMany(c => c.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b)
    {
        b.Property(x => x.Url).HasMaxLength(500).IsRequired();
        b.Property(x => x.AltText).HasMaxLength(200);
        b.HasOne(x => x.Product).WithMany(p => p.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(x => !x.Product.IsDeleted);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        b.Property(x => x.Sku).HasMaxLength(50).IsRequired();
        b.Property(x => x.LabelAr).HasMaxLength(50).IsRequired();
        b.Property(x => x.LabelEn).HasMaxLength(50).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.Sku).IsUnique();
        b.Ignore(x => x.IsLowStock);
        b.Ignore(x => x.IsInStock);
        b.HasOne(x => x.Product).WithMany(p => p.Variants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ProductVariants_Stock", "[StockQuantity] >= 0");
            t.HasCheckConstraint("CK_ProductVariants_Price", "[Price] >= 0");
        });
    }
}

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> b)
    {
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => new { x.VariantId, x.CreatedAt });
        b.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
    }
}
