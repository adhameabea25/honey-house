import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/format.dart';
import '../../core/l10n.dart';
import '../../core/theme.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import '../auth/auth_controller.dart';
import '../catalog/product_screen.dart' show QuantityStepper;
import 'cart_controller.dart';
import 'cart_models.dart';

class CartScreen extends ConsumerWidget {
  const CartScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final signedIn = ref.watch(isSignedInProvider);
    final cart = ref.watch(cartProvider);
    return Scaffold(
      appBar: AppBar(title: Text(l.navCart)),
      body: !signedIn
          ? const SignInPrompt(icon: Icons.shopping_bag_outlined)
          : AsyncBody(
              value: cart,
              onRetry: () => ref.invalidate(cartProvider),
              builder: (c) => c.items.isEmpty
                  ? EmptyView(
                      icon: Icons.shopping_bag_outlined,
                      text: l.cartEmpty,
                      action: FilledButton(onPressed: () => context.go('/shop'), child: Text(l.startShopping)),
                    )
                  : _CartBody(c),
            ),
    );
  }
}

class _CartBody extends ConsumerWidget {
  const _CartBody(this.cart);
  final Cart cart;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    return Column(children: [
      if (cart.hasUnavailableItems)
        MaterialBanner(
          backgroundColor: Colors.orange.shade50,
          leading: Icon(Icons.warning_amber, color: Colors.orange.shade900),
          content: Text(l.cartHasUnavailable),
          actions: const [SizedBox.shrink()],
        ),
      Expanded(
        child: RefreshIndicator(
          onRefresh: () => ref.refresh(cartProvider.future),
          child: ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: cart.items.length,
            separatorBuilder: (_, _) => const SizedBox(height: 12),
            itemBuilder: (_, i) => _CartItemTile(cart.items[i], currency: cart.currency),
          ),
        ),
      ),
      SafeArea(
        top: false,
        child: Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(color: Colors.white, boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.06), blurRadius: 8)]),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            AmountRow('${l.subtotal} (${l.itemsCount(cart.itemCount)})', Fmt.money(cart.subtotal, cart.currency), bold: true),
            const SizedBox(height: 10),
            BusyButton(
              label: l.checkout,
              icon: Icons.lock_outline,
              onPressed: cart.hasUnavailableItems ? null : () => context.push('/checkout'),
            ),
          ]),
        ),
      ),
    ]);
  }
}

class _CartItemTile extends ConsumerStatefulWidget {
  const _CartItemTile(this.item, {required this.currency});
  final CartItem item;
  final String currency;

  @override
  ConsumerState<_CartItemTile> createState() => _CartItemTileState();
}

class _CartItemTileState extends ConsumerState<_CartItemTile> {
  bool _busy = false;

  Future<void> _run(Future<void> Function(CartController c) action) async {
    setState(() => _busy = true);
    try {
      await action(ref.read(cartProvider.notifier));
    } catch (e) {
      if (mounted) showError(context, e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    final it = widget.item;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(10),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          InkWell(
            onTap: () => context.push('/product/${it.productId}'),
            child: SizedBox(width: 84, height: 84, child: NetImage(it.imageUrl)),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Expanded(child: Text(it.productName, maxLines: 2, overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w700))),
                IconButton(
                  visualDensity: VisualDensity.compact,
                  tooltip: l.removeItem,
                  icon: const Icon(Icons.delete_outline),
                  onPressed: _busy ? null : () => _run((c) => c.remove(it.id)),
                ),
              ]),
              Text('${it.variantLabel}  •  ${Fmt.money(it.unitPrice, widget.currency)}', style: TextStyle(color: Colors.grey.shade700)),
              if (!it.isAvailable)
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text(it.availableQuantity == 0 ? l.outOfStock : l.itemUnavailable(it.availableQuantity),
                      style: TextStyle(color: Colors.red.shade700, fontSize: 12.5)),
                ),
              const SizedBox(height: 6),
              Row(children: [
                QuantityStepper(
                  value: it.quantity,
                  max: it.availableQuantity > it.quantity ? it.availableQuantity.clamp(1, 99) : it.quantity,
                  onChanged: _busy ? null : (q) => _run((c) => c.setQuantity(it.id, q)),
                ),
                const Spacer(),
                Text(Fmt.money(it.lineTotal, widget.currency),
                    style: const TextStyle(fontWeight: FontWeight.w800, color: HoneyColors.honeyDark)),
              ]),
            ]),
          ),
        ]),
      ),
    );
  }
}
