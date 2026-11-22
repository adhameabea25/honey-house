import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/format.dart';
import '../../core/l10n.dart';
import '../../core/theme.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import '../auth/auth_controller.dart';
import '../cart/cart_controller.dart';
import 'catalog_models.dart';
import 'catalog_providers.dart';
import 'product_card.dart';
import 'review_sheet.dart';

class ProductScreen extends ConsumerWidget {
  const ProductScreen({super.key, required this.productId});
  final int productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final product = ref.watch(productProvider(productId));
    return Scaffold(
      appBar: AppBar(
        title: Text(product.value?.name ?? '', overflow: TextOverflow.ellipsis),
        actions: [FavoriteButton(productId: productId)],
      ),
      body: AsyncBody(
        value: product,
        onRetry: () => ref.invalidate(productProvider(productId)),
        builder: (p) => _ProductBody(p),
      ),
    );
  }
}

class _ProductBody extends ConsumerStatefulWidget {
  const _ProductBody(this.product);
  final ProductDetail product;

  @override
  ConsumerState<_ProductBody> createState() => _ProductBodyState();
}

class _ProductBodyState extends ConsumerState<_ProductBody> {
  late Variant? _variant = widget.product.variants.where((v) => v.inStock).firstOrNull ?? widget.product.variants.firstOrNull;
  int _qty = 1;
  int _image = 0;
  bool _busy = false;

  Future<void> _addToCart() async {
    final l = context.l10n;
    if (!ref.read(isSignedInProvider)) return goToLogin(context);
    setState(() => _busy = true);
    try {
      await ref.read(cartProvider.notifier).add(_variant!.id, _qty);
      if (!mounted) return;
      showMessage(context, l.addedToCart, action: SnackBarAction(label: l.viewCart, onPressed: () => context.go('/cart')));
    } catch (e) {
      if (mounted) showError(context, e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    final p = widget.product;
    final v = _variant;
    final images = p.images.isEmpty ? [null] : p.images.map((e) => e.url).toList();
    final maxQty = v == null ? 1 : v.availableQuantity.clamp(1, 99);

    return Column(children: [
      Expanded(
        child: ListView(children: [
          AspectRatio(
            aspectRatio: 1.2,
            child: Stack(children: [
              PageView.builder(
                itemCount: images.length,
                onPageChanged: (i) => setState(() => _image = i),
                itemBuilder: (_, i) => NetImage(images[i], radius: 0),
              ),
              if (images.length > 1)
                Positioned(
                  bottom: 10,
                  left: 0,
                  right: 0,
                  child: Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                    for (var i = 0; i < images.length; i++)
                      Container(
                        width: i == _image ? 18 : 7,
                        height: 7,
                        margin: const EdgeInsets.symmetric(horizontal: 3),
                        decoration: BoxDecoration(
                            color: i == _image ? HoneyColors.honey : Colors.white70, borderRadius: BorderRadius.circular(4)),
                      ),
                  ]),
                ),
            ]),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(p.category.name, style: TextStyle(color: Colors.grey.shade700)),
              const SizedBox(height: 4),
              Text(p.name, style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800)),
              const SizedBox(height: 6),
              Row(children: [
                Stars(p.averageRating),
                const SizedBox(width: 6),
                Text(l.reviewsCount(p.reviewCount), style: Theme.of(context).textTheme.bodySmall),
              ]),
              if (p.origin?.isNotEmpty ?? false) ...[
                const SizedBox(height: 6),
                Text(l.origin(p.origin!), style: TextStyle(color: Colors.grey.shade800)),
              ],
              const SizedBox(height: 16),
              if (v != null)
                Row(crossAxisAlignment: CrossAxisAlignment.end, children: [
                  Text(Fmt.money(v.price, p.currency),
                      style: Theme.of(context).textTheme.headlineSmall?.copyWith(color: HoneyColors.honeyDark, fontWeight: FontWeight.w800)),
                  const SizedBox(width: 10),
                  if (v.compareAtPrice != null && v.compareAtPrice! > v.price)
                    Text(Fmt.money(v.compareAtPrice!, p.currency),
                        style: TextStyle(decoration: TextDecoration.lineThrough, color: Colors.grey.shade600)),
                ]),
              const SizedBox(height: 16),
              Text(l.chooseWeight, style: const TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(height: 8),
              Wrap(spacing: 8, runSpacing: 8, children: [
                for (final x in p.variants)
                  ChoiceChip(
                    label: Text(x.label),
                    selected: x.id == v?.id,
                    onSelected: x.inStock ? (_) => setState(() {
                          _variant = x;
                          _qty = 1;
                        }) : null,
                  ),
              ]),
              if (v != null && !v.inStock)
                Padding(padding: const EdgeInsets.only(top: 8), child: Text(l.outOfStock, style: TextStyle(color: Colors.red.shade700)))
              else if (v != null && v.availableQuantity <= 5)
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Text(l.onlyLeft(v.availableQuantity), style: TextStyle(color: Colors.orange.shade900)),
                ),
              const SizedBox(height: 16),
              Row(children: [
                Text(l.quantity, style: const TextStyle(fontWeight: FontWeight.w700)),
                const Spacer(),
                QuantityStepper(value: _qty, max: maxQty, onChanged: (q) => setState(() => _qty = q)),
              ]),
              if (p.description?.isNotEmpty ?? false) ...[
                const SizedBox(height: 20),
                Text(l.description, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
                const SizedBox(height: 6),
                Text(p.description!, style: const TextStyle(height: 1.6)),
              ],
            ]),
          ),
          const Divider(),
          _ReviewsSection(productId: p.id),
          const SizedBox(height: 16),
        ]),
      ),
      SafeArea(
        top: false,
        child: Container(
          padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
          decoration: BoxDecoration(color: Colors.white, boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.06), blurRadius: 8)]),
          child: BusyButton(
            label: v != null && v.inStock ? '${l.addToCart}  •  ${Fmt.money(v.price * _qty, p.currency)}' : l.outOfStock,
            icon: Icons.add_shopping_cart,
            busy: _busy,
            onPressed: v != null && v.inStock ? _addToCart : null,
          ),
        ),
      ),
    ]);
  }
}

