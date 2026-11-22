import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:honey_house/core/api/api_client.dart';
import 'package:honey_house/core/api/api_exception.dart';
import 'package:honey_house/core/storage/token_storage.dart';

import 'helpers.dart';

void main() {
  late MemoryTokenStorage tokens;
  late int expired;

  ApiClient client(FakeApi api, {String lang = 'en'}) => ApiClient(
        tokens: tokens,
        language: () => lang,
        dio: Dio(BaseOptions(baseUrl: 'http://test/api/v1'))..httpClientAdapter = api,
        onSessionExpired: () => expired++,
      );

  setUp(() async {
    tokens = MemoryTokenStorage();
    await tokens.write(StoredTokens('old-access', 'old-refresh'));
    expired = 0;
  });

  test('sends language and bearer token', () async {
    final api = FakeApi({'GET /cart': (_) => ok({'items': []})});
    await client(api, lang: 'ar').get<Map<String, dynamic>>('/cart');
    expect(api.calls.single.headers['Accept-Language'], 'ar');
    expect(api.calls.single.headers['Authorization'], 'Bearer old-access');
  });

  test('public calls do not send the token', () async {
    final api = FakeApi({'GET /categories': (_) => ok([])});
    await client(api).get<List<dynamic>>('/categories', auth: false);
    expect(api.calls.single.headers.containsKey('Authorization'), isFalse);
  });

  test('refreshes once on 401 and retries with the new token (concurrent requests share the refresh)', () async {
    var refreshes = 0;
    final api = FakeApi({
      'GET /cart': (c) => c.headers['Authorization'] == 'Bearer new-access' ? ok({'ok': true}) : (401, {'title': 'expired'}),
      'POST /auth/refresh': (c) {
        refreshes++;
        expect((c.body as Map)['refreshToken'], 'old-refresh');
        return ok({'accessToken': 'new-access', 'refreshToken': 'new-refresh', 'user': user});
      },
    });
    final c = client(api);
    final results = await Future.wait([c.get<Map<String, dynamic>>('/cart'), c.get<Map<String, dynamic>>('/cart')]);
    expect(results.every((r) => r['ok'] == true), isTrue);
    expect(refreshes, 1);
    expect((await tokens.read())!.refreshToken, 'new-refresh');
    expect(expired, 0);
  });

  test('clears tokens and reports session expiry when refresh is rejected', () async {
    final api = FakeApi({
      'GET /orders': (_) => (401, {'title': 'expired'}),
      'POST /auth/refresh': (_) => (401, {'title': 'invalid', 'code': 'invalid_refresh_token'}),
    });
    await expectLater(client(api).get<Map<String, dynamic>>('/orders'), throwsA(isA<ApiException>()));
    expect(await tokens.read(), isNull);
    expect(expired, 1);
  });

  test('parses problem+json validation errors', () async {
    final api = FakeApi({
      'POST /auth/register': (_) => (400, {
            'title': 'One or more validation errors occurred.',
            'code': 'validation_failed',
            'errors': {'email': ['Email is already registered']},
          }),
    });
    try {
      await client(api).post<Map<String, dynamic>>('/auth/register', data: {}, auth: false);
      fail('should throw');
    } on ApiException catch (e) {
      expect(e.statusCode, 400);
      expect(e.code, 'validation_failed');
      expect(e.fieldError('email'), 'Email is already registered');
      expect(e.displayMessage, 'Email is already registered');
    }
  });

  test('network failure is reported as such', () async {
    final dio = Dio(BaseOptions(baseUrl: 'http://127.0.0.1:1/api/v1', connectTimeout: const Duration(seconds: 2)));
    final c = ApiClient(tokens: tokens, language: () => 'en', dio: dio);
    try {
      await c.get<Object>('/meta', auth: false);
      fail('should throw');
    } on ApiException catch (e) {
      expect(e.isNetwork, isTrue);
    }
  });
}
