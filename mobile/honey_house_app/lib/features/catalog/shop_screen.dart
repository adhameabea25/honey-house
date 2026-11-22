import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/widgets/common.dart';
import 'catalog_models.dart';
import 'catalog_providers.dart';
import 'product_card.dart';

/// Product list with search, category chips, sort and price/stock filters.
class ShopScreen extends ConsumerStatefulWidget {
  const ShopScreen({super.key, this.initial = const ProductFilter()});
  final ProductFilter initial;

  @override
  ConsumerState<ShopScreen> createState() => _ShopScreenState();
}

class _ShopScreenState extends ConsumerState<ShopScreen> {
  late ProductFilter _filter = widget.initial;
  late final _search = TextEditingController(text: widget.initial.search);

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  void _set(ProductFilter f) => setState(() => _filter = f);

  String _sortLabel(ProductSort s) {
    final l = context.l10n;
    return switch (s) {
      ProductSort.newest => l.sortNewest,
      ProductSort.priceAsc => l.sortPriceAsc,
      ProductSort.priceDesc => l.sortPriceDesc,
      ProductSort.rating => l.sortRating,
      ProductSort.bestselling => l.sortBestselling,
    };
  }

  Future<void> _openFilters() async {
    final result = await showModalBottomSheet<ProductFilter>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _FilterSheet(filter: _filter),
    );
    if (result != null) _set(result);
  }

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    final list = ref.watch(productListProvider(_filter));
    final categories = ref.watch(categoriesProvider).value ?? const <Category>[];
    final hasExtraFilters = _filter.minPrice != null || _filter.maxPrice != null || _filter.inStock;

    // Sub-sections of the chosen top-level category (or of the parent of the chosen sub-section).
    Category? parent;
    for (final c in categories) {
      if (c.id == _filter.categoryId || c.subCategories.any((s) => s.id == _filter.categoryId)) parent = c;
    }

    return Scaffold(
      appBar: AppBar(title: Text(l.allProducts)),
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
          child: TextField(
            controller: _search,
            textInputAction: TextInputAction.search,
            decoration: InputDecoration(
              hintText: l.searchHint,
              prefixIcon: const Icon(Icons.search),
              suffixIcon: _search.text.isEmpty
                  ? null
                  : IconButton(
                      icon: const Icon(Icons.close),
                      onPressed: () {
                        _search.clear();
                        _set(ProductFilter(categoryId: _filter.categoryId, minPrice: _filter.minPrice,
                            maxPrice: _filter.maxPrice, inStock: _filter.inStock, sort: _filter.sort));
                      },
                    ),
            ),
            onSubmitted: (q) => _set(ProductFilter(search: q.trim().isEmpty ? null : q.trim(), categoryId: _filter.categoryId,
                minPrice: _filter.minPrice, maxPrice: _filter.maxPrice, inStock: _filter.inStock, sort: _filter.sort)),
          ),
        ),
        SizedBox(
          height: 44,
          child: ListView(scrollDirection: Axis.horizontal, padding: const EdgeInsets.symmetric(horizontal: 12), children: [
            _chip(l.all, _filter.categoryId == null, () => _set(_filter.copyWith(clearCategory: true))),
            for (final c in categories) _chip(c.name, parent?.id == c.id, () => _set(_filter.copyWith(categoryId: c.id))),
          ]),
        ),
        if (parent != null && parent.subCategories.isNotEmpty)
          SizedBox(
            height: 40,
            child: ListView(scrollDirection: Axis.horizontal, padding: const EdgeInsets.symmetric(horizontal: 12), children: [
              for (final s in parent.subCategories)
                _chip(s.name, _filter.categoryId == s.id,
                    () => _set(_filter.copyWith(categoryId: _filter.categoryId == s.id ? parent!.id : s.id)), small: true),
            ]),
          ),
        Padding(
          padding: const EdgeInsets.fromLTRB(8, 0, 8, 0),
          child: Row(children: [
            PopupMenuButton<ProductSort>(
              initialValue: _filter.sort,
              onSelected: (s) => _set(_filter.copyWith(sort: s)),
              itemBuilder: (_) => [for (final s in ProductSort.values) PopupMenuItem(value: s, child: Text(_sortLabel(s)))],
              child: Padding(
                padding: const EdgeInsets.all(8),
                child: Row(children: [const Icon(Icons.sort, size: 20), const SizedBox(width: 6), Text(_sortLabel(_filter.sort))]),
              ),
            ),
            const Spacer(),
            TextButton.icon(
              onPressed: _openFilters,
              icon: Badge(isLabelVisible: hasExtraFilters, smallSize: 8, child: const Icon(Icons.tune, size: 20)),
              label: Text(l.filters),
            ),
          ]),
        ),
        Expanded(
          child: AsyncBody(
            value: list,
            onRetry: () => ref.invalidate(productListProvider(_filter)),
            builder: (s) {
              if (s.items.isEmpty) return EmptyView(icon: Icons.search_off, text: l.noResults);
              return NotificationListener<ScrollNotification>(
                onNotification: (n) {
                  if (n.metrics.pixels > n.metrics.maxScrollExtent - 400) {
                    ref.read(productListProvider(_filter).notifier).loadMore().catchError((Object e) {
                      if (context.mounted) showError(context, e);
                    });
                  }
                  return false;
                },
                child: RefreshIndicator(
                  onRefresh: () => ref.refresh(productListProvider(_filter).future),
                  child: CustomScrollView(slivers: [
                    SliverPadding(
                      padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
                      sliver: SliverGrid.builder(
                        gridDelegate: productGridDelegate(),
                        itemCount: s.items.length,
                        itemBuilder: (_, i) => ProductCard(s.items[i]),
                      ),
                    ),
                    if (s.loadingMore)
                      const SliverToBoxAdapter(child: Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator()))),
                  ]),
                ),
              );
            },
          ),
        ),
      ]),
    );
  }

  Widget _chip(String label, bool selected, VoidCallback onTap, {bool small = false}) => Padding(
        padding: const EdgeInsets.symmetric(horizontal: 4),
        child: ChoiceChip(
          label: Text(label, style: TextStyle(fontSize: small ? 12.5 : 14)),
          selected: selected,
          onSelected: (_) => onTap(),
          visualDensity: small ? VisualDensity.compact : null,
        ),
      );
}

