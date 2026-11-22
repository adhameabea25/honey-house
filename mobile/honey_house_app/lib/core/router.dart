import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../features/addresses/address_form_screen.dart';
import '../features/addresses/addresses_screen.dart';
import '../features/auth/auth_controller.dart';
import '../features/auth/forgot_password_screen.dart';
import '../features/auth/login_screen.dart';
import '../features/auth/register_screen.dart';
import '../features/cart/cart_screen.dart';
import '../features/catalog/catalog_models.dart';
import '../features/catalog/home_screen.dart';
import '../features/catalog/product_screen.dart';
import '../features/catalog/shop_screen.dart';
import '../features/checkout/checkout_screen.dart';
import '../features/checkout/order_placed_screen.dart';
import '../features/checkout/payment_screen.dart';
import '../features/favorites/favorites_screen.dart';
import '../features/orders/order_screen.dart';
import '../features/orders/orders_screen.dart';
import '../features/settings/account_screen.dart';
import '../features/settings/change_password_screen.dart';
import '../features/settings/profile_screen.dart';
import '../features/shell/app_shell.dart';

final scaffoldMessengerKey = GlobalKey<ScaffoldMessengerState>();
final rootNavigatorKey = GlobalKey<NavigatorState>();

/// Screens that need a signed-in customer. Guests are sent to sign in and come back afterwards.
const _protected = ['/checkout', '/payment', '/order-placed', '/orders', '/addresses', '/profile', '/change-password'];
const _guestOnly = ['/login', '/register', '/forgot-password'];

final routerProvider = Provider<GoRouter>((ref) {
  // Re-run redirects whenever the user signs in or out.
  final authChanges = ValueNotifier<int>(0);
  ref.listen(authProvider, (_, _) => authChanges.value++);
  ref.onDispose(authChanges.dispose);

  return GoRouter(
    navigatorKey: rootNavigatorKey,
    initialLocation: '/',
    refreshListenable: authChanges,
    redirect: (context, state) {
      final auth = ref.read(authProvider);
      if (auth.isLoading && !auth.hasValue) return null;
      final signedIn = auth.value != null;
      final path = state.matchedLocation;
      if (!signedIn && _protected.any(path.startsWith)) {
        return Uri(path: '/login', queryParameters: {'from': state.uri.toString()}).toString();
      }
      if (signedIn && _guestOnly.contains(path)) {
        return state.uri.queryParameters['from'] ?? '/';
      }
      return null;
    },
    routes: [
      StatefulShellRoute.indexedStack(
        builder: (context, state, shell) => AppShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [GoRoute(path: '/', builder: (_, _) => const HomeScreen())]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/shop',
              builder: (_, s) => ShopScreen(
                key: ValueKey(s.uri.query),
                initial: ProductFilter(
                  categoryId: int.tryParse(s.uri.queryParameters['category'] ?? ''),
                  search: s.uri.queryParameters['q'],
                ),
              ),
            ),
          ]),
          StatefulShellBranch(routes: [GoRoute(path: '/cart', builder: (_, _) => const CartScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/favorites', builder: (_, _) => const FavoritesScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: '/account', builder: (_, _) => const AccountScreen())]),
        ],
      ),
      GoRoute(path: '/product/:id', builder: (_, s) => ProductScreen(productId: int.parse(s.pathParameters['id']!))),
      GoRoute(path: '/login', builder: (_, s) => LoginScreen(from: s.uri.queryParameters['from'])),
      GoRoute(path: '/register', builder: (_, s) => RegisterScreen(from: s.uri.queryParameters['from'])),
      GoRoute(path: '/forgot-password', builder: (_, _) => const ForgotPasswordScreen()),
      GoRoute(path: '/profile', builder: (_, _) => const ProfileScreen()),
      GoRoute(path: '/change-password', builder: (_, _) => const ChangePasswordScreen()),
      GoRoute(path: '/addresses', builder: (_, s) => AddressesScreen(selecting: s.uri.queryParameters['select'] == '1')),
      GoRoute(path: '/addresses/new', builder: (_, _) => const AddressFormScreen()),
      GoRoute(path: '/addresses/:id', builder: (_, s) => AddressFormScreen(addressId: int.parse(s.pathParameters['id']!))),
      GoRoute(path: '/checkout', builder: (_, _) => const CheckoutScreen()),
      GoRoute(
        path: '/payment/:orderId',
        builder: (_, s) => PaymentScreen(orderId: int.parse(s.pathParameters['orderId']!), url: s.uri.queryParameters['url']!),
      ),
      GoRoute(path: '/order-placed/:id', builder: (_, s) => OrderPlacedScreen(orderId: int.parse(s.pathParameters['id']!))),
      GoRoute(path: '/orders', builder: (_, _) => const OrdersScreen()),
      GoRoute(path: '/orders/:id', builder: (_, s) => OrderScreen(orderId: int.parse(s.pathParameters['id']!))),
    ],
  );
});
