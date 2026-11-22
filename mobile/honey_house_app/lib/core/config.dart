/// Build-time configuration. Override with `--dart-define=API_BASE_URL=https://api.example.com`.
class AppConfig {
  /// Android emulator reaches the host machine on 10.0.2.2.
  static const apiBaseUrl = String.fromEnvironment('API_BASE_URL', defaultValue: 'http://10.0.2.2:5080');

  static String get apiRoot => '$apiBaseUrl/api/v1';

  /// Image paths from the API are relative (`/uploads/...`); make them absolute.
  static String? imageUrl(String? path) {
    if (path == null || path.isEmpty) return null;
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    return '$apiBaseUrl${path.startsWith('/') ? '' : '/'}$path';
  }
}
