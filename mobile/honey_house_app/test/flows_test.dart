import 'package:flutter_test/flutter_test.dart';
import 'package:honey_house/core/providers.dart';
import 'package:honey_house/core/router.dart';
import 'package:material_ui/material_ui.dart';

import 'helpers.dart';

Map<String, dynamic> cartJson(List<Map<String, dynamic>> items) => {
      'items': items,
      'itemCount': items.fold<int>(0, (s, i) => s + (i['quantity'] as int)),
      'subtotal': items.fold<double>(0, (s, i) => s + (i['lineTotal'] as num)),
      'currency': 'EGP',
      'hasUnavailableItems': false,
    };

Map<String, dynamic> cartItem(int qty) => {
      'id': 100, 'variantId': 12, 'productId': 1, 'productName': 'عسل سدر', 'variantLabel': '500 جم', 'unitPrice': 180,
      'quantity': qty, 'lineTotal': 180 * qty, 'isAvailable': true, 'availableQuantity': 50,
    };

final address = {
  'id': 7, 'governorateId': 1, 'governorateName': 'القاهرة', 'shippingFee': 50, 'recipientName': 'أدهم', 'phone': '01012345678',
  'city': 'مدينة نصر', 'street': 'شارع عباس العقاد', 'isDefault': true,
};

Map<String, dynamic> orderJson({String status = 'Pending', bool canCancel = true}) => {
      'id': 42, 'orderNumber': 'HH-20261122-0042', 'status': status, 'statusName': status == 'Pending' ? 'قيد المراجعة' : 'ملغي',
      'paymentMethod': 'CashOnDelivery', 'paymentStatus': 'Pending', 'paymentStatusName': 'في انتظار الدفع',
      'subtotal': 360, 'discountAmount': 36, 'shippingFee': 50, 'total': 374, 'currency': 'EGP', 'couponCode': 'HONEY10',
      'createdAt': '2026-11-22T08:00:00', 'canCancel': canCancel,
      'shippingAddress': {'recipientName': 'أدهم', 'phone': '01012345678', 'governorate': 'القاهرة', 'city': 'مدينة نصر', 'addressLine': 'شارع عباس العقاد'},
      'items': [{'productId': 1, 'productName': 'عسل سدر', 'variantLabel': '500 جم', 'unitPrice': 180, 'quantity': 2, 'lineTotal': 360}],
      'history': [{'status': 'Pending', 'statusName': 'قيد المراجعة', 'changedAt': '2026-11-22T08:00:00'}],
    };

Map<String, Handler> signedInRoutes({List<Map<String, dynamic>>? cart}) {
  var items = cart ?? <Map<String, dynamic>>[];
  return {
    ...catalogRoutes(),
    'GET /auth/me': (_) => ok(user),
    'GET /cart': (_) => ok(cartJson(items)),
    'POST /cart/items': (c) {
      final b = c.body as Map;
      items = [cartItem(b['quantity'] as int)];
      return ok(cartJson(items));
    },
    'PUT /cart/items/100': (c) {
      items = [cartItem((c.body as Map)['quantity'] as int)];
      return ok(cartJson(items));
    },
    'GET /favorites/ids': (_) => ok([]),
    'GET /favorites': (_) => ok([]),
    'GET /products/1/reviews/mine': (_) => (404, {'title': 'none'}),
    'GET /addresses': (_) => ok([address]),
    'GET /governorates': (_) => ok([{'id': 1, 'name': 'القاهرة', 'shippingFee': 50}]),
    'POST /orders/checkout/preview': (c) {
      final coupon = (c.body as Map)['couponCode'];
      final discount = coupon == 'HONEY10' ? 36 : 0;
      return ok({
        'items': [{'productName': 'عسل سدر', 'variantLabel': '500 جم', 'unitPrice': 180, 'quantity': 2, 'lineTotal': 360}],
        'subtotal': 360, 'discountAmount': discount, 'shippingFee': 50, 'total': 410 - discount, 'currency': 'EGP',
        'couponCode': discount > 0 ? coupon : null, 'couponError': coupon != null && discount == 0 ? 'كود غير صالح' : null,
        'canPlaceOrder': true,
      });
    },
    'POST /orders': (_) => ok({'order': orderJson(), 'paymentUrl': null}),
    'GET /orders/42': (_) => ok(orderJson()),
    'GET /orders': (_) => ok(paged([orderJson()])),
    'POST /orders/42/cancel': (_) => ok(orderJson(status: 'Cancelled', canCancel: false)),
  };
}

