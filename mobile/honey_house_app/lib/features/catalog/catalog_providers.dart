import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/json.dart';
import '../../core/providers.dart';
import 'catalog_models.dart';

// Catalog data is public; providers watch the language so names reload when it changes.

final categoriesProvider = FutureProvider<List<Category>>((ref) async {
  ref.watch(localeProvider);
  final data = await ref.read(apiClientProvider).get<List<dynamic>>('/categories', auth: false);
  return data.map((e) => Category.fromJson(e as Map<String, dynamic>)).toList();
});

final featuredProductsProvider = FutureProvider<List<ProductSummary>>((ref) async {
  ref.watch(localeProvider);
  final data = await ref.read(apiClientProvider).get<List<dynamic>>('/products/featured', query: {'limit': 10}, auth: false);
  return data.map((e) => ProductSummary.fromJson(e as Map<String, dynamic>)).toList();
});

final productProvider = FutureProvider.autoDispose.family<ProductDetail, int>((ref, id) async {
  ref.watch(localeProvider);
  return ProductDetail.fromJson(await ref.read(apiClientProvider).get<Map<String, dynamic>>('/products/$id', auth: false));
});

final productReviewsProvider = FutureProvider.autoDispose.family<ProductReviews, int>((ref, id) async {
  ref.watch(localeProvider);
  return ProductReviews.fromJson(await ref
      .read(apiClientProvider)
      .get<Map<String, dynamic>>('/products/$id/reviews', query: {'page': 1, 'pageSize': 10}, auth: false));
});

/// Product list for a filter, loaded page by page ("load more").
class ProductListState {
  const ProductListState(this.items, this.page, this.hasNext, this.totalCount, {this.loadingMore = false});
  final List<ProductSummary> items;
  final int page;
  final bool hasNext;
  final int totalCount;
  final bool loadingMore;
}

class ProductListController extends AsyncNotifier<ProductListState> {
  ProductListController(this.filter);
  final ProductFilter filter;

  @override
  Future<ProductListState> build() async {
    ref.watch(localeProvider);
    final page = await _fetch(1);
    return ProductListState(page.items, 1, page.hasNext, page.totalCount);
  }

  Future<Paged<ProductSummary>> _fetch(int page) async => Paged.fromJson(
      await ref.read(apiClientProvider).get<Map<String, dynamic>>('/products', query: filter.toQuery(page), auth: false),
      ProductSummary.fromJson);

  Future<void> loadMore() async {
    final s = state.value;
    if (s == null || !s.hasNext || s.loadingMore) return;
    state = AsyncData(ProductListState(s.items, s.page, s.hasNext, s.totalCount, loadingMore: true));
    try {
      final next = await _fetch(s.page + 1);
      state = AsyncData(ProductListState([...s.items, ...next.items], s.page + 1, next.hasNext, next.totalCount));
    } catch (_) {
      state = AsyncData(ProductListState(s.items, s.page, s.hasNext, s.totalCount));
      rethrow;
    }
  }
}

final productListProvider = AsyncNotifierProvider.autoDispose.family<ProductListController, ProductListState, ProductFilter>(
    ProductListController.new);

/// The signed-in customer's own review of a product (null if none).
final myReviewProvider = FutureProvider.autoDispose.family<MyReview?, int>((ref, productId) async {
  try {
    return MyReview.fromJson(await ref.read(apiClientProvider).get<Map<String, dynamic>>('/products/$productId/reviews/mine'));
  } on Exception {
    return null;
  }
});

class ReviewRepository {
  ReviewRepository(this.ref);
  final Ref ref;

  Future<void> submit(int productId, {MyReview? existing, required int rating, String? comment}) async {
    final body = {'rating': rating, 'comment': (comment?.trim().isEmpty ?? true) ? null : comment!.trim()};
    final api = ref.read(apiClientProvider);
    if (existing == null) {
      await api.post<Map<String, dynamic>>('/products/$productId/reviews', data: body);
    } else {
      await api.put<Map<String, dynamic>>('/reviews/${existing.id}', data: body);
    }
    ref.invalidate(myReviewProvider(productId));
  }
}

final reviewRepositoryProvider = Provider<ReviewRepository>(ReviewRepository.new);
