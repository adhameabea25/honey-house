// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Arabic (`ar`).
class AppLocalizationsAr extends AppLocalizations {
  AppLocalizationsAr([String locale = 'ar']) : super(locale);

  @override
  String get appTitle => 'بيت العسل';

  @override
  String get navHome => 'الرئيسية';

  @override
  String get navShop => 'المتجر';

  @override
  String get navCart => 'السلة';

  @override
  String get navFavorites => 'المفضلة';

  @override
  String get navAccount => 'حسابي';

  @override
  String get currency => 'ج.م';

  @override
  String get retry => 'إعادة المحاولة';

  @override
  String get errorGeneric => 'حدث خطأ غير متوقع، حاول مرة أخرى';

  @override
  String get errorNetwork => 'تعذر الاتصال بالخادم، تحقق من الإنترنت';

  @override
  String get sessionExpired => 'انتهت الجلسة، سجّل الدخول مرة أخرى';

  @override
  String get save => 'حفظ';

  @override
  String get cancel => 'إلغاء';

  @override
  String get delete => 'حذف';

  @override
  String get edit => 'تعديل';

  @override
  String get confirm => 'تأكيد';

  @override
  String get close => 'إغلاق';

  @override
  String get seeAll => 'عرض الكل';

  @override
  String get required => 'هذا الحقل مطلوب';

  @override
  String get loadMore => 'تحميل المزيد';

  @override
  String get noResults => 'لا توجد نتائج';

  @override
  String get language => 'اللغة';

  @override
  String get arabic => 'العربية';

  @override
  String get english => 'English';

  @override
  String get login => 'تسجيل الدخول';

  @override
  String get logout => 'تسجيل الخروج';

  @override
  String get register => 'إنشاء حساب';

  @override
  String get email => 'البريد الإلكتروني';

  @override
  String get password => 'كلمة المرور';

  @override
  String get fullName => 'الاسم بالكامل';

  @override
  String get phone => 'رقم الموبايل';

  @override
  String get phoneHint => '01012345678';

  @override
  String get emailInvalid => 'البريد الإلكتروني غير صحيح';

  @override
  String get phoneInvalid => 'رقم موبايل مصري غير صحيح';

  @override
  String get passwordRule => '8 أحرف على الأقل وتحتوي على حرف صغير ورقم';

  @override
  String get noAccount => 'ليس لديك حساب؟';

  @override
  String get haveAccount => 'لديك حساب بالفعل؟';

  @override
  String get forgotPassword => 'نسيت كلمة المرور؟';

  @override
  String get forgotPasswordTitle => 'استعادة كلمة المرور';

  @override
  String get forgotPasswordHint => 'أدخل بريدك وسنرسل لك رمز إعادة التعيين';

  @override
  String get sendCode => 'إرسال الرمز';

  @override
  String get resetCode => 'رمز إعادة التعيين';

  @override
  String get newPassword => 'كلمة المرور الجديدة';

  @override
  String get currentPassword => 'كلمة المرور الحالية';

  @override
  String get resetPasswordButton => 'تغيير كلمة المرور';

  @override
  String get resetCodeSent => 'لو البريد مسجل هيوصلك رمز خلال دقائق';

  @override
  String get passwordChanged => 'تم تغيير كلمة المرور';

  @override
  String get loginToContinue => 'سجّل الدخول للمتابعة';

  @override
  String get welcomeBack => 'أهلًا بعودتك';

  @override
  String welcomeUser(String name) {
    return 'أهلًا $name';
  }

  @override
  String get guest => 'زائر';

  @override
  String get myProfile => 'بياناتي';

  @override
  String get profileSaved => 'تم حفظ بياناتك';

  @override
  String get changePassword => 'تغيير كلمة المرور';

  @override
  String get myAddresses => 'عناويني';

  @override
  String get myOrders => 'طلباتي';

  @override
  String get confirmLogout => 'هل تريد تسجيل الخروج؟';

  @override
  String get searchHint => 'ابحث عن عسل، شمع، غذاء ملكات...';

  @override
  String get categories => 'التصنيفات';

  @override
  String get featured => 'منتجات مميزة';

  @override
  String get allProducts => 'كل المنتجات';

  @override
  String fromPrice(String price) {
    return 'من $price';
  }

  @override
  String get outOfStock => 'نفدت الكمية';

  @override
  String get inStockOnly => 'المتوفر فقط';

  @override
  String get sortBy => 'الترتيب';

  @override
  String get sortNewest => 'الأحدث';

  @override
  String get sortPriceAsc => 'السعر: من الأقل';

  @override
  String get sortPriceDesc => 'السعر: من الأعلى';

  @override
  String get sortRating => 'الأعلى تقييمًا';

  @override
  String get sortBestselling => 'الأكثر مبيعًا';

  @override
  String get filters => 'تصفية';

  @override
  String get minPrice => 'أقل سعر';

  @override
  String get maxPrice => 'أعلى سعر';

  @override
  String get apply => 'تطبيق';

  @override
  String get clear => 'مسح';

  @override
  String get chooseWeight => 'اختر الوزن';

  @override
  String get quantity => 'الكمية';

  @override
  String get addToCart => 'أضف إلى السلة';

  @override
  String get addedToCart => 'تمت الإضافة إلى السلة';

  @override
  String get viewCart => 'عرض السلة';

  @override
  String get description => 'الوصف';

  @override
  String origin(String origin) {
    return 'المصدر: $origin';
  }

