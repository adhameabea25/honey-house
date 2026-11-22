using System.Text;
using System.Text.RegularExpressions;

namespace HoneyHouse.Application.Common;

public static partial class SlugHelper
{
    public static string Generate(string text)
    {
        var lower = text.Trim().ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        foreach (var c in lower)
            sb.Append(c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-');
        var slug = MultiDash().Replace(sb.ToString(), "-").Trim('-');
        return slug.Length == 0 ? Guid.NewGuid().ToString("N")[..8] : slug;
    }

    public static bool IsValid(string slug) => ValidSlug().IsMatch(slug);

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultiDash();

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex ValidSlug();
}
