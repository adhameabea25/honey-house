import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../l10n.dart';

/// Primary button that shows a spinner while [busy].
class BusyButton extends StatelessWidget {
  const BusyButton({super.key, required this.label, required this.onPressed, this.busy = false, this.icon});
  final String label;
  final VoidCallback? onPressed;
  final bool busy;
  final IconData? icon;

  @override
  Widget build(BuildContext context) => SizedBox(
        width: double.infinity,
        child: FilledButton.icon(
          onPressed: busy ? null : onPressed,
          icon: busy
              ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : (icon == null ? const SizedBox.shrink() : Icon(icon)),
          label: Text(label),
        ),
      );
}

class PasswordField extends StatefulWidget {
  const PasswordField({super.key, required this.controller, required this.label, this.validator, this.textInputAction, this.onSubmitted});
  final TextEditingController controller;
  final String label;
  final FormFieldValidator<String>? validator;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onSubmitted;

  @override
  State<PasswordField> createState() => _PasswordFieldState();
}

class _PasswordFieldState extends State<PasswordField> {
  bool _hidden = true;

  @override
  Widget build(BuildContext context) => TextFormField(
        controller: widget.controller,
        obscureText: _hidden,
        validator: widget.validator,
        textInputAction: widget.textInputAction,
        onFieldSubmitted: widget.onSubmitted,
        autofillHints: const [AutofillHints.password],
        decoration: InputDecoration(
          labelText: widget.label,
          prefixIcon: const Icon(Icons.lock_outline),
          suffixIcon: IconButton(
            icon: Icon(_hidden ? Icons.visibility_outlined : Icons.visibility_off_outlined),
            onPressed: () => setState(() => _hidden = !_hidden),
          ),
        ),
      );
}

/// Latin-digit input (email, phone, numbers) stays left-to-right inside Arabic screens.
class LtrField extends StatelessWidget {
  const LtrField({super.key, required this.controller, required this.label, this.validator, this.keyboardType, this.icon,
      this.hint, this.autofillHints, this.textInputAction});
  final TextEditingController controller;
  final String label;
  final FormFieldValidator<String>? validator;
  final TextInputType? keyboardType;
  final IconData? icon;
  final String? hint;
  final Iterable<String>? autofillHints;
  final TextInputAction? textInputAction;

  @override
  Widget build(BuildContext context) => TextFormField(
        controller: controller,
        validator: validator,
        keyboardType: keyboardType,
        textDirection: TextDirection.ltr,
        autofillHints: autofillHints,
        textInputAction: textInputAction ?? TextInputAction.next,
        decoration: InputDecoration(labelText: label, hintText: hint, prefixIcon: icon == null ? null : Icon(icon)),
      );
}

/// Opens the sign-in screen and returns here afterwards.
void goToLogin(BuildContext context) {
  final here = GoRouterState.of(context).uri.toString();
  context.push(Uri(path: '/login', queryParameters: {'from': here}).toString());
}

/// Shown on tabs that need an account (cart, favorites) when browsing as a guest.
class SignInPrompt extends StatelessWidget {
  const SignInPrompt({super.key, required this.icon});
  final IconData icon;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            Icon(icon, size: 64, color: Theme.of(context).colorScheme.secondary),
            const SizedBox(height: 16),
            Text(context.l10n.loginToContinue, style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 20),
            FilledButton(onPressed: () => goToLogin(context), child: Text(context.l10n.login)),
          ]),
        ),
      );
}

Future<bool> confirmDialog(BuildContext context, String text, {String? confirmLabel, bool destructive = false}) async {
  final l = context.l10n;
  final ok = await showDialog<bool>(
    context: context,
    builder: (c) => AlertDialog(
      content: Text(text),
      actions: [
        TextButton(onPressed: () => Navigator.pop(c, false), child: Text(l.cancel)),
        FilledButton(
          style: destructive ? FilledButton.styleFrom(backgroundColor: Colors.red.shade700, foregroundColor: Colors.white) : null,
          onPressed: () => Navigator.pop(c, true),
          child: Text(confirmLabel ?? l.confirm),
        ),
      ],
    ),
  );
  return ok ?? false;
}
