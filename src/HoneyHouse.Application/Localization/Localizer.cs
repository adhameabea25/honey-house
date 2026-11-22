using System.Globalization;
using HoneyHouse.Application.Interfaces.Services;

namespace HoneyHouse.Application.Localization;

public interface ILocalizer
{
    string Language { get; }
    string this[string code, params object[] args] { get; }
}

/// <summary>Language from the current UI culture (set per request by the request-localization middleware).</summary>
public sealed class CultureLanguageContext : ILanguageContext
{
    public string Language => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? "en" : "ar";
}

public sealed class Localizer(ILanguageContext language) : ILocalizer
{
    public string Language => language.Language;
    public string this[string code, params object[] args] => Messages.Get(code, language.Language, args);
}
