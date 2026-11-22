import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart' show Size;
import 'package:honey_house/app.dart';
import 'package:honey_house/core/api/api_client.dart';
import 'package:honey_house/core/providers.dart';
import 'package:honey_house/core/storage/token_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// A recorded request seen by [FakeApi].
class Call {
  Call(this.method, this.path, this.query, this.body, this.headers);
  final String method;
  final String path;
  final Map<String, dynamic> query;
  final Object? body;
  final Map<String, dynamic> headers;
}

typedef Handler = (int, Object?) Function(Call call);

/// In-memory stand-in for the API: routes "METHOD /path" to canned JSON responses.
class FakeApi implements HttpClientAdapter {
  FakeApi(this.routes);
  final Map<String, Handler> routes;
  final calls = <Call>[];

  @override
  Future<ResponseBody> fetch(RequestOptions o, Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    final path = o.uri.path.replaceFirst('/api/v1', '');
    Object? body = o.data;
    final call = Call(o.method, path, o.queryParameters, body, o.headers);
    calls.add(call);
    final handler = routes['${o.method} $path'];
    final (status, json) = handler == null ? (404, {'title': 'Not found: ${o.method} $path'}) : handler(call);
    return ResponseBody.fromString(json == null ? '' : jsonEncode(json), status, headers: {
      Headers.contentTypeHeader: [status >= 400 ? 'application/problem+json' : 'application/json'],
    });
  }

  @override
  void close({bool force = false}) {}
}

(int, Object?) ok(Object? json) => (200, json);

Map<String, dynamic> product(int id, String name, {double price = 100, bool featured = false}) => {
      'id': id, 'name': name, 'slug': 'p$id', 'category': {'id': 1, 'name': 'عسل', 'slug': 'honey'}, 'mainImageUrl': null,
      'minPrice': price, 'maxPrice': price * 2, 'compareAtPrice': null, 'currency': 'EGP', 'averageRating': 4.5,
      'reviewCount': 2, 'isFeatured': featured, 'inStock': true,
    };

Map<String, dynamic> paged(List<Object> items) =>
    {'items': items, 'page': 1, 'pageSize': 20, 'totalCount': items.length, 'totalPages': 1, 'hasNext': false, 'hasPrevious': false};

final user = {'id': 'u1', 'fullName': 'Adham Rabea', 'email': 'a@b.com', 'phoneNumber': '01012345678', 'preferredLanguage': 'ar'};

/// Routes shared by most tests (public catalog).
Map<String, Handler> catalogRoutes() => {
      'GET /categories': (_) => ok([
            {'id': 1, 'name': 'عسل', 'slug': 'honey', 'subCategories': []},
            {'id': 2, 'name': 'شمع', 'slug': 'wax', 'subCategories': []},
          ]),
      'GET /products/featured': (_) => ok([product(1, 'عسل سدر', featured: true), product(2, 'عسل برسيم', featured: true)]),
      'GET /products': (_) => ok(paged([product(1, 'عسل سدر'), product(2, 'عسل برسيم')])),
      'GET /products/1': (_) => ok({
            'id': 1, 'name': 'عسل سدر', 'description': 'عسل طبيعي', 'origin': 'سيناء',
            'category': {'id': 1, 'name': 'عسل'}, 'images': [], 'currency': 'EGP', 'averageRating': 4.5, 'reviewCount': 2,
            'variants': [
              {'id': 11, 'weightGrams': 250, 'label': '250 جم', 'price': 100, 'compareAtPrice': 120, 'inStock': true, 'availableQuantity': 3},
              {'id': 12, 'weightGrams': 500, 'label': '500 جم', 'price': 180, 'inStock': true, 'availableQuantity': 50},
            ],
          }),
      'GET /products/1/reviews': (_) => ok({
            'summary': {'average': 4.5, 'count': 2, 'distribution': {'5': 1, '4': 1}},
            'reviews': paged([
              {'id': 1, 'rating': 5, 'comment': 'ممتاز', 'reviewerName': 'منى', 'isVerifiedPurchase': true, 'createdAt': '2026-11-01T10:00:00'},
            ]),
          }),
    };

/// Builds the whole app on top of [api], optionally signed in.
Future<ProviderContainer> pumpApp(WidgetTester tester, FakeApi api, {bool signedIn = false, String language = 'ar'}) async {
  SharedPreferences.setMockInitialValues({'hh_language': language});
  final prefs = await SharedPreferences.getInstance();
  final tokens = MemoryTokenStorage();
  if (signedIn) await tokens.write(StoredTokens('access', 'refresh'));
  tester.view.physicalSize = const Size(1170, 2532);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
  final container = ProviderContainer(
    overrides: [
      sharedPreferencesProvider.overrideWithValue(prefs),
      tokenStorageProvider.overrideWithValue(tokens),
      apiClientProvider.overrideWith((ref) {
        final dio = Dio(BaseOptions(baseUrl: 'http://test/api/v1'))..httpClientAdapter = api;
        return ApiClient(
          tokens: tokens,
          dio: dio,
          language: () => ref.read(localeProvider).languageCode,
          onSessionExpired: () => ref.read(sessionExpiredProvider).add(null),
        );
      }),
    ],
    retry: (_, _) => null,
  );
  addTearDown(container.dispose);
  await tester.pumpWidget(UncontrolledProviderScope(container: container, child: const HoneyHouseApp()));
  await settle(tester);
  return container;
}

/// Lets the fake HTTP responses finish (Dio completes them outside the test's fake clock), then settles the UI.
Future<void> settle(WidgetTester tester) async {
  for (var i = 0; i < 4; i++) {
    await tester.runAsync(() => Future<void>.delayed(const Duration(milliseconds: 20)));
    await tester.pumpAndSettle();
  }
}
