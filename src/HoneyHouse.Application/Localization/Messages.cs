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

        // Auth
        ["email_taken"] = ("البريد الإلكتروني مستخدم بالفعل", "This email is already registered."),
        ["invalid_credentials"] = ("البريد الإلكتروني أو كلمة المرور غير صحيحة", "Invalid email or password."),
        ["account_locked"] = ("تم إيقاف الحساب مؤقتًا بسبب محاولات دخول خاطئة كثيرة، حاول بعد 15 دقيقة", "Account temporarily locked after too many failed attempts. Try again in 15 minutes."),
        ["account_disabled"] = ("هذا الحساب موقوف، تواصل مع خدمة العملاء", "This account is disabled. Please contact support."),
        ["invalid_refresh_token"] = ("انتهت الجلسة، سجّل الدخول مرة أخرى", "Your session has expired. Please sign in again."),
        ["invalid_reset_token"] = ("رابط إعادة تعيين كلمة المرور غير صالح أو منتهي", "The password reset link is invalid or has expired."),
        ["password_incorrect"] = ("كلمة المرور الحالية غير صحيحة", "The current password is incorrect."),
        ["user_not_found"] = ("المستخدم غير موجود", "User not found."),
        // Catalog
        ["category_not_found"] = ("التصنيف غير موجود", "Category not found."),
        ["parent_category_invalid"] = ("التصنيف الأب غير صالح (يُسمح بمستويين فقط)", "Invalid parent category (only two levels are allowed)."),
        ["category_not_empty"] = ("لا يمكن حذف تصنيف يحتوي على منتجات أو تصنيفات فرعية", "A category with products or sub-categories cannot be deleted."),
        ["slug_taken"] = ("الرابط المختصر (Slug) مستخدم بالفعل", "This slug is already in use."),
        ["product_not_found"] = ("المنتج غير موجود", "Product not found."),
        ["variant_not_found"] = ("الوزن المطلوب غير موجود", "Product option not found."),
        ["sku_taken"] = ("رمز المنتج (SKU) مستخدم بالفعل: {0}", "SKU already in use: {0}"),
        ["product_needs_variant"] = ("يجب أن يحتوي المنتج على وزن واحد نشط على الأقل", "A product must have at least one active weight option."),
        ["image_not_found"] = ("الصورة غير موجودة", "Image not found."),
        ["image_invalid"] = ("الصورة يجب أن تكون JPG أو PNG أو WEBP وبحجم أقل من 5 ميجابايت", "Image must be JPG, PNG or WEBP and smaller than 5 MB."),
        // Addresses & cart
        ["governorate_not_found"] = ("المحافظة غير موجودة أو غير متاحة للتوصيل", "Governorate not found or not available for delivery."),
        ["address_not_found"] = ("العنوان غير موجود", "Address not found."),
        ["address_limit"] = ("لا يمكن إضافة أكثر من {0} عناوين", "You cannot save more than {0} addresses."),
        ["cart_item_not_found"] = ("المنتج غير موجود في السلة", "Item not found in cart."),
        ["variant_unavailable"] = ("هذا المنتج غير متاح حاليًا", "This product is currently unavailable."),
        ["insufficient_stock"] = ("الكمية المطلوبة غير متوفرة، المتاح {0} فقط", "Requested quantity is not available. Only {0} left."),
        ["max_quantity_exceeded"] = ("الحد الأقصى {0} قطعة من كل منتج في الطلب الواحد", "Maximum {0} units of each item per order."),
        ["identity_error"] = ("تعذر تنفيذ العملية: {0}", "The operation failed: {0}"),
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
