import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../core/l10n.dart';
import '../../core/theme.dart';
import '../../core/widgets/common.dart';
import '../../core/widgets/forms.dart';
import 'catalog_models.dart';
import 'catalog_providers.dart';

/// Write or edit the customer's review of a product. Reviews are published after admin approval.
Future<void> showReviewSheet(BuildContext context, {required int productId, MyReview? existing}) =>
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _ReviewSheet(productId: productId, existing: existing),
    );

class _ReviewSheet extends ConsumerStatefulWidget {
  const _ReviewSheet({required this.productId, this.existing});
  final int productId;
  final MyReview? existing;

  @override
  ConsumerState<_ReviewSheet> createState() => _ReviewSheetState();
}

class _ReviewSheetState extends ConsumerState<_ReviewSheet> {
  late int _rating = widget.existing?.rating ?? 0;
  late final _comment = TextEditingController(text: widget.existing?.comment);
  bool _busy = false;

  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final l = context.l10n;
    if (_rating == 0) return showMessage(context, l.chooseRating, error: true);
    setState(() => _busy = true);
    try {
      await ref.read(reviewRepositoryProvider).submit(widget.productId, existing: widget.existing, rating: _rating, comment: _comment.text);
      if (!mounted) return;
      Navigator.pop(context);
      showMessage(context, l.reviewSubmitted);
    } catch (e) {
      if (mounted) showError(context, e);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    return Padding(
      padding: EdgeInsets.fromLTRB(20, 0, 20, 20 + MediaQuery.viewInsetsOf(context).bottom),
      child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(widget.existing == null ? l.writeReview : l.editReview, style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 12),
        Text(l.yourRating),
        Row(mainAxisAlignment: MainAxisAlignment.center, children: [
          for (var i = 1; i <= 5; i++)
            IconButton(
              iconSize: 36,
              onPressed: () => setState(() => _rating = i),
              icon: Icon(i <= _rating ? Icons.star : Icons.star_border, color: HoneyColors.honey),
            ),
        ]),
        TextField(controller: _comment, maxLines: 4, maxLength: 1000, decoration: InputDecoration(labelText: l.yourComment)),
        const SizedBox(height: 8),
        BusyButton(label: l.save, busy: _busy, onPressed: _submit),
      ]),
    );
  }
}
