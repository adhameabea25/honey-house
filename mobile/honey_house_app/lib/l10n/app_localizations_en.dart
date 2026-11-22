// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'Honey House';

  @override
  String get navHome => 'Home';

  @override
  String get navShop => 'Shop';

  @override
  String get navCart => 'Cart';

  @override
  String get navFavorites => 'Favorites';

  @override
  String get navAccount => 'Account';

  @override
  String get currency => 'EGP';

  @override
  String get retry => 'Retry';

  @override
  String get errorGeneric => 'Something went wrong. Please try again.';

  @override
  String get errorNetwork => 'Cannot reach the server. Check your connection.';

  @override
  String get sessionExpired =>
      'Your session has expired. Please sign in again.';

  @override
  String get save => 'Save';

  @override
  String get cancel => 'Cancel';

  @override
  String get delete => 'Delete';

  @override
  String get edit => 'Edit';

  @override
  String get confirm => 'Confirm';

  @override
  String get close => 'Close';

  @override
  String get seeAll => 'See all';

  @override
  String get required => 'This field is required';

  @override
  String get loadMore => 'Load more';

  @override
  String get noResults => 'No results';

  @override
  String get language => 'Language';

  @override
  String get arabic => 'العربية';

  @override
  String get english => 'English';

  @override
  String get login => 'Sign in';

  @override
  String get logout => 'Sign out';

  @override
  String get register => 'Create account';

  @override
  String get email => 'Email';

  @override
  String get password => 'Password';

  @override
  String get fullName => 'Full name';

  @override
  String get phone => 'Mobile number';

  @override
  String get phoneHint => '01012345678';

  @override
  String get emailInvalid => 'Invalid email address';

  @override
  String get phoneInvalid => 'Invalid Egyptian mobile number';

  @override
  String get passwordRule =>
      'At least 8 characters with a lowercase letter and a digit';

  @override
  String get noAccount => 'Don\'t have an account?';

  @override
  String get haveAccount => 'Already have an account?';

  @override
  String get forgotPassword => 'Forgot password?';

  @override
  String get forgotPasswordTitle => 'Reset password';

  @override
  String get forgotPasswordHint =>
      'Enter your email and we\'ll send you a reset code';

  @override
  String get sendCode => 'Send code';

  @override
  String get resetCode => 'Reset code';

  @override
  String get newPassword => 'New password';

  @override
  String get currentPassword => 'Current password';

  @override
  String get resetPasswordButton => 'Change password';

  @override
  String get resetCodeSent =>
      'If the email is registered, a code is on its way';

  @override
  String get passwordChanged => 'Password changed';

  @override
  String get loginToContinue => 'Sign in to continue';

  @override
  String get welcomeBack => 'Welcome back';

  @override
  String welcomeUser(String name) {
    return 'Hi $name';
  }

  @override
  String get guest => 'Guest';

  @override
  String get myProfile => 'My details';

  @override
  String get profileSaved => 'Your details were saved';

  @override
  String get changePassword => 'Change password';

  @override
  String get myAddresses => 'My addresses';

  @override
  String get myOrders => 'My orders';

  @override
  String get confirmLogout => 'Sign out?';

  @override
  String get searchHint => 'Search honey, beeswax, royal jelly...';

  @override
  String get categories => 'Categories';

  @override
  String get featured => 'Featured';

  @override
  String get allProducts => 'All products';

  @override
  String fromPrice(String price) {
    return 'From $price';
  }

  @override
  String get outOfStock => 'Out of stock';

  @override
  String get inStockOnly => 'In stock only';

  @override
  String get sortBy => 'Sort';

  @override
  String get sortNewest => 'Newest';

  @override
  String get sortPriceAsc => 'Price: low to high';

  @override
  String get sortPriceDesc => 'Price: high to low';

  @override
  String get sortRating => 'Top rated';

  @override
  String get sortBestselling => 'Best selling';

  @override
  String get filters => 'Filters';

  @override
  String get minPrice => 'Min price';

  @override
  String get maxPrice => 'Max price';

  @override
  String get apply => 'Apply';

  @override
  String get clear => 'Clear';

  @override
  String get chooseWeight => 'Choose weight';

  @override
  String get quantity => 'Quantity';

  @override
  String get addToCart => 'Add to cart';

  @override
  String get addedToCart => 'Added to cart';

  @override
  String get viewCart => 'View cart';

  @override
  String get description => 'Description';

  @override
  String origin(String origin) {
    return 'Origin: $origin';
  }

  @override
  String onlyLeft(int count) {
    return 'Only $count left';
  }

  @override
  String get reviews => 'Reviews';

  @override
  String reviewsCount(int count) {
    return '($count reviews)';
  }

  @override
  String get noReviews => 'No reviews yet';

  @override
  String get verifiedPurchase => 'Verified purchase';

  @override
  String get writeReview => 'Write a review';

  @override
  String get editReview => 'Edit your review';

  @override
  String get yourRating => 'Your rating';

  @override
  String get yourComment => 'Your comment (optional)';

  @override
  String get reviewPending => 'Your review is awaiting approval';

  @override
  String get reviewSubmitted => 'Thanks! Your review was sent for approval';

  @override
  String get chooseRating => 'Choose a star rating';

  @override
  String get subcategories => 'Sections';

  @override
  String get cartEmpty => 'Your cart is empty';

  @override
  String get startShopping => 'Start shopping';

  @override
  String get subtotal => 'Subtotal';

  @override
  String get checkout => 'Checkout';

  @override
  String itemUnavailable(int count) {
    return 'Not available in this quantity ($count left)';
  }

  @override
  String get cartHasUnavailable =>
      'Some items are unavailable. Change or remove them.';

  @override
  String get removeItem => 'Remove from cart';

  @override
  String itemsCount(int count) {
    return '$count items';
  }

  @override
  String get favoritesEmpty => 'No favorites yet';

  @override
  String get addedToFavorites => 'Added to favorites';

  @override
  String get removedFromFavorites => 'Removed from favorites';

  @override
  String get addAddress => 'Add address';

  @override
  String get editAddress => 'Edit address';

  @override
  String get noAddresses => 'No saved addresses';

  @override
  String get recipientName => 'Recipient name';

  @override
  String get governorate => 'Governorate';

  @override
  String get city => 'City';

  @override
  String get area => 'Area';

  @override
  String get street => 'Street';

  @override
  String get building => 'Building';

  @override
  String get floor => 'Floor';

  @override
  String get apartment => 'Apartment';

  @override
  String get landmark => 'Landmark';

  @override
  String get setDefault => 'Make default';

  @override
  String get defaultAddress => 'Default';

  @override
  String shippingFeeValue(String fee) {
    return 'Shipping: $fee';
  }

  @override
  String get confirmDeleteAddress => 'Delete this address?';

  @override
  String get deliveryAddress => 'Delivery address';

  @override
  String get change => 'Change';

  @override
  String get paymentMethod => 'Payment method';

  @override
  String get cashOnDelivery => 'Cash on delivery';

  @override
  String get onlinePayment => 'Card / wallet';

  @override
  String get couponCode => 'Coupon code';

  @override
  String get applyCoupon => 'Apply';

  @override
  String get removeCoupon => 'Remove code';

  @override
  String get discount => 'Discount';

  @override
  String get shipping => 'Shipping';

  @override
  String get total => 'Total';

  @override
  String get orderNotes => 'Order notes (optional)';

  @override
  String get placeOrder => 'Place order';

  @override
  String get orderPlaced => 'Order placed 🎉';

  @override
  String orderPlacedText(String number) {
    return 'Your order number is $number. We\'ll contact you to confirm it.';
  }

  @override
  String get trackOrder => 'Track order';

  @override
  String get continueShopping => 'Continue shopping';

  @override
  String get orderSummary => 'Order summary';

  @override
  String get payNow => 'Pay now';

  @override
  String get paymentTitle => 'Online payment';

  @override
  String get paymentDone => 'I\'ve finished paying';

  @override
  String get paymentPendingNote =>
      'Pay within 30 minutes or the order is cancelled automatically';

  @override
  String get paymentSuccess => 'Payment successful';

  @override
  String get paymentFailed => 'The payment was not completed';

  @override
  String get addAddressFirst => 'Add a delivery address first';

  @override
  String get noOrders => 'No orders yet';

  @override
  String orderNumber(String number) {
    return 'Order $number';
  }

  @override
  String get orderDate => 'Order date';

  @override
  String get orderItems => 'Items';

  @override
  String get orderStatus => 'Status';

  @override
  String trackingNumber(String number) {
    return 'Tracking number: $number';
  }

  @override
  String get cancelOrder => 'Cancel order';

  @override
  String get cancelReason => 'Reason (optional)';

  @override
  String get orderCancelled => 'Order cancelled';

  @override
  String get rateProduct => 'Rate product';

  @override
  String get shipTo => 'Deliver to';

  @override
  String get payment => 'Payment';

  @override
  String get all => 'All';
}
