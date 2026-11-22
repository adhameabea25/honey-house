namespace HoneyHouse.Application.Interfaces.Services;

public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

public interface ILanguageContext
{
    /// <summary>"ar" or "en".</summary>
    string Language { get; }
    bool IsArabic => Language == "ar";
    string Pick(string arabic, string english) => IsArabic ? arabic : english;
}
