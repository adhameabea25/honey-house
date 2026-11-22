import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/theme.dart';
import '../../core/widgets/forms.dart';
import '../auth/auth_controller.dart';
import 'language_tile.dart';

class AccountScreen extends ConsumerWidget {
  const AccountScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l = context.l10n;
    final user = ref.watch(authProvider).value;
    return Scaffold(
      appBar: AppBar(title: Text(l.navAccount)),
      body: ListView(children: [
        Container(
          margin: const EdgeInsets.all(16),
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(color: HoneyColors.cream, borderRadius: BorderRadius.circular(16)),
          child: Row(children: [
            CircleAvatar(
              radius: 28,
              backgroundColor: HoneyColors.honey,
              child: Text(user == null ? '🐝' : user.fullName.characters.first,
                  style: const TextStyle(fontSize: 24, color: Colors.white, fontWeight: FontWeight.bold)),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(user == null ? l.guest : l.welcomeUser(user.firstName),
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700)),
                if (user != null) Text(user.email, textDirection: TextDirection.ltr),
              ]),
            ),
          ]),
        ),
        if (user == null)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(children: [
              Expanded(child: FilledButton(onPressed: () => goToLogin(context), child: Text(l.login))),
              const SizedBox(width: 12),
              Expanded(child: OutlinedButton(onPressed: () => context.push('/register'), child: Text(l.register))),
            ]),
          )
        else ...[
          _Tile(icon: Icons.receipt_long_outlined, title: l.myOrders, onTap: () => context.push('/orders')),
          _Tile(icon: Icons.location_on_outlined, title: l.myAddresses, onTap: () => context.push('/addresses')),
          _Tile(icon: Icons.badge_outlined, title: l.myProfile, onTap: () => context.push('/profile')),
          _Tile(icon: Icons.password, title: l.changePassword, onTap: () => context.push('/change-password')),
        ],
        const Divider(height: 32),
        const LanguageTile(),
        if (user != null)
          ListTile(
            leading: Icon(Icons.logout, color: Colors.red.shade700),
            title: Text(l.logout, style: TextStyle(color: Colors.red.shade700)),
            onTap: () async {
              if (await confirmDialog(context, l.confirmLogout, confirmLabel: l.logout, destructive: true)) {
                await ref.read(authProvider.notifier).logout();
              }
            },
          ),
        const SizedBox(height: 24),
      ]),
    );
  }
}

class _Tile extends StatelessWidget {
  const _Tile({required this.icon, required this.title, required this.onTap});
  final IconData icon;
  final String title;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => ListTile(
        leading: Icon(icon),
        title: Text(title),
        trailing: const Icon(Icons.arrow_forward_ios, size: 16),
        onTap: onTap,
      );
}
