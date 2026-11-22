namespace HoneyHouse.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Cancelled = 6,
    Returned = 7
}

public enum PaymentMethod
{
    CashOnDelivery = 0,
    Online = 1
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

public enum PaymentProvider
{
    CashOnDelivery = 0,
    Paymob = 1
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum InventoryTransactionType
{
    Restock = 0,
    Sale = 1,
    Return = 2,
    Adjustment = 3,
    Cancellation = 4
}
