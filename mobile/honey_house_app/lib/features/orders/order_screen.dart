import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/format.dart';
import '../../core/l10n.dart';
import '../../core/theme.dart';
import '../../core/widgets/common.dart';
import '../catalog/review_sheet.dart';
import '../checkout/order_placed_screen.dart' show payOrder;
import 'order_models.dart';
import 'order_providers.dart';
import 'order_widgets.dart';

/// Order details with the status timeline, cancel and pay-again actions, and product reviews after delivery.
class OrderScreen extends ConsumerWidget {
  const OrderScreen({super.key, required this.orderId});
  final int orderId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final order = ref.watch(orderProvider(orderId));
    return Scaffold(
      appBar: AppBar(title: Text(order.value == null ? l.myOrders : l.orderNumber(order.value!.summary.orderNumber))),
      body: AsyncBody(
        value: order,
        onRetry: () => ref.invalidate(orderProvider(orderId)),
        builder: (o) => RefreshIndicator(
          onRefresh: () => ref.refresh(orderProvider(orderId).future),
          child: _OrderBody(o),
        ),
      ),
    );
  }
}

class _OrderBody extends ConsumerWidget {
  const _OrderBody(this.order);
  final OrderDetail order;

  Future<void> _cancel(BuildContext context, WidgetRef ref) async {
    final l = context.l10n;
    final reason = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: Text(l.cancelOrder),
        content: TextField(controller: reason, maxLength: 500, decoration: InputDecoration(labelText: l.cancelReason)),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: Text(l.close)),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: Colors.red.shade700, foregroundColor: Colors.white),
            onPressed: () => Navigator.pop(c, true),
            child: Text(l.cancelOrder),
          ),
        ],
      ),
    );
    final text = reason.text.trim();
    reason.dispose();
    if (ok != true) return;
    try {
      await ref.read(orderRepositoryProvider).cancel(order.id, text.isEmpty ? null : text);
      if (context.mounted) showMessage(context, l.orderCancelled);
    } catch (e) {
      if (context.mounted) showError(context, e);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final o = order;
    final s = o.summary;
    String m(double v) => Fmt.money(v, s.currency);
    final unpaidOnline = s.paymentMethod == PaymentMethod.online && s.paymentStatus != PaymentStatus.paid && s.status == OrderStatus.pending;
    final delivered = s.status == OrderStatus.delivered;
    final products = {for (final i in o.items) i.productId: i.productName};

    return ListView(padding: const EdgeInsets.all(16), children: [
      Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              Text(l.orderStatus, style: const TextStyle(fontWeight: FontWeight.w700)),
              const Spacer(),
              StatusChip(s.status, s.statusName),
            ]),
            const SizedBox(height: 8),
            _Info(l.orderDate, Fmt.dateTime(s.createdAt)),
            _Info(l.payment, '${s.paymentMethod == PaymentMethod.online ? l.onlinePayment : l.cashOnDelivery} • ${s.paymentStatusName}'),
            if (o.trackingNumber?.isNotEmpty ?? false) _Info('', l.trackingNumber(o.trackingNumber!)),
          ]),
        ),
      ),
      if (unpaidOnline) ...[
        const SizedBox(height: 8),
        FilledButton.icon(icon: const Icon(Icons.credit_card), label: Text(l.payNow), onPressed: () => payOrder(context, ref, o.id)),
      ],
      _Title(l.trackOrder),
      Card(child: Padding(padding: const EdgeInsets.all(16), child: _Timeline(o.history))),
      _Title(l.orderItems),
      Card(
        child: Column(children: [
          for (final i in o.items)
            ListTile(
              onTap: () => context.push('/product/${i.productId}'),
              title: Text(i.productName, style: const TextStyle(fontWeight: FontWeight.w600)),
              subtitle: Text('${i.variantLabel} • ${m(i.unitPrice)} × ${i.quantity}'),
              trailing: Text(m(i.lineTotal), style: const TextStyle(fontWeight: FontWeight.w700)),
            ),
        ]),
      ),
      if (delivered) ...[
        _Title(l.rateProduct),
        Wrap(spacing: 8, runSpacing: 8, children: [
          for (final e in products.entries)
            ActionChip(
              avatar: const Icon(Icons.star_border, size: 18, color: HoneyColors.honey),
              label: Text(e.value),
              onPressed: () => showReviewSheet(context, productId: e.key),
            ),
        ]),
      ],
      _Title(l.orderSummary),
      Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(children: [
            AmountRow(l.subtotal, m(o.subtotal)),
            if (o.discountAmount > 0)
              AmountRow('${l.discount}${o.couponCode == null ? '' : ' (${o.couponCode})'}', '- ${m(o.discountAmount)}',
                  color: Colors.green.shade700),
            AmountRow(l.shipping, m(o.shippingFee)),
            const Divider(height: 20),
            AmountRow(l.total, m(s.total), bold: true),
          ]),
        ),
      ),
      _Title(l.shipTo),
      Card(
        child: ListTile(
          leading: const Icon(Icons.location_on_outlined),
          title: Text(o.shippingAddress.recipientName),
          subtitle: Text([
            o.shippingAddress.addressLine,
            '${o.shippingAddress.city}، ${o.shippingAddress.governorate}',
            if (o.shippingAddress.landmark?.isNotEmpty ?? false) o.shippingAddress.landmark!,
            o.shippingAddress.phone,
          ].join('\n')),
        ),
      ),
      if (o.notes?.isNotEmpty ?? false) ...[
        _Title(l.orderNotes),
        Card(child: Padding(padding: const EdgeInsets.all(16), child: Text(o.notes!))),
      ],
      if (o.canCancel) ...[
        const SizedBox(height: 20),
        OutlinedButton.icon(
          style: OutlinedButton.styleFrom(foregroundColor: Colors.red.shade700, side: BorderSide(color: Colors.red.shade200)),
          icon: const Icon(Icons.cancel_outlined),
          label: Text(l.cancelOrder),
          onPressed: () => _cancel(context, ref),
        ),
      ],
      const SizedBox(height: 24),
    ]);
  }
}

