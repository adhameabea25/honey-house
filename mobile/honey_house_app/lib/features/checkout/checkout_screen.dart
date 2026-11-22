import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/format.dart';
import '../../core/l10n.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import '../addresses/address_models.dart';
import '../addresses/address_providers.dart';
import '../addresses/addresses_screen.dart' show AddressCard;
import '../cart/cart_controller.dart';
import '../orders/order_models.dart';
import '../orders/order_providers.dart';

/// Address, coupon, payment method and the server-calculated totals, then place the order.
class CheckoutScreen extends ConsumerStatefulWidget {
  const CheckoutScreen({super.key});

  @override
  ConsumerState<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends ConsumerState<CheckoutScreen> {
  Address? _address;
  String? _coupon;
  PaymentMethod _method = PaymentMethod.cashOnDelivery;
  final _couponCtrl = TextEditingController();
  final _notes = TextEditingController();
  Future<CheckoutSummary>? _summary;
  bool _placing = false;

  @override
  void dispose() {
    _couponCtrl.dispose();
    _notes.dispose();
    super.dispose();
  }

  void _refresh() {
    final a = _address;
    setState(() => _summary = a == null ? null : ref.read(orderRepositoryProvider).preview(a.id, _coupon));
  }

  Future<void> _pickAddress() async {
    final picked = await context.push<Address>('/addresses?select=1');
    if (picked != null) {
      _address = picked;
      _refresh();
    }
  }

  Future<void> _applyCoupon() async {
    final code = _couponCtrl.text.trim();
    _coupon = code.isEmpty ? null : code;
    _refresh();
    final s = await _summary;
    if (s?.couponError != null && mounted) setState(() => _coupon = null);
  }

  Future<void> _place(CheckoutSummary s) async {
    setState(() => _placing = true);
    try {
      final placed = await ref
          .read(orderRepositoryProvider)
          .place(addressId: _address!.id, method: _method, coupon: s.couponCode, notes: _notes.text);
      ref.read(cartProvider.notifier).reset();
      ref.invalidate(myOrdersProvider);
      if (!mounted) return;
      final id = placed.order.id;
      if (placed.paymentUrl != null) {
        context.go('/order-placed/$id');
        context.push(Uri(path: '/payment/$id', queryParameters: {'url': placed.paymentUrl}).toString());
      } else {
        context.go('/order-placed/$id');
      }
    } catch (e) {
      if (mounted) showError(context, e);
      _refresh();
    } finally {
      if (mounted) setState(() => _placing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    final addresses = ref.watch(addressesProvider);

    // Start with the default address once the list loads.
    if (_address == null && addresses.value != null && addresses.value!.isNotEmpty) {
      _address = addresses.value!.firstWhere((a) => a.isDefault, orElse: () => addresses.value!.first);
      _summary = ref.read(orderRepositoryProvider).preview(_address!.id, _coupon);
    }

    return Scaffold(
      appBar: AppBar(title: Text(l.checkout)),
      body: AsyncBody(
        value: addresses,
        onRetry: () => ref.invalidate(addressesProvider),
        builder: (list) => ListView(padding: const EdgeInsets.all(16), children: [
          _Heading(l.deliveryAddress, action: _address == null ? null : TextButton(onPressed: _pickAddress, child: Text(l.change))),
          if (_address == null)
            Card(
              child: ListTile(
                leading: const Icon(Icons.add_location_alt_outlined),
                title: Text(l.addAddressFirst),
                onTap: () async {
                  final created = await context.push<Address>('/addresses/new');
                  if (created != null) {
                    _address = created;
                    _refresh();
                  }
                },
              ),
            )
          else
            AddressCard(_address!, onTap: _pickAddress),
          _Heading(l.paymentMethod),
          Card(
            child: RadioGroup<PaymentMethod>(
              groupValue: _method,
              onChanged: (m) => setState(() => _method = m!),
              child: Column(children: [
                RadioListTile(value: PaymentMethod.cashOnDelivery, title: Text(l.cashOnDelivery),
                    secondary: const Icon(Icons.payments_outlined)),
                RadioListTile(value: PaymentMethod.online, title: Text(l.onlinePayment),
                    subtitle: Text(l.paymentPendingNote, style: const TextStyle(fontSize: 12)),
                    secondary: const Icon(Icons.credit_card)),
              ]),
            ),
          ),
          _Heading(l.couponCode),
          Row(children: [
            Expanded(
              child: TextField(
                controller: _couponCtrl,
                textCapitalization: TextCapitalization.characters,
                textDirection: TextDirection.ltr,
                decoration: InputDecoration(hintText: l.couponCode, prefixIcon: const Icon(Icons.local_offer_outlined)),
                onSubmitted: (_) => _applyCoupon(),
              ),
            ),
            const SizedBox(width: 8),
            if (_coupon == null)
              OutlinedButton(onPressed: _address == null ? null : _applyCoupon, child: Text(l.applyCoupon))
            else
              TextButton(
                onPressed: () {
                  _couponCtrl.clear();
                  _coupon = null;
                  _refresh();
                },
                child: Text(l.removeCoupon),
              ),
          ]),
          const SizedBox(height: 12),
          TextField(controller: _notes, maxLines: 2, maxLength: 500, decoration: InputDecoration(labelText: l.orderNotes)),
          if (_summary != null)
            FutureBuilder<CheckoutSummary>(
              future: _summary,
              builder: (context, snap) {
                if (snap.hasError) return ErrorView(error: snap.error!, onRetry: _refresh);
                if (!snap.hasData) return const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator()));
                return _Totals(summary: snap.data!, placing: _placing, onPlace: () => _place(snap.data!));
              },
            ),
        ]),
      ),
    );
  }
}

class _Heading extends StatelessWidget {
  const _Heading(this.text, {this.action});
  final String text;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: 16, bottom: 8),
        child: Row(children: [
          Expanded(child: Text(text, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16))),
          ?action,
        ]),
      );
}

class _Totals extends StatelessWidget {
  const _Totals({required this.summary, required this.placing, required this.onPlace});
  final CheckoutSummary summary;
  final bool placing;
  final VoidCallback onPlace;

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    final s = summary;
    String m(double v) => Fmt.money(v, s.currency);
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      _Heading(l.orderSummary),
      Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(children: [
            for (final i in s.items)
              AmountRow('${i.productName} (${i.variantLabel}) × ${i.quantity}', m(i.lineTotal)),
            const Divider(height: 20),
            AmountRow(l.subtotal, m(s.subtotal)),
            if (s.discountAmount > 0)
              AmountRow('${l.discount} (${s.couponCode})', '- ${m(s.discountAmount)}', color: Colors.green.shade700),
            AmountRow(l.shipping, m(s.shippingFee)),
            const Divider(height: 20),
            AmountRow(l.total, m(s.total), bold: true),
          ]),
        ),
      ),
      if (s.couponError != null)
        Padding(padding: const EdgeInsets.only(top: 8), child: Text(s.couponError!, style: TextStyle(color: Colors.red.shade700))),
      if (!s.canPlaceOrder && s.blockingReason != null)
        Padding(padding: const EdgeInsets.only(top: 8), child: Text(s.blockingReason!, style: TextStyle(color: Colors.red.shade700))),
      const SizedBox(height: 16),
      BusyButton(label: '${l.placeOrder}  •  ${m(s.total)}', busy: placing, onPressed: s.canPlaceOrder ? onPlace : null),
      const SizedBox(height: 24),
    ]);
  }
}
