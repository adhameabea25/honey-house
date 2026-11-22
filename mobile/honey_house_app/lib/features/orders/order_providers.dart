import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/json.dart';
import '../../core/providers.dart';
import 'order_models.dart';

class OrderRepository {
  OrderRepository(this.ref);
  final Ref ref;

  Future<CheckoutSummary> preview(int addressId, String? coupon) async => CheckoutSummary.fromJson(await ref
      .read(apiClientProvider)
      .post<Map<String, dynamic>>('/orders/checkout/preview', data: {'addressId': addressId, 'couponCode': coupon}));

  Future<PlacedOrder> place({required int addressId, required PaymentMethod method, String? coupon, String? notes}) async =>
      PlacedOrder.fromJson(await ref.read(apiClientProvider).post<Map<String, dynamic>>('/orders', data: {
        'addressId': addressId, 'paymentMethod': apiPaymentMethod(method), 'couponCode': coupon,
        'notes': (notes?.trim().isEmpty ?? true) ? null : notes!.trim(),
      }));

  Future<OrderDetail> cancel(int id, String? reason) async {
    final r = OrderDetail.fromJson(
        await ref.read(apiClientProvider).post<Map<String, dynamic>>('/orders/$id/cancel', data: {'reason': reason}));
    ref.invalidate(orderProvider(id));
    ref.invalidate(myOrdersProvider);
    return r;
  }

  /// New payment link for an unpaid online order.
  Future<String?> retryPayment(int orderId) async =>
      (await ref.read(apiClientProvider).post<Map<String, dynamic>>('/payments/$orderId/retry'))['paymentUrl'] as String?;
}

final orderRepositoryProvider = Provider<OrderRepository>(OrderRepository.new);

final myOrdersProvider = FutureProvider.autoDispose<Paged<OrderSummary>>((ref) async {
  ref.watch(localeProvider);
  return Paged.fromJson(
      await ref.read(apiClientProvider).get<Map<String, dynamic>>('/orders', query: {'Page': 1, 'PageSize': 50}),
      OrderSummary.fromJson);
});

final orderProvider = FutureProvider.autoDispose.family<OrderDetail, int>((ref, id) async {
  ref.watch(localeProvider);
  return OrderDetail.fromJson(await ref.read(apiClientProvider).get<Map<String, dynamic>>('/orders/$id'));
});
