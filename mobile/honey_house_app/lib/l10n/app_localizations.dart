import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_ar.dart';
import 'app_localizations_en.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('ar'),
    Locale('en'),
  ];

  /// No description provided for @appTitle.
  ///
  /// In ar, this message translates to:
  /// **'بيت العسل'**
  String get appTitle;

  /// No description provided for @navHome.
  ///
  /// In ar, this message translates to:
  /// **'الرئيسية'**
  String get navHome;

  /// No description provided for @navShop.
  ///
  /// In ar, this message translates to:
  /// **'المتجر'**
  String get navShop;

  /// No description provided for @navCart.
  ///
  /// In ar, this message translates to:
  /// **'السلة'**
  String get navCart;

  /// No description provided for @navFavorites.
  ///
  /// In ar, this message translates to:
  /// **'المفضلة'**
  String get navFavorites;

  /// No description provided for @navAccount.
  ///
  /// In ar, this message translates to:
  /// **'حسابي'**
  String get navAccount;

  /// No description provided for @currency.
  ///
  /// In ar, this message translates to:
  /// **'ج.م'**
  String get currency;

  /// No description provided for @retry.
  ///
  /// In ar, this message translates to:
  /// **'إعادة المحاولة'**
  String get retry;

  /// No description provided for @errorGeneric.
  ///
  /// In ar, this message translates to:
  /// **'حدث خطأ غير متوقع، حاول مرة أخرى'**
  String get errorGeneric;

  /// No description provided for @errorNetwork.
  ///
  /// In ar, this message translates to:
  /// **'تعذر الاتصال بالخادم، تحقق من الإنترنت'**
  String get errorNetwork;

  /// No description provided for @sessionExpired.
  ///
  /// In ar, this message translates to:
  /// **'انتهت الجلسة، سجّل الدخول مرة أخرى'**
  String get sessionExpired;

  /// No description provided for @save.
  ///
  /// In ar, this message translates to:
  /// **'حفظ'**
  String get save;

  /// No description provided for @cancel.
  ///
  /// In ar, this message translates to:
  /// **'إلغاء'**
  String get cancel;

  /// No description provided for @delete.
  ///
  /// In ar, this message translates to:
  /// **'حذف'**
  String get delete;

  /// No description provided for @edit.
  ///
  /// In ar, this message translates to:
  /// **'تعديل'**
  String get edit;

  /// No description provided for @confirm.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد'**
  String get confirm;

  /// No description provided for @close.
  ///
  /// In ar, this message translates to:
  /// **'إغلاق'**
  String get close;

  /// No description provided for @seeAll.
  ///
  /// In ar, this message translates to:
  /// **'عرض الكل'**
  String get seeAll;

  /// No description provided for @required.
  ///
  /// In ar, this message translates to:
  /// **'هذا الحقل مطلوب'**
  String get required;

  /// No description provided for @loadMore.
  ///
  /// In ar, this message translates to:
  /// **'تحميل المزيد'**
  String get loadMore;

  /// No description provided for @noResults.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد نتائج'**
  String get noResults;

  /// No description provided for @language.
  ///
  /// In ar, this message translates to:
  /// **'اللغة'**
  String get language;

  /// No description provided for @arabic.
  ///
  /// In ar, this message translates to:
  /// **'العربية'**
  String get arabic;

  /// No description provided for @english.
  ///
  /// In ar, this message translates to:
  /// **'English'**
  String get english;

  /// No description provided for @login.
  ///
  /// In ar, this message translates to:
  /// **'تسجيل الدخول'**
  String get login;

  /// No description provided for @logout.
  ///
  /// In ar, this message translates to:
  /// **'تسجيل الخروج'**
  String get logout;

  /// No description provided for @register.
  ///
  /// In ar, this message translates to:
  /// **'إنشاء حساب'**
  String get register;

  /// No description provided for @email.
  ///
  /// In ar, this message translates to:
  /// **'البريد الإلكتروني'**
  String get email;

  /// No description provided for @password.
  ///
  /// In ar, this message translates to:
  /// **'كلمة المرور'**
  String get password;

  /// No description provided for @fullName.
  ///
  /// In ar, this message translates to:
  /// **'الاسم بالكامل'**
  String get fullName;

  /// No description provided for @phone.
  ///
  /// In ar, this message translates to:
  /// **'رقم الموبايل'**
  String get phone;

  /// No description provided for @phoneHint.
  ///
  /// In ar, this message translates to:
  /// **'01012345678'**
  String get phoneHint;

  /// No description provided for @emailInvalid.
  ///
  /// In ar, this message translates to:
  /// **'البريد الإلكتروني غير صحيح'**
  String get emailInvalid;

  /// No description provided for @phoneInvalid.
  ///
  /// In ar, this message translates to:
  /// **'رقم موبايل مصري غير صحيح'**
  String get phoneInvalid;

  /// No description provided for @passwordRule.
  ///
  /// In ar, this message translates to:
  /// **'8 أحرف على الأقل وتحتوي على حرف صغير ورقم'**
  String get passwordRule;

  /// No description provided for @noAccount.
  ///
  /// In ar, this message translates to:
  /// **'ليس لديك حساب؟'**
  String get noAccount;

  /// No description provided for @haveAccount.
  ///
  /// In ar, this message translates to:
  /// **'لديك حساب بالفعل؟'**
  String get haveAccount;

  /// No description provided for @forgotPassword.
  ///
  /// In ar, this message translates to:
  /// **'نسيت كلمة المرور؟'**
  String get forgotPassword;

  /// No description provided for @forgotPasswordTitle.
  ///
  /// In ar, this message translates to:
  /// **'استعادة كلمة المرور'**
  String get forgotPasswordTitle;

  /// No description provided for @forgotPasswordHint.
  ///
  /// In ar, this message translates to:
  /// **'أدخل بريدك وسنرسل لك رمز إعادة التعيين'**
  String get forgotPasswordHint;

  /// No description provided for @sendCode.
  ///
  /// In ar, this message translates to:
  /// **'إرسال الرمز'**
  String get sendCode;

  /// No description provided for @resetCode.
  ///
  /// In ar, this message translates to:
  /// **'رمز إعادة التعيين'**
  String get resetCode;

  /// No description provided for @newPassword.
  ///
  /// In ar, this message translates to:
  /// **'كلمة المرور الجديدة'**
  String get newPassword;

  /// No description provided for @currentPassword.
  ///
  /// In ar, this message translates to:
  /// **'كلمة المرور الحالية'**
  String get currentPassword;

  /// No description provided for @resetPasswordButton.
  ///
  /// In ar, this message translates to:
  /// **'تغيير كلمة المرور'**
  String get resetPasswordButton;

  /// No description provided for @resetCodeSent.
  ///
  /// In ar, this message translates to:
  /// **'لو البريد مسجل هيوصلك رمز خلال دقائق'**
  String get resetCodeSent;

  /// No description provided for @passwordChanged.
  ///
  /// In ar, this message translates to:
  /// **'تم تغيير كلمة المرور'**
  String get passwordChanged;

  /// No description provided for @loginToContinue.
  ///
  /// In ar, this message translates to:
  /// **'سجّل الدخول للمتابعة'**
  String get loginToContinue;

  /// No description provided for @welcomeBack.
  ///
  /// In ar, this message translates to:
  /// **'أهلًا بعودتك'**
  String get welcomeBack;

  /// No description provided for @welcomeUser.
  ///
  /// In ar, this message translates to:
  /// **'أهلًا {name}'**
  String welcomeUser(String name);

  /// No description provided for @guest.
  ///
  /// In ar, this message translates to:
  /// **'زائر'**
  String get guest;

  /// No description provided for @myProfile.
  ///
  /// In ar, this message translates to:
  /// **'بياناتي'**
  String get myProfile;

  /// No description provided for @profileSaved.
  ///
  /// In ar, this message translates to:
  /// **'تم حفظ بياناتك'**
  String get profileSaved;

  /// No description provided for @changePassword.
  ///
  /// In ar, this message translates to:
  /// **'تغيير كلمة المرور'**
  String get changePassword;

  /// No description provided for @myAddresses.
  ///
  /// In ar, this message translates to:
  /// **'عناويني'**
  String get myAddresses;

  /// No description provided for @myOrders.
  ///
  /// In ar, this message translates to:
  /// **'طلباتي'**
  String get myOrders;

  /// No description provided for @confirmLogout.
  ///
  /// In ar, this message translates to:
  /// **'هل تريد تسجيل الخروج؟'**
  String get confirmLogout;

  /// No description provided for @searchHint.
  ///
  /// In ar, this message translates to:
  /// **'ابحث عن عسل، شمع، غذاء ملكات...'**
  String get searchHint;

  /// No description provided for @categories.
  ///
  /// In ar, this message translates to:
  /// **'التصنيفات'**
  String get categories;

  /// No description provided for @featured.
  ///
  /// In ar, this message translates to:
  /// **'منتجات مميزة'**
  String get featured;

  /// No description provided for @allProducts.
  ///
  /// In ar, this message translates to:
  /// **'كل المنتجات'**
  String get allProducts;

  /// No description provided for @fromPrice.
  ///
  /// In ar, this message translates to:
  /// **'من {price}'**
  String fromPrice(String price);

  /// No description provided for @outOfStock.
  ///
  /// In ar, this message translates to:
  /// **'نفدت الكمية'**
  String get outOfStock;

  /// No description provided for @inStockOnly.
  ///
  /// In ar, this message translates to:
  /// **'المتوفر فقط'**
  String get inStockOnly;

  /// No description provided for @sortBy.
  ///
  /// In ar, this message translates to:
  /// **'الترتيب'**
  String get sortBy;

  /// No description provided for @sortNewest.
  ///
  /// In ar, this message translates to:
  /// **'الأحدث'**
  String get sortNewest;

  /// No description provided for @sortPriceAsc.
  ///
  /// In ar, this message translates to:
  /// **'السعر: من الأقل'**
  String get sortPriceAsc;

  /// No description provided for @sortPriceDesc.
  ///
  /// In ar, this message translates to:
  /// **'السعر: من الأعلى'**
  String get sortPriceDesc;

  /// No description provided for @sortRating.
  ///
  /// In ar, this message translates to:
  /// **'الأعلى تقييمًا'**
  String get sortRating;

  /// No description provided for @sortBestselling.
  ///
  /// In ar, this message translates to:
  /// **'الأكثر مبيعًا'**
  String get sortBestselling;

  /// No description provided for @filters.
  ///
  /// In ar, this message translates to:
  /// **'تصفية'**
  String get filters;

  /// No description provided for @minPrice.
  ///
  /// In ar, this message translates to:
  /// **'أقل سعر'**
  String get minPrice;

  /// No description provided for @maxPrice.
  ///
  /// In ar, this message translates to:
  /// **'أعلى سعر'**
  String get maxPrice;

  /// No description provided for @apply.
  ///
  /// In ar, this message translates to:
  /// **'تطبيق'**
  String get apply;

  /// No description provided for @clear.
  ///
  /// In ar, this message translates to:
  /// **'مسح'**
  String get clear;

  /// No description provided for @chooseWeight.
  ///
  /// In ar, this message translates to:
  /// **'اختر الوزن'**
  String get chooseWeight;

  /// No description provided for @quantity.
  ///
  /// In ar, this message translates to:
  /// **'الكمية'**
  String get quantity;

  /// No description provided for @addToCart.
  ///
  /// In ar, this message translates to:
  /// **'أضف إلى السلة'**
  String get addToCart;

  /// No description provided for @addedToCart.
  ///
  /// In ar, this message translates to:
  /// **'تمت الإضافة إلى السلة'**
  String get addedToCart;

  /// No description provided for @viewCart.
  ///
  /// In ar, this message translates to:
  /// **'عرض السلة'**
  String get viewCart;

  /// No description provided for @description.
  ///
  /// In ar, this message translates to:
  /// **'الوصف'**
  String get description;

  /// No description provided for @origin.
  ///
  /// In ar, this message translates to:
  /// **'المصدر: {origin}'**
  String origin(String origin);

  /// No description provided for @onlyLeft.
  ///
  /// In ar, this message translates to:
  /// **'متبقي {count} فقط'**
  String onlyLeft(int count);

  /// No description provided for @reviews.
  ///
  /// In ar, this message translates to:
  /// **'التقييمات'**
  String get reviews;

  /// No description provided for @reviewsCount.
  ///
  /// In ar, this message translates to:
  /// **'({count} تقييم)'**
  String reviewsCount(int count);

  /// No description provided for @noReviews.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد تقييمات بعد'**
  String get noReviews;

  /// No description provided for @verifiedPurchase.
  ///
  /// In ar, this message translates to:
  /// **'مشتري مؤكد'**
  String get verifiedPurchase;

  /// No description provided for @writeReview.
  ///
  /// In ar, this message translates to:
  /// **'اكتب تقييمك'**
  String get writeReview;

  /// No description provided for @editReview.
  ///
  /// In ar, this message translates to:
  /// **'عدّل تقييمك'**
  String get editReview;

  /// No description provided for @yourRating.
  ///
  /// In ar, this message translates to:
  /// **'تقييمك'**
  String get yourRating;

  /// No description provided for @yourComment.
  ///
  /// In ar, this message translates to:
  /// **'تعليقك (اختياري)'**
  String get yourComment;

  /// No description provided for @reviewPending.
  ///
  /// In ar, this message translates to:
  /// **'تقييمك قيد المراجعة وسيظهر بعد الموافقة'**
  String get reviewPending;

  /// No description provided for @reviewSubmitted.
  ///
  /// In ar, this message translates to:
  /// **'شكرًا! تم إرسال تقييمك للمراجعة'**
  String get reviewSubmitted;

  /// No description provided for @chooseRating.
  ///
  /// In ar, this message translates to:
  /// **'اختر عدد النجوم'**
  String get chooseRating;

  /// No description provided for @subcategories.
  ///
  /// In ar, this message translates to:
  /// **'الأقسام'**
  String get subcategories;

  /// No description provided for @cartEmpty.
  ///
  /// In ar, this message translates to:
  /// **'سلتك فاضية'**
  String get cartEmpty;

  /// No description provided for @startShopping.
  ///
  /// In ar, this message translates to:
  /// **'ابدأ التسوق'**
  String get startShopping;

  /// No description provided for @subtotal.
  ///
  /// In ar, this message translates to:
  /// **'المجموع الفرعي'**
  String get subtotal;

  /// No description provided for @checkout.
  ///
  /// In ar, this message translates to:
  /// **'إتمام الطلب'**
  String get checkout;

  /// No description provided for @itemUnavailable.
  ///
  /// In ar, this message translates to:
  /// **'غير متاح بالكمية المطلوبة (المتاح {count})'**
  String itemUnavailable(int count);

  /// No description provided for @cartHasUnavailable.
  ///
  /// In ar, this message translates to:
  /// **'بعض المنتجات غير متاحة، عدّل الكمية أو احذفها'**
  String get cartHasUnavailable;

  /// No description provided for @removeItem.
  ///
  /// In ar, this message translates to:
  /// **'حذف من السلة'**
  String get removeItem;

  /// No description provided for @itemsCount.
  ///
  /// In ar, this message translates to:
  /// **'{count} منتج'**
  String itemsCount(int count);

  /// No description provided for @favoritesEmpty.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد منتجات في المفضلة'**
  String get favoritesEmpty;

  /// No description provided for @addedToFavorites.
  ///
  /// In ar, this message translates to:
  /// **'أضيف للمفضلة'**
  String get addedToFavorites;

  /// No description provided for @removedFromFavorites.
  ///
  /// In ar, this message translates to:
  /// **'أزيل من المفضلة'**
  String get removedFromFavorites;

  /// No description provided for @addAddress.
  ///
  /// In ar, this message translates to:
  /// **'إضافة عنوان'**
  String get addAddress;

  /// No description provided for @editAddress.
  ///
  /// In ar, this message translates to:
  /// **'تعديل العنوان'**
  String get editAddress;

  /// No description provided for @noAddresses.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد عناوين محفوظة'**
  String get noAddresses;

  /// No description provided for @recipientName.
  ///
  /// In ar, this message translates to:
  /// **'اسم المستلم'**
  String get recipientName;

  /// No description provided for @governorate.
  ///
  /// In ar, this message translates to:
  /// **'المحافظة'**
  String get governorate;

  /// No description provided for @city.
  ///
  /// In ar, this message translates to:
  /// **'المدينة / المركز'**
  String get city;

  /// No description provided for @area.
  ///
  /// In ar, this message translates to:
  /// **'المنطقة / الحي'**
  String get area;

  /// No description provided for @street.
  ///
  /// In ar, this message translates to:
  /// **'الشارع'**
  String get street;

  /// No description provided for @building.
  ///
  /// In ar, this message translates to:
  /// **'العمارة'**
  String get building;

  /// No description provided for @floor.
  ///
  /// In ar, this message translates to:
  /// **'الدور'**
  String get floor;

  /// No description provided for @apartment.
  ///
  /// In ar, this message translates to:
  /// **'الشقة'**
  String get apartment;

  /// No description provided for @landmark.
  ///
  /// In ar, this message translates to:
  /// **'علامة مميزة'**
  String get landmark;

  /// No description provided for @setDefault.
  ///
  /// In ar, this message translates to:
  /// **'اجعله العنوان الافتراضي'**
  String get setDefault;

  /// No description provided for @defaultAddress.
  ///
  /// In ar, this message translates to:
  /// **'الافتراضي'**
  String get defaultAddress;

  /// No description provided for @shippingFeeValue.
  ///
  /// In ar, this message translates to:
  /// **'الشحن: {fee}'**
  String shippingFeeValue(String fee);

  /// No description provided for @confirmDeleteAddress.
  ///
  /// In ar, this message translates to:
  /// **'حذف هذا العنوان؟'**
  String get confirmDeleteAddress;

  /// No description provided for @deliveryAddress.
  ///
  /// In ar, this message translates to:
  /// **'عنوان التوصيل'**
  String get deliveryAddress;

  /// No description provided for @change.
  ///
  /// In ar, this message translates to:
  /// **'تغيير'**
  String get change;

  /// No description provided for @paymentMethod.
  ///
  /// In ar, this message translates to:
  /// **'طريقة الدفع'**
  String get paymentMethod;

  /// No description provided for @cashOnDelivery.
  ///
  /// In ar, this message translates to:
  /// **'الدفع عند الاستلام'**
  String get cashOnDelivery;

  /// No description provided for @onlinePayment.
  ///
  /// In ar, this message translates to:
  /// **'الدفع بالبطاقة / المحفظة'**
  String get onlinePayment;

  /// No description provided for @couponCode.
  ///
  /// In ar, this message translates to:
  /// **'كود الخصم'**
  String get couponCode;

  /// No description provided for @applyCoupon.
  ///
  /// In ar, this message translates to:
  /// **'تطبيق'**
  String get applyCoupon;

  /// No description provided for @removeCoupon.
  ///
  /// In ar, this message translates to:
  /// **'إزالة الكود'**
  String get removeCoupon;

  /// No description provided for @discount.
  ///
  /// In ar, this message translates to:
  /// **'الخصم'**
  String get discount;

  /// No description provided for @shipping.
  ///
  /// In ar, this message translates to:
  /// **'الشحن'**
  String get shipping;

  /// No description provided for @total.
  ///
  /// In ar, this message translates to:
  /// **'الإجمالي'**
  String get total;

  /// No description provided for @orderNotes.
  ///
  /// In ar, this message translates to:
  /// **'ملاحظات للطلب (اختياري)'**
  String get orderNotes;

  /// No description provided for @placeOrder.
  ///
  /// In ar, this message translates to:
  /// **'تأكيد الطلب'**
  String get placeOrder;

  /// No description provided for @orderPlaced.
  ///
  /// In ar, this message translates to:
  /// **'تم استلام طلبك 🎉'**
  String get orderPlaced;

  /// No description provided for @orderPlacedText.
  ///
  /// In ar, this message translates to:
  /// **'رقم طلبك {number}. هنتواصل معاك لتأكيد الطلب.'**
  String orderPlacedText(String number);

  /// No description provided for @trackOrder.
  ///
  /// In ar, this message translates to:
  /// **'تتبع الطلب'**
  String get trackOrder;

  /// No description provided for @continueShopping.
  ///
  /// In ar, this message translates to:
  /// **'متابعة التسوق'**
  String get continueShopping;

  /// No description provided for @orderSummary.
  ///
  /// In ar, this message translates to:
  /// **'ملخص الطلب'**
  String get orderSummary;

  /// No description provided for @payNow.
  ///
  /// In ar, this message translates to:
  /// **'ادفع الآن'**
  String get payNow;

  /// No description provided for @paymentTitle.
  ///
  /// In ar, this message translates to:
  /// **'الدفع الإلكتروني'**
  String get paymentTitle;

  /// No description provided for @paymentDone.
  ///
  /// In ar, this message translates to:
  /// **'انتهيت من الدفع'**
  String get paymentDone;

  /// No description provided for @paymentPendingNote.
  ///
  /// In ar, this message translates to:
  /// **'ادفع خلال 30 دقيقة وإلا سيتم إلغاء الطلب تلقائيًا'**
  String get paymentPendingNote;

  /// No description provided for @paymentSuccess.
  ///
  /// In ar, this message translates to:
  /// **'تم الدفع بنجاح'**
  String get paymentSuccess;

  /// No description provided for @paymentFailed.
  ///
  /// In ar, this message translates to:
  /// **'لم تكتمل عملية الدفع'**
  String get paymentFailed;

  /// No description provided for @addAddressFirst.
  ///
  /// In ar, this message translates to:
  /// **'أضف عنوان توصيل أولًا'**
  String get addAddressFirst;

  /// No description provided for @noOrders.
  ///
  /// In ar, this message translates to:
  /// **'لا توجد طلبات بعد'**
  String get noOrders;

  /// No description provided for @orderNumber.
  ///
  /// In ar, this message translates to:
  /// **'طلب {number}'**
  String orderNumber(String number);

  /// No description provided for @orderDate.
  ///
  /// In ar, this message translates to:
  /// **'تاريخ الطلب'**
  String get orderDate;

  /// No description provided for @orderItems.
  ///
  /// In ar, this message translates to:
  /// **'المنتجات'**
  String get orderItems;

  /// No description provided for @orderStatus.
  ///
  /// In ar, this message translates to:
  /// **'حالة الطلب'**
  String get orderStatus;

  /// No description provided for @trackingNumber.
  ///
  /// In ar, this message translates to:
  /// **'رقم الشحنة: {number}'**
  String trackingNumber(String number);

  /// No description provided for @cancelOrder.
  ///
  /// In ar, this message translates to:
  /// **'إلغاء الطلب'**
  String get cancelOrder;

  /// No description provided for @cancelReason.
  ///
  /// In ar, this message translates to:
  /// **'سبب الإلغاء (اختياري)'**
  String get cancelReason;

  /// No description provided for @orderCancelled.
  ///
  /// In ar, this message translates to:
  /// **'تم إلغاء الطلب'**
  String get orderCancelled;

  /// No description provided for @rateProduct.
  ///
  /// In ar, this message translates to:
  /// **'قيّم المنتج'**
  String get rateProduct;

  /// No description provided for @shipTo.
  ///
  /// In ar, this message translates to:
  /// **'التوصيل إلى'**
  String get shipTo;

  /// No description provided for @payment.
  ///
  /// In ar, this message translates to:
  /// **'الدفع'**
  String get payment;

  /// No description provided for @all.
  ///
  /// In ar, this message translates to:
  /// **'الكل'**
  String get all;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['ar', 'en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'ar':
      return AppLocalizationsAr();
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
