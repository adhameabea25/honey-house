import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/validators.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import '../auth/auth_controller.dart';

class ProfileScreen extends ConsumerStatefulWidget {
  const ProfileScreen({super.key});

  @override
  ConsumerState<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends ConsumerState<ProfileScreen> {
  final _form = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _phone;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    final user = ref.read(authProvider).value;
    _name = TextEditingController(text: user?.fullName);
    _phone = TextEditingController(text: user?.phoneNumber);
  }

  @override
  void dispose() {
    _name.dispose();
    _phone.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    setState(() => _busy = true);
    try {
      await ref.read(authProvider.notifier).updateProfile(fullName: _name.text, phone: _phone.text);
      if (!mounted) return;
      showMessage(context, context.l10n.profileSaved);
      context.pop();
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
    final user = ref.watch(authProvider).value;
    return Scaffold(
      appBar: AppBar(title: Text(l.myProfile)),
      body: Form(
        key: _form,
        child: ListView(padding: const EdgeInsets.all(24), children: [
          TextFormField(
            controller: _name,
            validator: v.required,
            decoration: InputDecoration(labelText: l.fullName, prefixIcon: const Icon(Icons.person_outline)),
          ),
          const SizedBox(height: 16),
          TextFormField(
            initialValue: user?.email,
            enabled: false,
            textDirection: TextDirection.ltr,
            decoration: InputDecoration(labelText: l.email, prefixIcon: const Icon(Icons.email_outlined)),
          ),
          const SizedBox(height: 16),
          LtrField(controller: _phone, label: l.phone, hint: l.phoneHint, icon: Icons.phone_iphone, validator: v.phone,
              keyboardType: TextInputType.phone),
          const SizedBox(height: 24),
          BusyButton(label: l.save, busy: _busy, onPressed: _save),
        ]),
      ),
    );
  }
}
