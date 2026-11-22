import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/theme.dart';
import '../../core/widgets/common.dart';
import '../auth/auth_controller.dart';
import 'catalog_models.dart';
import 'catalog_providers.dart';
import 'product_card.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final user = ref.watch(authProvider).value;
    final categories = ref.watch(categoriesProvider);
    final featured = ref.watch(featuredProductsProvider);
    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(categoriesProvider);
            ref.invalidate(featuredProductsProvider);
            await ref.read(featuredProductsProvider.future);
          },
          child: CustomScrollView(slivers: [
            SliverToBoxAdapter(
              child: Container(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 20),
                decoration: const BoxDecoration(
                  color: HoneyColors.cream,
                  borderRadius: BorderRadius.vertical(bottom: Radius.circular(24)),
                ),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(children: [
                    const Text('🍯', style: TextStyle(fontSize: 28)),
                    const SizedBox(width: 8),
                    Text(l.appTitle,
                        style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800, color: HoneyColors.honeyDark)),
                  ]),
                  const SizedBox(height: 4),
                  Text(user == null ? l.welcomeBack : l.welcomeUser(user.firstName)),
                  const SizedBox(height: 14),
                  TextField(
                    textInputAction: TextInputAction.search,
                    decoration: InputDecoration(hintText: l.searchHint, prefixIcon: const Icon(Icons.search)),
                    onSubmitted: (q) => context.go(Uri(path: '/shop', queryParameters: {if (q.trim().isNotEmpty) 'q': q.trim()}).toString()),
                  ),
                ]),
              ),
            ),
            SliverToBoxAdapter(child: SectionTitle(l.categories)),
            SliverToBoxAdapter(
              child: SizedBox(
                height: 118,
                child: AsyncBody(
                  value: categories,
                  onRetry: () => ref.invalidate(categoriesProvider),
                  builder: (list) => ListView.separated(
                    scrollDirection: Axis.horizontal,
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    itemCount: list.length,
                    separatorBuilder: (_, _) => const SizedBox(width: 12),
                    itemBuilder: (_, i) => _CategoryChip(list[i]),
                  ),
                ),
              ),
            ),
            SliverToBoxAdapter(
              child: SectionTitle(l.featured,
                  action: TextButton(onPressed: () => context.go('/shop'), child: Text(l.seeAll))),
            ),
            switch (featured) {
              AsyncData(:final value) when value.isEmpty =>
                SliverToBoxAdapter(child: Padding(padding: const EdgeInsets.all(24), child: Center(child: Text(l.noResults)))),
              AsyncData(:final value) => SliverPadding(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                  sliver: SliverGrid.builder(
                    gridDelegate: productGridDelegate(),
                    itemCount: value.length,
                    itemBuilder: (_, i) => ProductCard(value[i]),
                  ),
                ),
              AsyncError(:final error) => SliverToBoxAdapter(
                  child: ErrorView(error: error, onRetry: () => ref.invalidate(featuredProductsProvider))),
              _ => const SliverToBoxAdapter(child: Padding(padding: EdgeInsets.all(32), child: Center(child: CircularProgressIndicator()))),
            },
          ]),
        ),
      ),
    );
  }
}

class _CategoryChip extends StatelessWidget {
  const _CategoryChip(this.category);
  final Category category;

  @override
  Widget build(BuildContext context) => InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.go('/shop?category=${category.id}'),
        child: SizedBox(
          width: 84,
          child: Column(children: [
            SizedBox(width: 72, height: 72, child: NetImage(category.imageUrl, radius: 36)),
            const SizedBox(height: 6),
            Text(category.name, maxLines: 2, textAlign: TextAlign.center, overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontSize: 12.5, fontWeight: FontWeight.w600)),
          ]),
        ),
      );
}