class _Title extends StatelessWidget {
  const _Title(this.text);
  final String text;

  @override
  Widget build(BuildContext context) =>
      Padding(padding: const EdgeInsets.only(top: 20, bottom: 8), child: Text(text, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)));
}

class _Info extends StatelessWidget {
  const _Info(this.label, this.value);
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: 4),
        child: Row(children: [
          if (label.isNotEmpty) Text('$label: ', style: TextStyle(color: Colors.grey.shade700)),
          Expanded(child: Text(value)),
        ]),
      );
}

/// Vertical status history, newest last.
class _Timeline extends StatelessWidget {
  const _Timeline(this.history);
  final List<StatusEntry> history;

  @override
  Widget build(BuildContext context) {
    final entries = [...history]..sort((a, b) => a.changedAt.compareTo(b.changedAt));
    return Column(children: [
      for (var i = 0; i < entries.length; i++)
        IntrinsicHeight(
          child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Column(children: [
              Container(
                width: 14,
                height: 14,
                margin: const EdgeInsets.only(top: 3),
                decoration: BoxDecoration(color: StatusChip.colorOf(entries[i].status), shape: BoxShape.circle),
              ),
              if (i < entries.length - 1) Expanded(child: Container(width: 2, color: Colors.grey.shade300)),
            ]),
            const SizedBox(width: 12),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(entries[i].statusName, style: const TextStyle(fontWeight: FontWeight.w700)),
                  Text(Fmt.dateTime(entries[i].changedAt), style: TextStyle(color: Colors.grey.shade700, fontSize: 12.5)),
                  if (entries[i].note?.isNotEmpty ?? false) Text(entries[i].note!),
                ]),
              ),
            ),
          ]),
        ),
    ]);
  }
}
