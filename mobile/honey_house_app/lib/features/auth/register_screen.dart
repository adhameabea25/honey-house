import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/validators.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import 'auth_controller.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key, this.from});
  final String? from;

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _form = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  final _password = TextEditingController();
  bool _busy = false;

  @override
  void dispose() {
    for (final c in [_name, _email, _phone, _password]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    setState(() => _busy = true);
    try {
      await ref.read(authProvider.notifier).register(
          fullName: _name.text.trim(), email: _email.text.trim(), phone: _phone.text.trim(), password: _password.text);
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
      appBar: AppBar(title: Text(l.register)),
      body: SafeArea(
        child: Form(
          key: _form,
          child: ListView(
            padding: const EdgeInsets.all(24),
            children: [
              TextFormField(
                controller: _name,
                validator: v.required,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.name],
                decoration: InputDecoration(labelText: l.fullName, prefixIcon: const Icon(Icons.person_outline)),
              ),
              const SizedBox(height: 16),
              LtrField(controller: _email, label: l.email, icon: Icons.email_outlined, validator: v.email,
                  keyboardType: TextInputType.emailAddress, autofillHints: const [AutofillHints.email]),
              const SizedBox(height: 16),
              LtrField(controller: _phone, label: l.phone, hint: l.phoneHint, icon: Icons.phone_iphone, validator: v.phone,
                  keyboardType: TextInputType.phone, autofillHints: const [AutofillHints.telephoneNumber]),
              const SizedBox(height: 16),
              PasswordField(controller: _password, label: l.password, validator: v.password,
                  textInputAction: TextInputAction.done, onSubmitted: (_) => _submit()),
              Padding(
                padding: const EdgeInsets.only(top: 6, bottom: 20),
                child: Text(l.passwordRule, style: Theme.of(context).textTheme.bodySmall),
              ),
              BusyButton(label: l.register, busy: _busy, onPressed: _submit),
              const SizedBox(height: 16),
              Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                Text(l.haveAccount),
                TextButton(
                  onPressed: () => context.pushReplacement(
                      Uri(path: '/login', queryParameters: {if (widget.from != null) 'from': widget.from}).toString()),
                  child: Text(l.login),
                ),
              ]),
            ],
          ),
        ),
      ),
    );
  }
}
