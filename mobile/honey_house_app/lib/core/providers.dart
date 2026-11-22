import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'api/api_client.dart';
import 'storage/token_storage.dart';

/// Overridden in main() with the loaded instance.
final sharedPreferencesProvider = Provider<SharedPreferences>((ref) => throw UnimplementedError());

final tokenStorageProvider = Provider<TokenStorage>((ref) => SecureTokenStorage());

/// Fires when the server rejects the refresh token, so the UI can sign the user out.
final sessionExpiredProvider = Provider<StreamController<void>>((ref) {
  final c = StreamController<void>.broadcast();
  ref.onDispose(c.close);
  return c;
});

/// App language: Arabic by default, remembered on the device.
class LocaleController extends Notifier<Locale> {
  static const _key = 'hh_language';

  @override
  Locale build() => Locale(ref.read(sharedPreferencesProvider).getString(_key) ?? 'ar');

  Future<void> set(String languageCode) async {
    state = Locale(languageCode);
    await ref.read(sharedPreferencesProvider).setString(_key, languageCode);
  }
}

final localeProvider = NotifierProvider<LocaleController, Locale>(LocaleController.new);

final apiClientProvider = Provider<ApiClient>((ref) => ApiClient(
      tokens: ref.watch(tokenStorageProvider),
      language: () => ref.read(localeProvider).languageCode,
      onSessionExpired: () => ref.read(sessionExpiredProvider).add(null),
    ));
