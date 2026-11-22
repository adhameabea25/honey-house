import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/format.dart';
import '../../core/l10n.dart';
import '../../core/widgets/common.dart';
import '../orders/order_models.dart';
import '../orders/order_providers.dart';

class OrderPlacedScreen extends ConsumerWidget {
  const OrderPlacedScreen({super.key, required this.orderId});
  final int orderId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final order = ref.watch(orderProvider(orderId));
    return Scaffold(
      appBar: AppBar(automaticallyImplyLeading: false, title: Text(l.orderPlaced)),
      body: AsyncBody(
        value: order,
        onRetry: () => ref.invalidate(orderProvider(orderId)),
        builder: (o) {
          final unpaidOnline = o.summary.paymentMethod == PaymentMethod.online &&
              o.summary.paymentStatus != PaymentStatus.paid && o.summary.status == OrderStatus.pending;
          return ListView(padding: const EdgeInsets.all(24), children: [
            const Center(child: Text('🎉', style: TextStyle(fontSize: 72))),
            const SizedBox(height: 12),
            Text(l.orderPlacedText(o.summary.orderNumber), textAlign: TextAlign.center, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 20),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(children: [
                  AmountRow(l.total, Fmt.money(o.summary.total, o.summary.currency), bold: true),
                  AmountRow(l.payment, o.summary.paymentStatusName),
                ]),
              ),
            ),
            if (unpaidOnline) ...[
              const SizedBox(height: 12),
              Text(l.paymentPendingNote, textAlign: TextAlign.center, style: TextStyle(color: Colors.orange.shade900)),
              const SizedBox(height: 8),
              FilledButton.icon(
                icon: const Icon(Icons.credit_card),
                label: Text(l.payNow),
                onPressed: () => payOrder(context, ref, orderId),
              ),
            ],
            const SizedBox(height: 16),
            FilledButton.tonal(onPressed: () => context.go('/orders/$orderId'), child: Text(l.trackOrder)),
            const SizedBox(height: 8),
            TextButton(onPressed: () => context.go('/'), child: Text(l.continueShopping)),
          ]);
        },
      ),
    );
  }
}

/// Starts a new payment attempt for an unpaid online order and opens the payment page.
Future<void> payOrder(BuildContext context, WidgetRef ref, int orderId) async {
  try {
    final url = await ref.read(orderRepositoryProvider).retryPayment(orderId);
    if (url != null && context.mounted) {
      await context.push(Uri(path: '/payment/$orderId', queryParameters: {'url': url}).toString());
    }
  } catch (e) {
    if (context.mounted) showError(context, e);
  }
}
