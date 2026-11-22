using HoneyHouse.Domain.Common;

namespace HoneyHouse.Domain.Entities;

public class Product : BaseEntity, ISoftDelete
{
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Origin { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
