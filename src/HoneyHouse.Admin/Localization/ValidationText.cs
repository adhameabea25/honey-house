using FluentValidation.Results;

namespace HoneyHouse.Admin.Localization;

/// <summary>Turns FluentValidation failures into dashboard text (custom error codes or built-in validator names).</summary>
public static class ValidationText
{
    public static string Describe(IText t, ValidationFailure failure)
    {
        var code = failure.ErrorCode;
        if (!string.IsNullOrEmpty(code) && (AdminText.Has(code) || HoneyHouse.Application.Localization.Messages.Exists(code)))
        {
            var values = failure.FormattedMessagePlaceholderValues;
            object[] args = code switch
            {
                "MaximumLengthValidator" or "LengthValidator" => [Get(values, "MaxLength")],
                "MinimumLengthValidator" => [Get(values, "MinLength")],
                "GreaterThanValidator" or "GreaterThanOrEqualValidator" or "LessThanValidator" or "LessThanOrEqualValidator"
                    => [Get(values, "ComparisonValue")],
                "InclusiveBetweenValidator" or "ExclusiveBetweenValidator" => [Get(values, "From"), Get(values, "To")],
                _ => []
            };
            return t[code, args];
        }
        return t.IsArabic ? t["invalid_value"] : failure.ErrorMessage;
    }

    private static object Get(Dictionary<string, object>? values, string key) =>
        values is not null && values.TryGetValue(key, out var v) ? v : "";
}