void main() {
  testWidgets('guest sees a sign-in prompt on the cart tab and can sign in', (tester) async {
    final api = FakeApi({
      ...signedInRoutes(),
      'POST /auth/login': (c) => ok({'accessToken': 'a1', 'refreshToken': 'r1', 'user': user}),
    });
    final container = await pumpApp(tester, api);
    await tester.tap(find.text('السلة'));
    await settle(tester);
    expect(find.text('سجّل الدخول للمتابعة'), findsOneWidget);

    await tester.tap(find.widgetWithText(FilledButton, 'تسجيل الدخول'));
    await settle(tester);
    await tester.enterText(find.byType(TextFormField).at(0), 'a@b.com');
    await tester.enterText(find.byType(TextFormField).at(1), 'Secret123');
    await tester.tap(find.widgetWithText(FilledButton, 'تسجيل الدخول'));
    await settle(tester);

    final login = api.calls.firstWhere((c) => c.path == '/auth/login');
    expect((login.body as Map)['email'], 'a@b.com');
    expect((await container.read(tokenStorageProvider).read())!.accessToken, 'a1');
    // Back on the cart tab, now signed in with an empty cart.
    expect(find.text('سلتك فاضية'), findsOneWidget);
  });

  testWidgets('switching to English changes texts and the API language', (tester) async {
    final api = FakeApi(signedInRoutes());
    await pumpApp(tester, api, signedIn: true);
    await tester.tap(find.text('حسابي'));
    await settle(tester);
    await tester.tap(find.text('English'));
    await settle(tester);
    expect(find.text('Account'), findsWidgets);
    expect(api.calls.last.headers['Accept-Language'], 'en');
  });

  testWidgets('product page: choose a weight and quantity, then add to cart', (tester) async {
    final api = FakeApi(signedInRoutes());
    final container = await pumpApp(tester, api, signedIn: true);
    container.read(routerProvider).push('/product/1');
    await settle(tester);
    expect(find.text('المصدر: سيناء'), findsOneWidget);
    expect(find.text('متبقي 3 فقط'), findsOneWidget);

    await tester.tap(find.text('500 جم'));
    await settle(tester);
    await tester.tap(find.byIcon(Icons.add));
    await settle(tester);
    await tester.tap(find.byIcon(Icons.add_shopping_cart));
    await settle(tester);

    final add = api.calls.lastWhere((c) => c.path == '/cart/items');
    expect(add.body, {'variantId': 12, 'quantity': 2});
    expect(find.text('تمت الإضافة إلى السلة'), findsOneWidget);
  });

  testWidgets('checkout with a coupon and cash on delivery, then cancel the order', (tester) async {
    final api = FakeApi(signedInRoutes(cart: [cartItem(2)]));
    await pumpApp(tester, api, signedIn: true);
    await tester.tap(find.text('السلة'));
    await settle(tester);
    expect(find.text('360 ج.م'), findsWidgets);
    await tester.tap(find.text('إتمام الطلب'));
    await settle(tester);

    expect(find.text('أدهم'), findsOneWidget);
    expect(find.text('410 ج.م'), findsWidgets);
    await tester.enterText(find.widgetWithText(TextField, 'كود الخصم'), 'HONEY10');
    await tester.tap(find.widgetWithText(OutlinedButton, 'تطبيق'));
    await settle(tester);
    expect(find.text('- 36 ج.م'), findsOneWidget);

    await tester.ensureVisible(find.textContaining('تأكيد الطلب'));
    await tester.pumpAndSettle();
    await tester.tap(find.textContaining('تأكيد الطلب'));
    await settle(tester);
    final placed = api.calls.lastWhere((c) => c.method == 'POST' && c.path == '/orders');
    expect(placed.body, containsPair('paymentMethod', 'CashOnDelivery'));
    expect(placed.body, containsPair('couponCode', 'HONEY10'));
    expect(placed.body, containsPair('addressId', 7));
    expect(find.textContaining('HH-20261122-0042'), findsWidgets);

    await tester.tap(find.text('تتبع الطلب'));
    await settle(tester);
    await tester.scrollUntilVisible(find.text('إلغاء الطلب'), 300,
        scrollable: find.descendant(of: find.byType(ListView), matching: find.byType(Scrollable)).first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('إلغاء الطلب'));
    await settle(tester);
    await tester.tap(find.widgetWithText(FilledButton, 'إلغاء الطلب'));
    await settle(tester);
    expect(api.calls.any((c) => c.path == '/orders/42/cancel'), isTrue);
    expect(find.text('تم إلغاء الطلب'), findsOneWidget);
  });
}
