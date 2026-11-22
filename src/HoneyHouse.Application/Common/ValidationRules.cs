using FluentValidation;

namespace HoneyHouse.Application.Common;

public static class ValidationRules
{
    /// <summary>Egyptian mobile numbers: 010/011/012/015 followed by 8 digits, optional +2 / 002 prefix.</summary>
    public const string EgyptianMobilePattern = @"^(\+2|002)?01[0125][0-9]{8}$";

    public static IRuleBuilderOptions<T, string?> EgyptianMobile<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches(EgyptianMobilePattern).WithErrorCode("phone_invalid");

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MinimumLength(8).Matches("[a-z]").Matches("[0-9]").WithErrorCode("password_weak");

    public static IRuleBuilderOptions<T, string?> SupportedLanguage<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(l => l is null or "ar" or "en").WithErrorCode("language_invalid");
}
