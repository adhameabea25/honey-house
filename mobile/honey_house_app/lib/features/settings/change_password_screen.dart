import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/validators.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import '../auth/auth_controller.dart';

class ChangePasswordScreen extends ConsumerStatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  ConsumerState<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends ConsumerState<ChangePasswordScreen> {
  final _form = GlobalKey<FormState>();
  final _current = TextEditingController();
  final _next = TextEditingController();
  bool _busy = false;

  @override
  void dispose() {
    _current.dispose();
    _next.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    setState(() => _busy = true);
    try {
      await ref.read(authProvider.notifier).changePassword(_current.text, _next.text);
      if (!mounted) return;
      showMessage(context, context.l10n.passwordChanged);
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
    return Scaffold(
      appBar: AppBar(title: Text(l.changePassword)),
      body: Form(
        key: _form,
        child: ListView(padding: const EdgeInsets.all(24), children: [
          PasswordField(controller: _current, label: l.currentPassword, validator: v.required),
          const SizedBox(height: 16),
          PasswordField(controller: _next, label: l.newPassword, validator: v.password),
          Padding(padding: const EdgeInsets.only(top: 6, bottom: 20), child: Text(l.passwordRule, style: Theme.of(context).textTheme.bodySmall)),
          BusyButton(label: l.save, busy: _busy, onPressed: _save),
        ]),
      ),
    );
  }
}
