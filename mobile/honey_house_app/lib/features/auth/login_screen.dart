import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/validators.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import 'auth_controller.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key, this.from});
  final String? from;

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _form = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _busy = false;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    final router = GoRouter.of(context);
    setState(() => _busy = true);
    try {
      await ref.read(authProvider.notifier).login(_email.text.trim(), _password.text);
      leaveAuthScreen(router, widget.from);
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
      appBar: AppBar(title: Text(l.login)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            const Center(child: Text('🍯', style: TextStyle(fontSize: 64))),
            const SizedBox(height: 8),
            Center(child: Text(l.welcomeBack, style: Theme.of(context).textTheme.headlineSmall)),
            const SizedBox(height: 24),
            Form(
              key: _form,
              child: AutofillGroup(
                child: Column(children: [
                  LtrField(controller: _email, label: l.email, icon: Icons.email_outlined, validator: v.email,
                      keyboardType: TextInputType.emailAddress, autofillHints: const [AutofillHints.email]),
                  const SizedBox(height: 16),
                  PasswordField(controller: _password, label: l.password, validator: v.required,
                      textInputAction: TextInputAction.done, onSubmitted: (_) => _submit()),
                ]),
              ),
            ),
            Align(
              alignment: AlignmentDirectional.centerEnd,
              child: TextButton(onPressed: () => context.push('/forgot-password'), child: Text(l.forgotPassword)),
            ),
            const SizedBox(height: 8),
            BusyButton(label: l.login, busy: _busy, onPressed: _submit),
            const SizedBox(height: 16),
            Wrap(alignment: WrapAlignment.center, crossAxisAlignment: WrapCrossAlignment.center, children: [
              Text(l.noAccount),
              TextButton(
                onPressed: () => context.pushReplacement(
                    Uri(path: '/register', queryParameters: {if (widget.from != null) 'from': widget.from}).toString()),
                child: Text(l.register),
              ),
            ]),
          ],
        ),
      ),
    );
  }
}
