import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/providers.dart';
import '../auth/auth_controller.dart';

/// Switches between Arabic and English (and saves the choice on the account for emails).
class LanguageTile extends ConsumerWidget {
  const LanguageTile({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final code = ref.watch(localeProvider).languageCode;
    return ListTile(
      leading: const Icon(Icons.language),
      title: Text(l.language),
      trailing: SegmentedButton<String>(
        showSelectedIcon: false,
        segments: [
          ButtonSegment(value: 'ar', label: Text(l.arabic)),
          ButtonSegment(value: 'en', label: Text(l.english)),
        ],
        selected: {code},
        onSelectionChanged: (s) async {
          await ref.read(localeProvider.notifier).set(s.first);
          await ref.read(authProvider.notifier).syncLanguage(s.first);
        },
      ),
    );
  }
}
