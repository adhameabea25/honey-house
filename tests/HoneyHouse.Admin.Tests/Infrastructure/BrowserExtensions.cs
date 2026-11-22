using System.Net;
using System.Text.RegularExpressions;

namespace HoneyHouse.Admin.Tests.Infrastructure;

/// <summary>Helpers that drive the dashboard like a browser: read the antiforgery token from a page and post forms.</summary>
public static partial class BrowserExtensions
{
    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    public static async Task<string> GetPageAsync(this HttpClient client, string url, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(expected, body.Length > 500 ? body[..500] : body);
        return body;
    }

    public static string Token(string html)
    {
        var match = TokenRegex().Match(html);
        match.Success.ShouldBeTrue("page has no antiforgery token");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    /// <summary>Loads <paramref name="pageUrl"/> for its token, then posts the form fields to <paramref name="action"/>.</summary>
    public static async Task<HttpResponseMessage> SubmitAsync(this HttpClient client, string pageUrl, string action,
        IEnumerable<KeyValuePair<string, string>> fields)
    {
        var page = await client.GetPageAsync(pageUrl);
        return await client.PostFormAsync(action, fields, Token(page));
    }

    public static Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string action,
        IEnumerable<KeyValuePair<string, string>> fields, string? token)
    {
        var all = fields.ToList();
        if (token is not null) all.Add(new("__RequestVerificationToken", token));
        return client.PostAsync(action, new FormUrlEncodedContent(all));
    }

    public static async Task<HttpResponseMessage> SubmitAsync(this HttpClient client, string pageUrl, string action,
        object fields) => await client.SubmitAsync(pageUrl, action, ToFields(fields));

    public static IEnumerable<KeyValuePair<string, string>> ToFields(object fields) =>
        fields is IEnumerable<KeyValuePair<string, string>> pairs
            ? pairs
            : fields.GetType().GetProperties().Select(p => new KeyValuePair<string, string>(p.Name.Replace("__", "."), Convert.ToString(p.GetValue(fields), System.Globalization.CultureInfo.InvariantCulture) ?? ""));

    public static async Task LoginAsync(this HttpClient client, string email, string password = AdminAppFactory.Password)
    {
        var response = await client.SubmitAsync("/Account/Login", "/Account/Login", new { Email = email, Password = password });
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect, await response.Content.ReadAsStringAsync());
    }

    public static async Task<HttpClient> SignedInAsync(this AdminAppFactory factory, string? email = null)
    {
        var client = factory.CreateBrowser();
        await client.LoginAsync(email ?? AdminAppFactory.AdminEmail);
        return client;
    }

    public static string Location(this HttpResponseMessage response) => response.Headers.Location?.OriginalString ?? "";
}
