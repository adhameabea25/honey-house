import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../api/api_exception.dart';
import '../config.dart';
import '../l10n.dart';
import '../theme.dart';

/// Localized message for any error (API errors are already translated by the server).
String errorText(BuildContext context, Object error) {
  if (error is ApiException) {
    if (error.isNetwork) return context.l10n.errorNetwork;
    return error.displayMessage ?? context.l10n.errorGeneric;
  }
  return context.l10n.errorGeneric;
}

void showMessage(BuildContext context, String text, {bool error = false, SnackBarAction? action}) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(
      content: Text(text),
      backgroundColor: error ? Colors.red.shade700 : null,
      behavior: SnackBarBehavior.floating,
      action: action,
      // Messages with an action (e.g. "View cart") still close on their own.
      persist: false,
    ));
}

void showError(BuildContext context, Object error) => showMessage(context, errorText(context, error), error: true);

class ErrorView extends StatelessWidget {
  const ErrorView({super.key, required this.error, this.onRetry});
  final Object error;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            Icon(Icons.cloud_off_outlined, size: 48, color: Colors.grey.shade500),
            const SizedBox(height: 12),
            Text(errorText(context, error), textAlign: TextAlign.center),
            if (onRetry != null) ...[
              const SizedBox(height: 12),
              TextButton.icon(onPressed: onRetry, icon: const Icon(Icons.refresh), label: Text(context.l10n.retry)),
            ],
          ]),
        ),
      );
}

class EmptyView extends StatelessWidget {
  const EmptyView({super.key, required this.icon, required this.text, this.action});
  final IconData icon;
  final String text;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            Icon(icon, size: 64, color: HoneyColors.honey),
            const SizedBox(height: 16),
            Text(text, style: Theme.of(context).textTheme.titleMedium, textAlign: TextAlign.center),
            if (action != null) ...[const SizedBox(height: 20), action!],
          ]),
        ),
      );
}

/// Renders an AsyncValue with standard loading and error states.
class AsyncBody<T> extends StatelessWidget {
  const AsyncBody({super.key, required this.value, required this.builder, this.onRetry});
  final AsyncValue<T> value;
  final Widget Function(T data) builder;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) => switch (value) {
        AsyncValue(:final value?, hasValue: true) => builder(value),
        AsyncError(:final error) => ErrorView(error: error, onRetry: onRetry),
        _ => const Center(child: CircularProgressIndicator()),
      };
}

class NetImage extends StatelessWidget {
  const NetImage(this.path, {super.key, this.fit = BoxFit.cover, this.radius = 12});
  final String? path;
  final BoxFit fit;
  final double radius;

  @override
  Widget build(BuildContext context) {
    final url = AppConfig.imageUrl(path);
    final placeholder = Container(
      color: HoneyColors.cream,
      alignment: Alignment.center,
      child: const Text('🍯', style: TextStyle(fontSize: 36)),
    );
    return ClipRRect(
      borderRadius: BorderRadius.circular(radius),
      child: url == null
          ? placeholder
          : CachedNetworkImage(imageUrl: url, fit: fit, placeholder: (_, _) => placeholder, errorWidget: (_, _, _) => placeholder),
    );
  }
}

class Stars extends StatelessWidget {
  const Stars(this.rating, {super.key, this.size = 16});
  final double rating;
  final double size;

  @override
  Widget build(BuildContext context) => Row(
        mainAxisSize: MainAxisSize.min,
        children: List.generate(5, (i) {
          final icon = rating >= i + 1 ? Icons.star : (rating > i ? Icons.star_half : Icons.star_border);
          return Icon(icon, size: size, color: HoneyColors.honey);
        }),
      );
}

class SectionTitle extends StatelessWidget {
  const SectionTitle(this.text, {super.key, this.action});
  final String text;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(16, 20, 16, 8),
        child: Row(children: [
          Expanded(child: Text(text, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700))),
          ?action,
        ]),
      );
}

/// A row of label/value used in totals.
class AmountRow extends StatelessWidget {
  const AmountRow(this.label, this.value, {super.key, this.bold = false, this.color});
  final String label;
  final String value;
  final bool bold;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final style = TextStyle(fontWeight: bold ? FontWeight.w800 : FontWeight.w500, fontSize: bold ? 17 : 15, color: color);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(children: [Expanded(child: Text(label, style: style)), Text(value, style: style)]),
    );
  }
}
