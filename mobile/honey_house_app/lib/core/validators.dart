import '../l10n/app_localizations.dart';

/// Same rules as the API (FluentValidation) so most mistakes are caught before sending.
class Validators {
  Validators(this.l);
  final AppLocalizations l;

  static final _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
  static final _egyptMobile = RegExp(r'^01[0125][0-9]{8}$');

  String? required(String? v) => (v == null || v.trim().isEmpty) ? l.required : null;

  String? email(String? v) => required(v) ?? (_email.hasMatch(v!.trim()) ? null : l.emailInvalid);

  String? phone(String? v) => required(v) ?? (_egyptMobile.hasMatch(v!.trim()) ? null : l.phoneInvalid);

  String? password(String? v) {
    if (v == null || v.isEmpty) return l.required;
    final ok = v.length >= 8 && RegExp('[a-z]').hasMatch(v) && RegExp('[0-9]').hasMatch(v);
    return ok ? null : l.passwordRule;
  }
}
