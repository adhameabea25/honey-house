import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/validators.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import 'auth_controller.dart';

/// Two steps: request a code by email, then enter the code with a new password.
class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  final _form = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _code = TextEditingController();
  final _password = TextEditingController();
  bool _codeSent = false;
  bool _busy = false;

  @override
  void dispose() {
    for (final c in [_email, _code, _password]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    final l = context.l10n;
    final auth = ref.read(authProvider.notifier);
    setState(() => _busy = true);
    try {
      if (!_codeSent) {
        await auth.forgotPassword(_email.text);
        if (!mounted) return;
        showMessage(context, l.resetCodeSent);
        setState(() => _codeSent = true);
      } else {
        await auth.resetPassword(_email.text, _code.text, _password.text);
        if (!mounted) return;
        showMessage(context, l.passwordChanged);
        context.pop();
      }
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
      appBar: AppBar(title: Text(l.forgotPasswordTitle)),
      body: SafeArea(
        child: Form(
          key: _form,
          child: ListView(padding: const EdgeInsets.all(24), children: [
            Text(l.forgotPasswordHint),
            const SizedBox(height: 20),
            LtrField(controller: _email, label: l.email, icon: Icons.email_outlined, validator: v.email,
                keyboardType: TextInputType.emailAddress),
            if (_codeSent) ...[
              const SizedBox(height: 16),
              LtrField(controller: _code, label: l.resetCode, icon: Icons.pin_outlined, validator: v.required),
              const SizedBox(height: 16),
              PasswordField(controller: _password, label: l.newPassword, validator: v.password),
            ],
            const SizedBox(height: 24),
            BusyButton(label: _codeSent ? l.resetPasswordButton : l.sendCode, busy: _busy, onPressed: _submit),
          ]),
        ),
      ),
    );
  }
}
