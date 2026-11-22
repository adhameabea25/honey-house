namespace HoneyHouse.Infrastructure.Payments;

public sealed class PaymobOptions
{
    public const string Section = "Paymob";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://accept.paymob.com";
    /// <summary>Secret key (Token auth for the Intention API).</summary>
    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    /// <summary>HMAC secret used to verify transaction callbacks.</summary>
    public string HmacSecret { get; set; } = string.Empty;
    /// <summary>Paymob integration ids to offer (card, wallet, ...).</summary>
    public int[] IntegrationIds { get; set; } = [];
    /// <summary>Public URL of POST /api/v1/payments/paymob/webhook.</summary>
    public string? NotificationUrl { get; set; }
    /// <summary>Where Paymob sends the customer after paying (e.g. the app deep link or a web page).</summary>
    public string? RedirectionUrl { get; set; }
}
