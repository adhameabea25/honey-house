import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/format.dart';
import '../../core/l10n.dart';
import '../../core/validators.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import '../auth/auth_controller.dart';
import 'address_models.dart';
import 'address_providers.dart';

class AddressFormScreen extends ConsumerWidget {
  const AddressFormScreen({super.key, this.addressId});
  final int? addressId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final governorates = ref.watch(governoratesProvider);
    final addresses = ref.watch(addressesProvider);
    return Scaffold(
      appBar: AppBar(title: Text(addressId == null ? l.addAddress : l.editAddress)),
      body: AsyncBody(
        value: governorates,
        onRetry: () => ref.invalidate(governoratesProvider),
        builder: (govs) {
          if (addressId != null && !addresses.hasValue) return const Center(child: CircularProgressIndicator());
          final existing = addresses.value?.where((a) => a.id == addressId).firstOrNull;
          return _AddressForm(governorates: govs, existing: existing, isFirst: addresses.value?.isEmpty ?? true);
        },
      ),
    );
  }
}

class _AddressForm extends ConsumerStatefulWidget {
  const _AddressForm({required this.governorates, this.existing, required this.isFirst});
  final List<Governorate> governorates;
  final Address? existing;
  final bool isFirst;

  @override
  ConsumerState<_AddressForm> createState() => _AddressFormState();
}

class _AddressFormState extends ConsumerState<_AddressForm> {
  final _form = GlobalKey<FormState>();
  late final Map<String, TextEditingController> _c;
  late int? _governorateId = widget.existing?.governorateId;
  late bool _isDefault = widget.existing?.isDefault ?? widget.isFirst;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    final a = widget.existing;
    final user = ref.read(authProvider).value;
    _c = {
      'recipient': TextEditingController(text: a?.recipientName ?? user?.fullName),
      'phone': TextEditingController(text: a?.phone ?? user?.phoneNumber),
      'city': TextEditingController(text: a?.city),
      'area': TextEditingController(text: a?.area),
      'street': TextEditingController(text: a?.street),
      'building': TextEditingController(text: a?.building),
      'floor': TextEditingController(text: a?.floor),
      'apartment': TextEditingController(text: a?.apartment),
      'landmark': TextEditingController(text: a?.landmark),
    };
  }

  @override
  void dispose() {
    for (final c in _c.values) {
      c.dispose();
    }
    super.dispose();
  }

  String? _opt(String key) => _c[key]!.text.trim().isEmpty ? null : _c[key]!.text.trim();

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    setState(() => _busy = true);
    try {
      final saved = await ref.read(addressesProvider.notifier).save(
            AddressInput(
              governorateId: _governorateId!,
              recipientName: _c['recipient']!.text.trim(),
              phone: _c['phone']!.text.trim(),
              city: _c['city']!.text.trim(),
              area: _opt('area'),
              street: _c['street']!.text.trim(),
              building: _opt('building'),
              floor: _opt('floor'),
              apartment: _opt('apartment'),
              landmark: _opt('landmark'),
              isDefault: _isDefault,
            ),
            id: widget.existing?.id,
          );
      if (mounted) context.pop(saved);
    } catch (e) {
      if (mounted) showError(context, e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    final v = Validators(l);
    Widget field(String key, String label, {bool required = false, TextInputType? type}) => Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: TextFormField(
            controller: _c[key],
            validator: required ? v.required : null,
            keyboardType: type,
            textInputAction: TextInputAction.next,
            decoration: InputDecoration(labelText: label),
          ),
        );
    Widget half(String key, String label, {bool required = false}) => Expanded(child: field(key, label, required: required));

    return Form(
      key: _form,
      child: ListView(padding: const EdgeInsets.all(20), children: [
        field('recipient', l.recipientName, required: true),
        Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: LtrField(controller: _c['phone']!, label: l.phone, hint: l.phoneHint, validator: v.phone,
              keyboardType: TextInputType.phone),
        ),
        Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: DropdownButtonFormField<int>(
            initialValue: _governorateId,
            isExpanded: true,
            decoration: InputDecoration(labelText: l.governorate),
            validator: (x) => x == null ? l.required : null,
            items: [
              for (final g in widget.governorates)
                DropdownMenuItem(
                  value: g.id,
                  child: Text('${g.name}  (${l.shippingFeeValue(Fmt.money(g.shippingFee, 'EGP'))})', overflow: TextOverflow.ellipsis),
                ),
            ],
            onChanged: (x) => setState(() => _governorateId = x),
          ),
        ),
        Row(children: [half('city', l.city, required: true), const SizedBox(width: 12), half('area', l.area)]),
        field('street', l.street, required: true),
        Row(children: [
          half('building', l.building),
          const SizedBox(width: 12),
          half('floor', l.floor),
          const SizedBox(width: 12),
          half('apartment', l.apartment),
        ]),
        field('landmark', l.landmark),
        SwitchListTile(
          contentPadding: EdgeInsets.zero,
          title: Text(l.setDefault),
          value: _isDefault,
          onChanged: (x) => setState(() => _isDefault = x),
        ),
        const SizedBox(height: 12),
        BusyButton(label: l.save, busy: _busy, onPressed: _save),
      ]),
    );
  }
}
