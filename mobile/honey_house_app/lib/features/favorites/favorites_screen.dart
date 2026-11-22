import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import '../auth/auth_controller.dart';
import '../catalog/product_card.dart';
import 'favorites_controller.dart';

class FavoritesScreen extends ConsumerWidget {
  const FavoritesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final signedIn = ref.watch(isSignedInProvider);
    final ids = ref.watch(favoriteIdsProvider).value ?? const <int>{};
    final products = ref.watch(favoriteProductsProvider);
    return Scaffold(
      appBar: AppBar(title: Text(l.navFavorites)),
      body: !signedIn
          ? const SignInPrompt(icon: Icons.favorite_border)
          : AsyncBody(
              value: products,
              onRetry: () => ref.invalidate(favoriteProductsProvider),
              builder: (list) {
                // Hide hearts removed a moment ago while the list reloads.
                final shown = list.where((p) => ids.contains(p.id)).toList();
                if (shown.isEmpty) {
                  return EmptyView(
                    icon: Icons.favorite_border,
                    text: l.favoritesEmpty,
                    action: FilledButton(onPressed: () => context.go('/shop'), child: Text(l.startShopping)),
                  );
                }
                return RefreshIndicator(
                  onRefresh: () => ref.refresh(favoriteProductsProvider.future),
                  child: GridView.builder(
                    padding: const EdgeInsets.all(16),
                    gridDelegate: productGridDelegate(),
                    itemCount: shown.length,
                    itemBuilder: (_, i) => ProductCard(shown[i]),
                  ),
                );
              },
            ),
    );
  }
}
