using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyHouse.Infrastructure.Payments;

/// <summary>
/// Paymob (Accept) integration using the Intention API and Unified Checkout.
/// Docs: https://developers.paymob.com
/// </summary>
public sealed class PaymobGateway(HttpClient http, IOptions<PaymobOptions> options, ILogger<PaymobGateway> logger) : IPaymentGateway
{
    private readonly PaymobOptions _o = options.Value;

    /// <summary>Fields of the TRANSACTION callback concatenated (in this order) for the HMAC-SHA512 signature.</summary>
    public static readonly string[] HmacFields =
    [
        "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction", "id", "integration_id",
        "is_3d_secure", "is_auth", "is_capture", "is_refunded", "is_standalone_payment", "is_voided", "order.id", "owner",
        "pending", "source_data.pan", "source_data.sub_type", "source_data.type", "success"
    ];

    public bool IsEnabled => _o.Enabled && !string.IsNullOrWhiteSpace(_o.SecretKey) && !string.IsNullOrWhiteSpace(_o.PublicKey)
                             && !string.IsNullOrWhiteSpace(_o.HmacSecret) && _o.IntegrationIds.Length > 0;

    public async Task<PaymentSession> CreateSessionAsync(Order order, PaymentCustomer customer, CancellationToken ct = default)
    {
        var (first, last) = SplitName(customer.FullName);
        var body = new IntentionRequest(
            Amount: ToCents(order.Total),
            Currency: "EGP",
            PaymentMethods: _o.IntegrationIds,
            Items: order.Items.Select(i => new IntentionItem($"{i.ProductNameEn} {i.VariantLabelEn}".Trim(), ToCents(i.UnitPrice), i.Quantity)).ToList(),
            BillingData: new BillingData(first, last, string.IsNullOrWhiteSpace(customer.Email) ? "customer@honeyhouse.local" : customer.Email,
                customer.Phone, order.ShipAddressLine, order.ShipCity, order.ShipGovernorateName, "EG"),
            SpecialReference: order.OrderNumber,
            NotificationUrl: _o.NotificationUrl,
            RedirectionUrl: _o.RedirectionUrl);

        // Items must add up to the amount; shipping/discount are folded in as a single adjustment line when needed.
        var itemsTotal = body.Items.Sum(i => i.Amount * i.Quantity);
        if (itemsTotal != body.Amount)
            body = body with { Items = [new IntentionItem($"Honey House order {order.OrderNumber}", body.Amount, 1)] };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_o.BaseUrl.TrimEnd('/')}/v1/intention/")
        {
            Content = JsonContent.Create(body, options: Json)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _o.SecretKey);

        using var response = await http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Paymob intention failed ({Status}): {Body}", (int)response.StatusCode, text);
            throw new HttpRequestException($"Paymob intention failed with status {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        var clientSecret = root.GetProperty("client_secret").GetString()
                           ?? throw new InvalidOperationException("Paymob response has no client_secret.");
        var providerOrderId = root.TryGetProperty("intention_order_id", out var oid) ? JsonToString(oid)
                              : root.TryGetProperty("id", out var id) ? JsonToString(id) : order.OrderNumber;

        var url = $"{_o.BaseUrl.TrimEnd('/')}/unifiedcheckout/?publicKey={Uri.EscapeDataString(_o.PublicKey)}&clientSecret={Uri.EscapeDataString(clientSecret)}";
        return new PaymentSession(providerOrderId, url);
    }

    public PaymentNotification? ParseWebhook(string rawBody, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(_o.HmacSecret)) return null;

        JsonElement obj;
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var type) && type.GetString() != "TRANSACTION") return null;
            obj = (root.TryGetProperty("obj", out var o) ? o : root).Clone();
        }
        catch (JsonException)
        {
            return null;
        }

        var expected = ComputeHmac(obj, _o.HmacSecret);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant())))
        {
            logger.LogWarning("Rejected Paymob callback with invalid HMAC");
            return null;
        }

        var success = Read(obj, "success") == "true";
        var pending = Read(obj, "pending") == "true";
        var amountCents = decimal.TryParse(Read(obj, "amount_cents"), NumberStyles.Number, CultureInfo.InvariantCulture, out var c) ? c : 0;
        string? merchantRef = obj.TryGetProperty("order", out var ord) && ord.ValueKind == JsonValueKind.Object
                              && ord.TryGetProperty("merchant_order_id", out var m) && m.ValueKind != JsonValueKind.Null ? JsonToString(m) : null;
        string? failure = obj.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
                          && data.TryGetProperty("message", out var msg) ? JsonToString(msg) : null;

        return new PaymentNotification(
            ProviderOrderId: Read(obj, "order.id"),
            MerchantReference: merchantRef,
            TransactionId: Read(obj, "id"),
            Success: success,
            Pending: pending,
            Amount: amountCents / 100m,
            Currency: Read(obj, "currency"),
            FailureReason: success ? null : failure ?? "Declined",
            RawPayload: rawBody);
    }

    /// <summary>HMAC-SHA512 (hex, lower case) of the callback's signature fields.</summary>
    public static string ComputeHmac(JsonElement transaction, string secret)
    {
        var concatenated = string.Concat(HmacFields.Select(f => Read(transaction, f)));
        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(concatenated));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Read(JsonElement obj, string path)
    {
        var current = obj;
        foreach (var part in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current)) return string.Empty;
        }
        return JsonToString(current);
    }

    private static string JsonToString(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        JsonValueKind.String => e.GetString() ?? string.Empty,
        _ => e.GetRawText()
    };

    private static long ToCents(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private static (string First, string Last) SplitName(string name)
    {
        var parts = name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch { 0 => ("Customer", "NA"), 1 => (parts[0], "NA"), _ => (parts[0], parts[1]) };
    }

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private sealed record IntentionRequest(long Amount, string Currency, int[] PaymentMethods, IReadOnlyList<IntentionItem> Items,
        BillingData BillingData, string SpecialReference, string? NotificationUrl, string? RedirectionUrl);

    private sealed record IntentionItem(string Name, long Amount, int Quantity);

    private sealed record BillingData(string FirstName, string LastName, string Email, string PhoneNumber, string Street, string City,
        string State, string Country);
}
