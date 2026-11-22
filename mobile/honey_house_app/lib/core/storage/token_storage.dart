import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class StoredTokens {
  const StoredTokens(this.accessToken, this.refreshToken);
  final String accessToken;
  final String refreshToken;
}

/// Keeps the JWT pair. The secure implementation uses Keychain / Android Keystore.
abstract class TokenStorage {
  Future<StoredTokens?> read();
  Future<void> write(StoredTokens tokens);
  Future<void> clear();
}

class SecureTokenStorage implements TokenStorage {
  SecureTokenStorage([FlutterSecureStorage? storage]) : _storage = storage ?? const FlutterSecureStorage();
  final FlutterSecureStorage _storage;
  static const _access = 'hh_access_token';
  static const _refresh = 'hh_refresh_token';

  @override
  Future<StoredTokens?> read() async {
    final a = await _storage.read(key: _access);
    final r = await _storage.read(key: _refresh);
    return a == null || r == null ? null : StoredTokens(a, r);
  }

  @override
  Future<void> write(StoredTokens tokens) async {
    await _storage.write(key: _access, value: tokens.accessToken);
    await _storage.write(key: _refresh, value: tokens.refreshToken);
  }

  @override
  Future<void> clear() async {
    await _storage.delete(key: _access);
    await _storage.delete(key: _refresh);
  }
}

class MemoryTokenStorage implements TokenStorage {
  StoredTokens? tokens;
  @override
  Future<StoredTokens?> read() async => tokens;
  @override
  Future<void> write(StoredTokens t) async => tokens = t;
  @override
  Future<void> clear() async => tokens = null;
}
