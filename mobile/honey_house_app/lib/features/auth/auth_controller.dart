import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_exception.dart';
import '../../core/providers.dart';
import '../../core/storage/token_storage.dart';
import 'auth_models.dart';

/// Signed-in customer, or null for guests. Catalog browsing works without an account.
class AuthController extends AsyncNotifier<AppUser?> {
  @override
  Future<AppUser?> build() async {
    final sub = ref.read(sessionExpiredProvider).stream.listen((_) => state = const AsyncData(null));
    ref.onDispose(sub.cancel);

    if (await ref.read(tokenStorageProvider).read() == null) return null;
    try {
      return await _me();
    } on ApiException catch (e) {
      if (e.isUnauthorized) {
        await ref.read(tokenStorageProvider).clear();
        return null;
      }
      rethrow;
    }
  }

  Future<AppUser> _me() async =>
      AppUser.fromJson(await ref.read(apiClientProvider).get<Map<String, dynamic>>('/auth/me'));

  Future<void> login(String email, String password) =>
      _authenticate('/auth/login', {'email': email.trim(), 'password': password});

  Future<void> register({required String fullName, required String email, required String phone, required String password}) =>
      _authenticate('/auth/register', {
        'fullName': fullName.trim(), 'email': email.trim(), 'phoneNumber': phone.trim(), 'password': password,
        'preferredLanguage': ref.read(localeProvider).languageCode,
      });

  Future<void> _authenticate(String path, Map<String, dynamic> body) async {
    final r = await ref.read(apiClientProvider).post<Map<String, dynamic>>(path, data: body, auth: false);
    await ref.read(tokenStorageProvider).write(StoredTokens(r['accessToken'] as String, r['refreshToken'] as String));
    state = AsyncData(AppUser.fromJson(r['user'] as Map<String, dynamic>));
  }

  Future<void> logout() async {
    final tokens = await ref.read(tokenStorageProvider).read();
    if (tokens != null) {
      try {
        await ref.read(apiClientProvider).post<void>('/auth/logout', data: {'refreshToken': tokens.refreshToken});
      } on ApiException {
        // Signing out locally is enough if the server is unreachable.
      }
    }
    await ref.read(tokenStorageProvider).clear();
    state = const AsyncData(null);
  }

  Future<void> updateProfile({required String fullName, required String phone}) async {
    final r = await ref.read(apiClientProvider).put<Map<String, dynamic>>('/auth/me', data: {
      'fullName': fullName.trim(), 'phoneNumber': phone.trim(), 'preferredLanguage': ref.read(localeProvider).languageCode,
    });
    state = AsyncData(AppUser.fromJson(r));
  }

  /// Keeps the server-side preference in sync (used for emails) when the user switches language.
  Future<void> syncLanguage(String language) async {
    final user = state.value;
    if (user == null || user.preferredLanguage == language) return;
    try {
      final r = await ref.read(apiClientProvider).put<Map<String, dynamic>>('/auth/me',
          data: {'fullName': user.fullName, 'phoneNumber': user.phoneNumber, 'preferredLanguage': language});
      state = AsyncData(AppUser.fromJson(r));
    } on ApiException {
      // Not critical.
    }
  }

  Future<void> changePassword(String current, String next) =>
      ref.read(apiClientProvider).post<void>('/auth/change-password', data: {'currentPassword': current, 'newPassword': next});

  Future<void> forgotPassword(String email) =>
      ref.read(apiClientProvider).post<void>('/auth/forgot-password', data: {'email': email.trim()}, auth: false);

  Future<void> resetPassword(String email, String token, String newPassword) => ref.read(apiClientProvider)
      .post<void>('/auth/reset-password', data: {'email': email.trim(), 'token': token.trim(), 'newPassword': newPassword}, auth: false);
}

final authProvider = AsyncNotifierProvider<AuthController, AppUser?>(AuthController.new);

/// True once the customer is signed in.
final isSignedInProvider = Provider<bool>((ref) => ref.watch(authProvider).value != null);
