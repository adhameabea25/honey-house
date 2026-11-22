import 'package:material_ui/material_ui.dart';

import 'order_models.dart';

/// Colored status label (the text itself comes translated from the API).
class StatusChip extends StatelessWidget {
  const StatusChip(this.status, this.label, {super.key});
  final OrderStatus status;
  final String label;

  static Color colorOf(OrderStatus s) => switch (s) {
        OrderStatus.pending => Colors.orange.shade800,
        OrderStatus.confirmed || OrderStatus.processing => Colors.blue.shade700,
        OrderStatus.shipped || OrderStatus.outForDelivery => Colors.indigo.shade600,
        OrderStatus.delivered => Colors.green.shade700,
        OrderStatus.cancelled || OrderStatus.returned => Colors.red.shade700,
      };

  @override
  Widget build(BuildContext context) {
    final c = colorOf(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: c.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(20)),
      child: Text(label, style: TextStyle(color: c, fontWeight: FontWeight.w700, fontSize: 12.5)),
    );
  }
}
