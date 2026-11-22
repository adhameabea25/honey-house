using HoneyHouse.Domain.Common;

namespace HoneyHouse.Domain.Entities;

public class Governorate : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public bool IsActive { get; set; } = true;
}