  @override
  String onlyLeft(int count) {
    return 'متبقي $count فقط';
  }

  @override
  String get reviews => 'التقييمات';

  @override
  String reviewsCount(int count) {
    return '($count تقييم)';
  }

  @override
  String get noReviews => 'لا توجد تقييمات بعد';

  @override
  String get verifiedPurchase => 'مشتري مؤكد';

  @override
  String get writeReview => 'اكتب تقييمك';

  @override
  String get editReview => 'عدّل تقييمك';

  @override
  String get yourRating => 'تقييمك';

  @override
  String get yourComment => 'تعليقك (اختياري)';

  @override
  String get reviewPending => 'تقييمك قيد المراجعة وسيظهر بعد الموافقة';

  @override
  String get reviewSubmitted => 'شكرًا! تم إرسال تقييمك للمراجعة';

  @override
  String get chooseRating => 'اختر عدد النجوم';

  @override
  String get subcategories => 'الأقسام';

  @override
  String get cartEmpty => 'سلتك فاضية';

  @override
  String get startShopping => 'ابدأ التسوق';

  @override
  String get subtotal => 'المجموع الفرعي';

  @override
  String get checkout => 'إتمام الطلب';

  @override
  String itemUnavailable(int count) {
    return 'غير متاح بالكمية المطلوبة (المتاح $count)';
  }

  @override
  String get cartHasUnavailable =>
      'بعض المنتجات غير متاحة، عدّل الكمية أو احذفها';

  @override
  String get removeItem => 'حذف من السلة';

  @override
  String itemsCount(int count) {
    return '$count منتج';
  }

  @override
  String get favoritesEmpty => 'لا توجد منتجات في المفضلة';

  @override
  String get addedToFavorites => 'أضيف للمفضلة';

  @override
  String get removedFromFavorites => 'أزيل من المفضلة';

  @override
  String get addAddress => 'إضافة عنوان';

  @override
  String get editAddress => 'تعديل العنوان';

  @override
  String get noAddresses => 'لا توجد عناوين محفوظة';

  @override
  String get recipientName => 'اسم المستلم';

  @override
  String get governorate => 'المحافظة';

  @override
  String get city => 'المدينة / المركز';

  @override
  String get area => 'المنطقة / الحي';

  @override
  String get street => 'الشارع';

  @override
  String get building => 'العمارة';

  @override
  String get floor => 'الدور';

  @override
  String get apartment => 'الشقة';

  @override
  String get landmark => 'علامة مميزة';

  @override
  String get setDefault => 'اجعله العنوان الافتراضي';

  @override
  String get defaultAddress => 'الافتراضي';

  @override
  String shippingFeeValue(String fee) {
    return 'الشحن: $fee';
  }

  @override
  String get confirmDeleteAddress => 'حذف هذا العنوان؟';

  @override
  String get deliveryAddress => 'عنوان التوصيل';

  @override
  String get change => 'تغيير';

  @override
  String get paymentMethod => 'طريقة الدفع';

  @override
  String get cashOnDelivery => 'الدفع عند الاستلام';

  @override
  String get onlinePayment => 'الدفع بالبطاقة / المحفظة';

  @override
  String get couponCode => 'كود الخصم';

  @override
  String get applyCoupon => 'تطبيق';

  @override
  String get removeCoupon => 'إزالة الكود';

  @override
  String get discount => 'الخصم';

  @override
  String get shipping => 'الشحن';

  @override
  String get total => 'الإجمالي';

  @override
  String get orderNotes => 'ملاحظات للطلب (اختياري)';

  @override
  String get placeOrder => 'تأكيد الطلب';

  @override
  String get orderPlaced => 'تم استلام طلبك 🎉';

  @override
  String orderPlacedText(String number) {
    return 'رقم طلبك $number. هنتواصل معاك لتأكيد الطلب.';
  }

  @override
  String get trackOrder => 'تتبع الطلب';

  @override
  String get continueShopping => 'متابعة التسوق';

  @override
  String get orderSummary => 'ملخص الطلب';

  @override
  String get payNow => 'ادفع الآن';

  @override
  String get paymentTitle => 'الدفع الإلكتروني';

  @override
  String get paymentDone => 'انتهيت من الدفع';

  @override
  String get paymentPendingNote =>
      'ادفع خلال 30 دقيقة وإلا سيتم إلغاء الطلب تلقائيًا';

  @override
  String get paymentSuccess => 'تم الدفع بنجاح';

  @override
  String get paymentFailed => 'لم تكتمل عملية الدفع';

  @override
  String get addAddressFirst => 'أضف عنوان توصيل أولًا';

  @override
  String get noOrders => 'لا توجد طلبات بعد';

  @override
  String orderNumber(String number) {
    return 'طلب $number';
  }

  @override
  String get orderDate => 'تاريخ الطلب';

  @override
  String get orderItems => 'المنتجات';

  @override
  String get orderStatus => 'حالة الطلب';

  @override
  String trackingNumber(String number) {
    return 'رقم الشحنة: $number';
  }

  @override
  String get cancelOrder => 'إلغاء الطلب';

  @override
  String get cancelReason => 'سبب الإلغاء (اختياري)';

  @override
  String get orderCancelled => 'تم إلغاء الطلب';

  @override
  String get rateProduct => 'قيّم المنتج';

  @override
  String get shipTo => 'التوصيل إلى';

  @override
  String get payment => 'الدفع';

  @override
  String get all => 'الكل';
}
