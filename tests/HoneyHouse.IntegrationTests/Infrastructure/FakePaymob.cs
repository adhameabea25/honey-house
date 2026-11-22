using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HoneyHouse.Infrastructure.Payments;

namespace HoneyHouse.IntegrationTests.Infrastructure;

/// <summary>Stands in for the Paymob Intention API.</summary>
public sealed class FakePaymobHandler : HttpMessageHandler
{
    private int _nextOrderId = 900000;
    private readonly List<(string Auth, JsonNode Body)> _requests = [];
    public bool Fail { get; set; }

    public IReadOnlyList<(string Auth, JsonNode Body)> Requests { get { lock (_requests) return _requests.ToList(); } }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!;
        lock (_requests) _requests.Add((request.Headers.Authorization?.ToString() ?? "", body));
        if (Fail) return new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("{\"detail\":\"down\"}") };

        var id = Interlocked.Increment(ref _nextOrderId);
        var json = $$"""{"id":"pi_test_{{id}}","client_secret":"cs_test_{{id}}","intention_order_id":{{id}},"special_reference":"{{body["special_reference"]}}"}""";
        return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}

public static class PaymobCallback
{
    public const string HmacSecret = "test-hmac-secret";

    /// <summary>Builds a signed TRANSACTION callback body and its HMAC.</summary>
    public static (string Body, string Hmac) Create(string providerOrderId, string merchantOrderId, decimal amount, bool success, bool pending = false, long txId = 0)
    {
        var obj = new JsonObject
        {
            ["id"] = txId == 0 ? Random.Shared.NextInt64(1_000_000, 9_999_999) : txId,
            ["pending"] = pending,
            ["amount_cents"] = (long)(amount * 100),
            ["success"] = success,
            ["is_auth"] = false,
            ["is_capture"] = false,
            ["is_standalone_payment"] = true,
            ["is_voided"] = false,
            ["is_refunded"] = false,
            ["is_3d_secure"] = true,
            ["integration_id"] = 4567,
            ["has_parent_transaction"] = false,
            ["order"] = new JsonObject { ["id"] = long.Parse(providerOrderId), ["merchant_order_id"] = merchantOrderId },
            ["created_at"] = "2026-11-22T10:00:00.000000",
            ["currency"] = "EGP",
            ["source_data"] = new JsonObject { ["pan"] = "2346", ["type"] = "card", ["sub_type"] = "MasterCard" },
            ["error_occured"] = false,
            ["owner"] = 1234,
            ["data"] = new JsonObject { ["message"] = success ? "Approved" : "Do not honour" }
        };
        var body = new JsonObject { ["type"] = "TRANSACTION", ["obj"] = obj }.ToJsonString();
        using var doc = JsonDocument.Parse(obj.ToJsonString());
        return (body, PaymobGateway.ComputeHmac(doc.RootElement, HmacSecret));
    }
}
