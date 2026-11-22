using System.Text.Json;
using HoneyHouse.Infrastructure.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace HoneyHouse.UnitTests.Infrastructure;

public class PaymobGatewayTests
{
    // Field values in the documented HMAC order; expected digest computed independently (Python hmac/sha512).
    private const string Transaction = """
        {"amount_cents":1000,"created_at":"2026-01-01T00:00:00","currency":"EGP","error_occured":false,
         "has_parent_transaction":false,"id":42,"integration_id":7,"is_3d_secure":true,"is_auth":false,
         "is_capture":false,"is_refunded":false,"is_standalone_payment":true,"is_voided":false,
         "order":{"id":99,"merchant_order_id":"HH-261122-ABC123"},"owner":5,"pending":false,
         "source_data":{"pan":"1234","sub_type":"Visa","type":"card"},"success":true}
        """;

    private const string ExpectedHmac =
        "87738abbbe7ec1a6f3ba43de1e0b76c68febb56e56db2ade4ab223f2a58f413b6884cb53f90aecfa69a6f06ae49de957a7f98c5f0482b2b58e3b48de818a335e";

    private static PaymobGateway Gateway(string secret = "secret") =>
        new(new HttpClient(), Options.Create(new PaymobOptions { HmacSecret = secret }), NullLogger<PaymobGateway>.Instance);

    [Fact]
    public void Hmac_matches_independent_computation()
    {
        using var doc = JsonDocument.Parse(Transaction);
        PaymobGateway.ComputeHmac(doc.RootElement, "secret").ShouldBe(ExpectedHmac);
    }

    [Fact]
    public void Valid_callback_is_parsed()
    {
        var body = $$"""{"type":"TRANSACTION","obj":{{Transaction}}}""";
        var n = Gateway().ParseWebhook(body, ExpectedHmac.ToUpperInvariant());
        n.ShouldNotBeNull();
        n.Success.ShouldBeTrue();
        n.Pending.ShouldBeFalse();
        n.Amount.ShouldBe(10m);
        n.ProviderOrderId.ShouldBe("99");
        n.MerchantReference.ShouldBe("HH-261122-ABC123");
        n.TransactionId.ShouldBe("42");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("deadbeef")]
    public void Invalid_signature_is_rejected(string? hmac) =>
        Gateway().ParseWebhook($$"""{"type":"TRANSACTION","obj":{{Transaction}}}""", hmac).ShouldBeNull();

    [Fact]
    public void Tampered_amount_is_rejected() =>
        Gateway().ParseWebhook($$"""{"type":"TRANSACTION","obj":{{Transaction.Replace("1000", "100000")}}}""", ExpectedHmac).ShouldBeNull();

    [Fact]
    public void Non_transaction_and_garbage_payloads_are_ignored()
    {
        Gateway().ParseWebhook("""{"type":"TOKEN","obj":{}}""", ExpectedHmac).ShouldBeNull();
        Gateway().ParseWebhook("not json", ExpectedHmac).ShouldBeNull();
    }

    [Fact]
    public void Disabled_without_keys() => Gateway().IsEnabled.ShouldBeFalse();
}
