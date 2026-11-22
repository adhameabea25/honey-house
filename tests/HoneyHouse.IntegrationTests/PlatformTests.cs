using System.Net;
using HoneyHouse.IntegrationTests.Infrastructure;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PlatformTests(HoneyHouseApiFactory factory)
{
    [Fact]
    public async Task Health_endpoint_reports_healthy_database()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Theory]
    [InlineData("en", "Honey House")]
    [InlineData("ar", "بيت العسل")]
    [InlineData("fr", "بيت العسل")] // unsupported => Arabic default
    public async Task Meta_is_localized_by_accept_language(string lang, string expectedName)
    {
        var json = await (await factory.CreateClient().WithLanguage(lang).GetAsync("/api/v1/meta")).ReadJsonAsync();
        json.GetProperty("name").GetString().ShouldBe(expectedName);
        json.GetProperty("currency").GetString().ShouldBe("EGP");
    }

    [Fact]
    public async Task Lang_query_string_overrides_header()
    {
        var json = await (await factory.CreateClient().WithLanguage("ar").GetAsync("/api/v1/meta?lang=en")).ReadJsonAsync();
        json.GetProperty("language").GetString().ShouldBe("en");
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/does-not-exist");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Swagger_document_is_served()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Honey House API");
    }
}
