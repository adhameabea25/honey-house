import 'package:flutter_test/flutter_test.dart';
import 'package:honey_house/core/format.dart';
import 'package:honey_house/core/json.dart';
import 'package:honey_house/core/validators.dart';
import 'package:honey_house/features/catalog/catalog_models.dart';
import 'package:honey_house/features/orders/order_models.dart';
import 'package:honey_house/l10n/app_localizations_en.dart';

void main() {
  test('validators follow the API rules', () {
    final v = Validators(AppLocalizationsEn());
    expect(v.phone('01012345678'), isNull);
    expect(v.phone('01312345678'), isNotNull);
    expect(v.phone('0101234567'), isNotNull);
    expect(v.email('a@b.co'), isNull);
    expect(v.email('nope'), isNotNull);
    expect(v.password('abcdefg1'), isNull);
    expect(v.password('ABCDEFG1'), isNotNull);
    expect(v.password('abc1'), isNotNull);
    expect(v.required('  '), isNotNull);
  });

  test('money uses Latin digits and the currency code', () {
    expect(Fmt.money(1250.5, 'EGP'), '1,250.5 EGP');
    expect(Fmt.money(180, 'EGP'), '180 EGP');
    Fmt.language = 'ar';
    expect(Fmt.money(180, 'EGP'), '180 ج.م');
    Fmt.language = 'en';
  });

  test('timestamps without a zone are treated as UTC', () {
    expect(parseUtc('2026-11-22T10:00:00').isUtc, isTrue);
    expect(parseUtc('2026-11-22T10:00:00Z').hour, 10);
  });

  test('product filter builds the API query and skips empty values', () {
    const f = ProductFilter(search: 'سدر', categoryId: 2, inStock: true, sort: ProductSort.priceAsc);
    final q = f.toQuery(3);
    expect(q['Search'], 'سدر');
    expect(q['CategoryId'], 2);
    expect(q['InStock'], true);
    expect(q['Sort'], 'price_asc');
    expect(q['Page'], 3);
    expect(f.copyWith(clearCategory: true).categoryId, isNull);
    expect(f, const ProductFilter(search: 'سدر', categoryId: 2, inStock: true, sort: ProductSort.priceAsc));
    expect(const ProductFilter().toQuery(1)['InStock'], isNull);
  });

  test('order JSON with string enums is parsed', () {
    final o = OrderSummary.fromJson({
      'id': 5, 'orderNumber': 'HH-1', 'status': 'OutForDelivery', 'statusName': 'خرج للتوصيل', 'paymentMethod': 'CashOnDelivery',
      'paymentStatus': 'Pending', 'total': 250.50, 'currency': 'EGP', 'itemCount': 2, 'createdAt': '2026-11-22T08:00:00',
    });
    expect(o.status, OrderStatus.outForDelivery);
    expect(o.paymentMethod, PaymentMethod.cashOnDelivery);
    expect(o.total, 250.5);
    expect(apiPaymentMethod(PaymentMethod.online), 'Online');
  });
}
