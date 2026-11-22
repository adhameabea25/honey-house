import 'dart:async';

import 'package:dio/dio.dart';

import '../config.dart';
import '../storage/token_storage.dart';
import 'api_exception.dart';

/// HTTP client for the Honey House API:
/// sends the UI language, attaches the access token, and refreshes it once on 401
/// (concurrent requests share a single refresh; refresh tokens rotate on the server).
class ApiClient {
  ApiClient({required this.tokens, required this.language, Dio? dio, this.onSessionExpired})
      : dio = dio ?? Dio(BaseOptions(baseUrl: AppConfig.apiRoot, connectTimeout: const Duration(seconds: 15),
            receiveTimeout: const Duration(seconds: 30))) {
    this.dio.interceptors.add(InterceptorsWrapper(onRequest: _onRequest, onError: _onError));
  }

  final Dio dio;
  final TokenStorage tokens;

  /// Current UI language ("ar" or "en"), read on every request.
  final String Function() language;

  /// Called when the refresh token is rejected (user must sign in again).
  final void Function()? onSessionExpired;

  Future<StoredTokens?>? _refreshing;

  Future<void> _onRequest(RequestOptions o, RequestInterceptorHandler h) async {
    o.headers['Accept-Language'] = language();
    if (o.extra['auth'] != false) {
      final t = await tokens.read();
      if (t != null) o.headers['Authorization'] = 'Bearer ${t.accessToken}';
    }
    h.next(o);
  }

  Future<void> _onError(DioException e, ErrorInterceptorHandler h) async {
    final o = e.requestOptions;
    if (e.response?.statusCode != 401 || o.extra['auth'] == false || o.extra['retried'] == true) {
      return h.next(e);
    }
    final current = await tokens.read();
    if (current == null) return h.next(e);

    final fresh = await (_refreshing ??= _refresh(current.refreshToken).whenComplete(() => _refreshing = null));
    if (fresh == null) return h.next(e);

    try {
      o.extra['retried'] = true;
      o.headers['Authorization'] = 'Bearer ${fresh.accessToken}';
      h.resolve(await dio.fetch(o));
    } on DioException catch (err) {
      h.next(err);
    }
  }

  Future<StoredTokens?> _refresh(String refreshToken) async {
    try {
      final r = await dio.post('/auth/refresh', data: {'refreshToken': refreshToken}, options: Options(extra: {'auth': false}));
      final t = StoredTokens(r.data['accessToken'] as String, r.data['refreshToken'] as String);
      await tokens.write(t);
      return t;
    } on DioException catch (e) {
      if (e.response?.statusCode == 401 || e.response?.statusCode == 400) {
        await tokens.clear();
        onSessionExpired?.call();
      }
      return null;
    }
  }

  // ---- Helpers that turn Dio errors into ApiException ----

  Future<T> get<T>(String path, {Map<String, dynamic>? query, bool auth = true}) =>
      _wrap(() => dio.get<T>(path, queryParameters: _clean(query), options: Options(extra: {'auth': auth})));

  Future<T> post<T>(String path, {Object? data, bool auth = true}) =>
      _wrap(() => dio.post<T>(path, data: data, options: Options(extra: {'auth': auth})));

  Future<T> put<T>(String path, {Object? data}) => _wrap(() => dio.put<T>(path, data: data));

  Future<T> delete<T>(String path) => _wrap(() => dio.delete<T>(path));

  static Map<String, dynamic>? _clean(Map<String, dynamic>? q) =>
      q == null ? null : (Map.of(q)..removeWhere((_, v) => v == null || v == ''));

  Future<T> _wrap<T>(Future<Response<T>> Function() call) async {
    try {
      return (await call()).data as T;
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
