import 'package:flutter_test/flutter_test.dart';

import 'helpers.dart';

void main() {
  testWidgets('home shows categories and featured products in Arabic', (tester) async {
    final api = FakeApi(catalogRoutes());
    await pumpApp(tester, api);
    expect(find.text('بيت العسل'), findsWidgets);
    expect(find.text('شمع'), findsOneWidget);
    expect(find.text('عسل سدر'), findsOneWidget);
    expect(api.calls.first.headers['Accept-Language'], 'ar');
  });
}
