using HoneyHouse.Domain.Common;

namespace HoneyHouse.Domain.Entities;

public class Address : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int GovernorateId { get; set; }
    public Governorate Governorate { get; set; } = null!;
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string Street { get; set; } = string.Empty;
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Landmark { get; set; }
    public bool IsDefault { get; set; }
}
