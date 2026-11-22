using System.Net.Http.Json;
using System.Text.Json;

namespace HoneyHouse.IntegrationTests.Infrastructure;

public static class HttpExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static HttpClient WithLanguage(this HttpClient client, string lang)
    {
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(lang);
        return client;
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Json);
}
