using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Application.Localization;

namespace HoneyHouse.Admin.Localization;

/// <summary>Dashboard UI text in Arabic and English. Injected in views as <c>@inject IText T</c>.</summary>
public interface IText
{
    string Language { get; }
    bool IsArabic { get; }
    string this[string key, params object[] args] { get; }
}

public sealed partial class AdminText(ILanguageContext language) : IText
{
    public string Language => language.Language;
    public bool IsArabic => language.Language == "ar";

    public string this[string key, params object[] args]
    {
        get
        {
            if (All.TryGetValue(key, out var text))
            {
                var value = IsArabic ? text.Ar : text.En;
                return args.Length == 0 ? value : string.Format(value, args);
            }
            // Fall back to the shared API messages (error codes, statuses...)
            return Messages.Exists(key) ? Messages.Get(key, Language, args) : key;
        }
    }

    public static bool Has(string key) => All.ContainsKey(key);

    /// <summary>Text for the current UI culture, for code that runs outside DI (e.g. MVC binding messages).</summary>
    public static string Static(string key) =>
        All.TryGetValue(key, out var t)
            ? (System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? t.En : t.Ar)
            : key;
    public static IReadOnlyDictionary<string, (string Ar, string En)> Entries => All;
}
