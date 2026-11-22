import 'dart:async';

import 'package:cupertino_ui/cupertino_ui.dart' show GlobalCupertinoLocalizations;
import 'package:flutter_localizations/flutter_localizations.dart' show GlobalWidgetsLocalizations;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import 'core/format.dart';
import 'core/l10n.dart';
import 'core/providers.dart';
import 'core/router.dart';
import 'core/theme.dart';

class HoneyHouseApp extends ConsumerWidget {
  const HoneyHouseApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = ref.watch(localeProvider);
    Fmt.language = locale.languageCode;
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      onGenerateTitle: (c) => c.l10n.appTitle,
      theme: buildTheme(),
      locale: locale,
      supportedLocales: AppLocalizations.supportedLocales,
      // Material/Cupertino localizations now live in material_ui / cupertino_ui
      // (the generated AppLocalizations.localizationsDelegates still points at the old ones).
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ],
      scaffoldMessengerKey: scaffoldMessengerKey,
      routerConfig: ref.watch(routerProvider),
      builder: (context, child) => _SessionWatcher(child: child ?? const SizedBox()),
    );
  }
}

/// Tells the user when the server ended their session (refresh token rejected).
class _SessionWatcher extends ConsumerStatefulWidget {
  const _SessionWatcher({required this.child});
  final Widget child;

  @override
  ConsumerState<_SessionWatcher> createState() => _SessionWatcherState();
}

class _SessionWatcherState extends ConsumerState<_SessionWatcher> {
  StreamSubscription<void>? _sub;

  @override
  void initState() {
    super.initState();
    _sub = ref.read(sessionExpiredProvider).stream.listen((_) {
      if (!mounted) return;
      final l = AppLocalizations.of(context);
      scaffoldMessengerKey.currentState?.showSnackBar(SnackBar(content: Text(l.sessionExpired)));
    });
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
