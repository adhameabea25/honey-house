using System.Text.RegularExpressions;
using HoneyHouse.Admin.Localization;

namespace HoneyHouse.Admin.Tests;

public class TextTests
{
    [Fact]
    public void Every_dashboard_text_has_arabic_and_english_with_same_placeholders()
    {
        foreach (var (key, (ar, en)) in AdminText.Entries)
        {
            ar.ShouldNotBeNullOrWhiteSpace(key);
            en.ShouldNotBeNullOrWhiteSpace(key);
            Placeholders(ar).ShouldBe(Placeholders(en), key);
        }
    }

    private static string[] Placeholders(string s) =>
        Regex.Matches(s, @"\{\d+\}").Select(m => m.Value).Distinct().Order().ToArray();
}
