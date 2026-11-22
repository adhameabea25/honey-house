import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';
import '../auth/auth_controller.dart';
import '../catalog/catalog_models.dart';

/// Ids of favorite products, used for the heart icons everywhere.
class FavoriteIdsController extends AsyncNotifier<Set<int>> {
  @override
  Future<Set<int>> build() async {
    if (!ref.watch(isSignedInProvider)) return {};
    final ids = await ref.read(apiClientProvider).get<List<dynamic>>('/favorites/ids');
    return ids.map((e) => e as int).toSet();
  }

  /// Returns true when the product is now a favorite.
  Future<bool> toggle(int productId) async {
    final current = state.value ?? {};
    final api = ref.read(apiClientProvider);
    final adding = !current.contains(productId);
    state = AsyncData(adding ? {...current, productId} : ({...current}..remove(productId)));
    try {
      if (adding) {
        await api.post<void>('/favorites/$productId');
      } else {
        await api.delete<void>('/favorites/$productId');
      }
    } catch (_) {
      state = AsyncData(current);
      rethrow;
    }
    ref.invalidate(favoriteProductsProvider);
    return adding;
  }
}

final favoriteIdsProvider = AsyncNotifierProvider<FavoriteIdsController, Set<int>>(FavoriteIdsController.new);

final favoriteProductsProvider = FutureProvider<List<ProductSummary>>((ref) async {
  ref.watch(localeProvider);
  if (!ref.watch(isSignedInProvider)) return [];
  final data = await ref.read(apiClientProvider).get<List<dynamic>>('/favorites');
  return data.map((e) => ProductSummary.fromJson(e as Map<String, dynamic>)).toList();
});
