import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';
import '../auth/auth_controller.dart';
import 'cart_models.dart';

/// Server-side cart of the signed-in customer (guests have an empty cart and are asked to sign in).
class CartController extends AsyncNotifier<Cart> {
  @override
  Future<Cart> build() async {
    ref.watch(localeProvider);
    final signedIn = ref.watch(isSignedInProvider);
    if (!signedIn) return Cart.empty;
    return Cart.fromJson(await ref.read(apiClientProvider).get<Map<String, dynamic>>('/cart'));
  }

  Future<void> add(int variantId, int quantity) => _apply(() =>
      ref.read(apiClientProvider).post<Map<String, dynamic>>('/cart/items', data: {'variantId': variantId, 'quantity': quantity}));

  Future<void> setQuantity(int itemId, int quantity) => _apply(() =>
      ref.read(apiClientProvider).put<Map<String, dynamic>>('/cart/items/$itemId', data: {'quantity': quantity}));

  Future<void> remove(int itemId) =>
      _apply(() => ref.read(apiClientProvider).delete<Map<String, dynamic>>('/cart/items/$itemId'));

  /// Called after an order is placed (the server already emptied the cart).
  void reset() => state = AsyncData(Cart.empty);

  Future<void> _apply(Future<Map<String, dynamic>> Function() call) async {
    state = AsyncData(Cart.fromJson(await call()));
  }
}

final cartProvider = AsyncNotifierProvider<CartController, Cart>(CartController.new);

final cartCountProvider = Provider<int>((ref) => ref.watch(cartProvider).value?.itemCount ?? 0);
