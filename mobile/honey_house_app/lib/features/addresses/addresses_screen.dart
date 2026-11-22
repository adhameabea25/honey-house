import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/format.dart';
import '../../core/l10n.dart';
import '../../core/theme.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import 'address_models.dart';
import 'address_providers.dart';

/// Saved addresses. With [selecting] a tap returns the address (used by checkout).
class AddressesScreen extends ConsumerWidget {
  const AddressesScreen({super.key, this.selecting = false});
  final bool selecting;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final addresses = ref.watch(addressesProvider);
    return Scaffold(
      appBar: AppBar(title: Text(selecting ? l.deliveryAddress : l.myAddresses)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final created = await context.push<Address>('/addresses/new');
          if (created != null && selecting && context.mounted) context.pop(created);
        },
        icon: const Icon(Icons.add_location_alt_outlined),
        label: Text(l.addAddress),
      ),
      body: AsyncBody(
        value: addresses,
        onRetry: () => ref.invalidate(addressesProvider),
        builder: (list) => list.isEmpty
            ? EmptyView(icon: Icons.location_off_outlined, text: l.noAddresses)
            : ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
                itemCount: list.length,
                separatorBuilder: (_, _) => const SizedBox(height: 12),
                itemBuilder: (_, i) => AddressCard(
                  list[i],
                  onTap: selecting ? () => context.pop(list[i]) : null,
                  trailing: _AddressMenu(list[i]),
                ),
              ),
      ),
    );
  }
}

class AddressCard extends StatelessWidget {
  const AddressCard(this.address, {super.key, this.onTap, this.trailing});
  final Address address;
  final VoidCallback? onTap;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    final a = address;
    return Card(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: a.isDefault ? const BorderSide(color: HoneyColors.honey, width: 1.5) : BorderSide.none,
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsetsDirectional.fromSTEB(16, 12, 4, 12),
          child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Padding(padding: EdgeInsets.only(top: 2), child: Icon(Icons.location_on_outlined, color: HoneyColors.honeyDark)),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Row(children: [
                  Flexible(child: Text(a.recipientName, style: const TextStyle(fontWeight: FontWeight.w700))),
                  if (a.isDefault) ...[
                    const SizedBox(width: 8),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(color: HoneyColors.cream, borderRadius: BorderRadius.circular(8)),
                      child: Text(l.defaultAddress, style: const TextStyle(fontSize: 12, color: HoneyColors.honeyDark)),
                    ),
                  ],
                ]),
                const SizedBox(height: 4),
                Text(a.line),
                const SizedBox(height: 2),
                Text(a.phone, textDirection: TextDirection.ltr, style: TextStyle(color: Colors.grey.shade700)),
                Text(l.shippingFeeValue(Fmt.money(a.shippingFee, l.currency)), style: TextStyle(color: Colors.grey.shade700, fontSize: 12.5)),
              ]),
            ),
            ?trailing,
          ]),
        ),
      ),
    );
  }
}

class _AddressMenu extends ConsumerWidget {
  const _AddressMenu(this.address);
  final Address address;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final ctrl = ref.read(addressesProvider.notifier);
    Future<void> run(Future<void> Function() f) async {
      try {
        await f();
      } catch (e) {
        if (context.mounted) showError(context, e);
      }
    }

    return PopupMenuButton<String>(
      onSelected: (v) async {
        switch (v) {
          case 'edit':
            context.push('/addresses/${address.id}');
          case 'default':
            await run(() => ctrl.makeDefault(address.id));
          case 'delete':
            if (await confirmDialog(context, l.confirmDeleteAddress, confirmLabel: l.delete, destructive: true)) {
              await run(() => ctrl.delete(address.id));
            }
        }
      },
      itemBuilder: (_) => [
        PopupMenuItem(value: 'edit', child: Text(l.edit)),
        if (!address.isDefault) PopupMenuItem(value: 'default', child: Text(l.setDefault)),
        PopupMenuItem(value: 'delete', child: Text(l.delete, style: TextStyle(color: Colors.red.shade700))),
      ],
    );
  }
}