class _FilterSheet extends StatefulWidget {
  const _FilterSheet({required this.filter});
  final ProductFilter filter;

  @override
  State<_FilterSheet> createState() => _FilterSheetState();
}

class _FilterSheetState extends State<_FilterSheet> {
  late final _min = TextEditingController(text: _fmt(widget.filter.minPrice));
  late final _max = TextEditingController(text: _fmt(widget.filter.maxPrice));
  late bool _inStock = widget.filter.inStock;

  static String _fmt(double? v) => v == null ? '' : (v == v.roundToDouble() ? v.toInt().toString() : v.toString());

  @override
  void dispose() {
    _min.dispose();
    _max.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    Widget priceField(TextEditingController c, String label) => Expanded(
          child: TextField(
            controller: c,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
            textDirection: TextDirection.ltr,
            decoration: InputDecoration(labelText: label, suffixText: l.currency),
          ),
        );
    return Padding(
      padding: EdgeInsets.fromLTRB(20, 0, 20, 20 + MediaQuery.viewInsetsOf(context).bottom),
      child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(l.filters, style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 16),
        Row(children: [priceField(_min, l.minPrice), const SizedBox(width: 12), priceField(_max, l.maxPrice)]),
        const SizedBox(height: 8),
        SwitchListTile(contentPadding: EdgeInsets.zero, title: Text(l.inStockOnly), value: _inStock,
            onChanged: (v) => setState(() => _inStock = v)),
        const SizedBox(height: 8),
        Row(children: [
          Expanded(
            child: OutlinedButton(
              onPressed: () => Navigator.pop(context, widget.filter.copyWith(clearPrices: true, inStock: false)),
              child: Text(l.clear),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: FilledButton(
              onPressed: () => Navigator.pop(
                context,
                ProductFilter(
                  search: widget.filter.search,
                  categoryId: widget.filter.categoryId,
                  sort: widget.filter.sort,
                  minPrice: double.tryParse(_min.text),
                  maxPrice: double.tryParse(_max.text),
                  inStock: _inStock,
                ),
              ),
              child: Text(l.apply),
            ),
          ),
        ]),
      ]),
    );
  }
}