class QuantityStepper extends StatelessWidget {
  const QuantityStepper({super.key, required this.value, required this.onChanged, this.max = 99, this.min = 1});
  final int value;
  final int min;
  final int max;
  final ValueChanged<int>? onChanged;

  @override
  Widget build(BuildContext context) => Container(
        decoration: BoxDecoration(border: Border.all(color: Colors.grey.shade300), borderRadius: BorderRadius.circular(24)),
        child: Row(mainAxisSize: MainAxisSize.min, children: [
          IconButton(
            visualDensity: VisualDensity.compact,
            icon: const Icon(Icons.remove, size: 18),
            onPressed: onChanged != null && value > min ? () => onChanged!(value - 1) : null,
          ),
          SizedBox(width: 28, child: Text('$value', textAlign: TextAlign.center, style: const TextStyle(fontWeight: FontWeight.w700))),
          IconButton(
            visualDensity: VisualDensity.compact,
            icon: const Icon(Icons.add, size: 18),
            onPressed: onChanged != null && value < max ? () => onChanged!(value + 1) : null,
          ),
        ]),
      );
}

class _ReviewsSection extends ConsumerWidget {
  const _ReviewsSection({required this.productId});
  final int productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final reviews = ref.watch(productReviewsProvider(productId));
    final signedIn = ref.watch(isSignedInProvider);
    final mine = signedIn ? ref.watch(myReviewProvider(productId)).value : null;
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      SectionTitle(
        l.reviews,
        action: TextButton.icon(
          icon: const Icon(Icons.rate_review_outlined, size: 18),
          label: Text(mine == null ? l.writeReview : l.editReview),
          onPressed: () => signedIn ? showReviewSheet(context, productId: productId, existing: mine) : goToLogin(context),
        ),
      ),
      if (mine != null && !mine.isApproved)
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Text(l.reviewPending, style: TextStyle(color: Colors.orange.shade900)),
        ),
      switch (reviews) {
        AsyncData(:final value) when value.count == 0 =>
          Padding(padding: const EdgeInsets.all(16), child: Text(l.noReviews, style: TextStyle(color: Colors.grey.shade700))),
        AsyncData(:final value) => Column(children: [
            _RatingSummary(value),
            for (final r in value.reviews.items) _ReviewTile(r),
          ]),
        AsyncError() => const SizedBox.shrink(),
        _ => const Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator())),
      },
    ]);
  }
}

class _RatingSummary extends StatelessWidget {
  const _RatingSummary(this.data);
  final ProductReviews data;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        child: Row(children: [
          Column(children: [
            Text(data.average.toStringAsFixed(1), style: const TextStyle(fontSize: 36, fontWeight: FontWeight.w800)),
            Stars(data.average),
            Text(context.l10n.reviewsCount(data.count), style: Theme.of(context).textTheme.bodySmall),
          ]),
          const SizedBox(width: 20),
          Expanded(
            child: Column(children: [
              for (var star = 5; star >= 1; star--)
                Row(children: [
                  SizedBox(width: 14, child: Text('$star', style: const TextStyle(fontSize: 12))),
                  const SizedBox(width: 6),
                  Expanded(
                    child: LinearProgressIndicator(
                      value: data.count == 0 ? 0 : (data.distribution[star] ?? 0) / data.count,
                      minHeight: 6,
                      borderRadius: BorderRadius.circular(3),
                      color: HoneyColors.honey,
                      backgroundColor: Colors.grey.shade200,
                    ),
                  ),
                ]),
            ]),
          ),
        ]),
      );
}

class _ReviewTile extends StatelessWidget {
  const _ReviewTile(this.review);
  final Review review;

  @override
  Widget build(BuildContext context) => ListTile(
        title: Row(children: [
          Expanded(child: Text(review.reviewerName, style: const TextStyle(fontWeight: FontWeight.w600))),
          Text(Fmt.date(review.createdAt), style: Theme.of(context).textTheme.bodySmall),
        ]),
        subtitle: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          const SizedBox(height: 4),
          Row(children: [
            Stars(review.rating.toDouble(), size: 14),
            if (review.isVerifiedPurchase) ...[
              const SizedBox(width: 8),
              Icon(Icons.verified, size: 14, color: Colors.green.shade700),
              const SizedBox(width: 2),
              Text(context.l10n.verifiedPurchase, style: TextStyle(fontSize: 12, color: Colors.green.shade700)),
            ],
          ]),
          if (review.comment?.isNotEmpty ?? false) Padding(padding: const EdgeInsets.only(top: 4), child: Text(review.comment!)),
        ]),
      );
}
