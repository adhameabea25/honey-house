import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/format.dart';
import '../../core/l10n.dart';
import '../../core/theme.dart';
import '../../core/widgets/common.dart';
import 'order_providers.dart';
import 'order_widgets.dart';

class OrdersScreen extends ConsumerWidget {
  const OrdersScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final orders = ref.watch(myOrdersProvider);
    return Scaffold(
      appBar: AppBar(title: Text(l.myOrders)),
      body: AsyncBody(
        value: orders,
        onRetry: () => ref.invalidate(myOrdersProvider),
        builder: (page) => page.items.isEmpty
            ? EmptyView(
                icon: Icons.receipt_long_outlined,
                text: l.noOrders,
                action: FilledButton(onPressed: () => context.go('/shop'), child: Text(l.startShopping)),
              )
            : RefreshIndicator(
                onRefresh: () => ref.refresh(myOrdersProvider.future),
                child: ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: page.items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 10),
                  itemBuilder: (_, i) {
                    final o = page.items[i];
                    return Card(
                      child: InkWell(
                        borderRadius: BorderRadius.circular(12),
                        onTap: () => context.push('/orders/${o.id}'),
                        child: Padding(
                          padding: const EdgeInsets.all(14),
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Row(children: [
                              Expanded(
                                child: Text(l.orderNumber(o.orderNumber),
                                    style: const TextStyle(fontWeight: FontWeight.w700), overflow: TextOverflow.ellipsis),
                              ),
                              StatusChip(o.status, o.statusName),
                            ]),
                            const SizedBox(height: 6),
                            Text(Fmt.dateTime(o.createdAt), style: TextStyle(color: Colors.grey.shade700)),
                            const SizedBox(height: 6),
                            Row(children: [
                              Text(l.itemsCount(o.itemCount)),
                              const Spacer(),
                              Text(Fmt.money(o.total, o.currency),
                                  style: const TextStyle(fontWeight: FontWeight.w800, color: HoneyColors.honeyDark)),
                            ]),
                          ]),
                        ),
                      ),
                    );
                  },
                ),
              ),
      ),
    );
  }
}
