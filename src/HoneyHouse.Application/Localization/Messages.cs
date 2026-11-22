namespace HoneyHouse.Application.Localization;

/// <summary>Bilingual (Arabic / English) user-facing messages keyed by stable error/message codes.</summary>
public static class Messages
{
    private static readonly Dictionary<string, (string Ar, string En)> All = new(StringComparer.OrdinalIgnoreCase)
    {
        // General
        ["store_name"] = ("بيت العسل", "Honey House"),
        ["server_error"] = ("حدث خطأ غير متوقع، حاول مرة أخرى لاحقًا", "An unexpected error occurred. Please try again later."),
        ["validation_failed"] = ("خطأ في التحقق من البيانات", "One or more validation errors occurred."),
        ["not_found"] = ("العنصر غير موجود", "The requested resource was not found."),
        ["concurrency_conflict"] = ("تم تعديل البيانات من طلب آخر، حاول مرة أخرى", "The data was changed by another request. Please retry."),
        ["unauthorized"] = ("يجب تسجيل الدخول أولًا", "You must be signed in."),
        ["forbidden"] = ("ليس لديك صلاحية لتنفيذ هذا الإجراء", "You do not have permission to perform this action."),
        ["too_many_requests"] = ("محاولات كثيرة، انتظر قليلًا ثم حاول مرة أخرى", "Too many requests. Please wait and try again."),
        ["conflict"] = ("تعارض في البيانات", "Conflict."),
        ["business_rule"] = ("لا يمكن تنفيذ العملية", "The operation cannot be completed."),

        // Validation
        ["phone_invalid"] = ("رقم الهاتف غير صحيح، يجب أن يكون رقمًا مصريًا مثل 01012345678", "Invalid phone number. Use an Egyptian mobile number like 01012345678."),
        ["password_weak"] = ("كلمة المرور يجب ألا تقل عن 8 أحرف وتحتوي على حرف صغير ورقم", "Password must be at least 8 characters and contain a lowercase letter and a digit."),
        ["language_invalid"] = ("اللغة يجب أن تكون ar أو en", "Language must be 'ar' or 'en'."),
    };

    public static bool Exists(string code) => All.ContainsKey(code);

    public static string Get(string code, string language, params object[]? args)
    {
        if (!All.TryGetValue(code, out var m)) return code;
        var text = language == "en" ? m.En : m.Ar;
        return args is { Length: > 0 } ? string.Format(text, args) : text;
    }

    internal static void Register(string code, string ar, string en) => All[code] = (ar, en);

    public static IReadOnlyCollection<string> Codes => All.Keys;
}
