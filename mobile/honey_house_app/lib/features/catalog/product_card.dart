import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/format.dart';
import '../../core/l10n.dart';
import '../../core/theme.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import '../auth/auth_controller.dart';
import '../favorites/favorites_controller.dart';
import 'catalog_models.dart';

class ProductCard extends StatelessWidget {
  const ProductCard(this.product, {super.key});
  final ProductSummary product;

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    final p = product;
    final price = Fmt.money(p.minPrice, p.currency);
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push('/product/${p.id}'),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Expanded(
            child: Stack(fit: StackFit.expand, children: [
              NetImage(p.mainImageUrl, radius: 0),
              PositionedDirectional(top: 4, end: 4, child: FavoriteButton(productId: p.id, small: true)),
              if (!p.inStock)
                PositionedDirectional(
                  bottom: 6,
                  start: 6,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(color: Colors.black54, borderRadius: BorderRadius.circular(8)),
                    child: Text(l.outOfStock, style: const TextStyle(color: Colors.white, fontSize: 12)),
                  ),
                ),
            ]),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(10, 8, 10, 10),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(p.name, maxLines: 2, overflow: TextOverflow.ellipsis, style: const TextStyle(fontWeight: FontWeight.w600, height: 1.3)),
              const SizedBox(height: 4),
              if (p.reviewCount > 0)
                Row(children: [
                  Stars(p.averageRating, size: 13),
                  const SizedBox(width: 4),
                  Text('${p.reviewCount}', style: Theme.of(context).textTheme.bodySmall),
                ]),
              const SizedBox(height: 4),
              Text(p.maxPrice > p.minPrice ? l.fromPrice(price) : price,
                  style: const TextStyle(fontWeight: FontWeight.w800, color: HoneyColors.honeyDark)),
            ]),
          ),
        ]),
      ),
    );
  }
}

/// Heart icon that adds/removes a favorite (asks guests to sign in).
class FavoriteButton extends ConsumerWidget {
  const FavoriteButton({super.key, required this.productId, this.small = false});
  final int productId;
  final bool small;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final isFav = ref.watch(favoriteIdsProvider).value?.contains(productId) ?? false;
    final icon = Icon(isFav ? Icons.favorite : Icons.favorite_border, color: isFav ? Colors.red.shade600 : HoneyColors.honeyDark,
        size: small ? 20 : 24);
    Future<void> toggle() async {
      if (!ref.read(isSignedInProvider)) return goToLogin(context);
      try {
        final added = await ref.read(favoriteIdsProvider.notifier).toggle(productId);
        if (context.mounted) showMessage(context, added ? l.addedToFavorites : l.removedFromFavorites);
      } catch (e) {
        if (context.mounted) showError(context, e);
      }
    }

    if (!small) return IconButton(onPressed: toggle, icon: icon, tooltip: l.navFavorites);
    return Material(
      color: Colors.white.withValues(alpha: 0.9),
      shape: const CircleBorder(),
      child: InkWell(customBorder: const CircleBorder(), onTap: toggle, child: Padding(padding: const EdgeInsets.all(6), child: icon)),
    );
  }
}

/// Responsive product grid (2 columns on phones, more on wide screens).
SliverGridDelegate productGridDelegate() => const SliverGridDelegateWithMaxCrossAxisExtent(
      maxCrossAxisExtent: 220, mainAxisExtent: 270, crossAxisSpacing: 12, mainAxisSpacing: 12);
