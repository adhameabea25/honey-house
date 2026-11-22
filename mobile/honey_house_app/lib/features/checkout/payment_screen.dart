import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:webview_flutter/webview_flutter.dart';

import '../../core/l10n.dart';
import '../../core/widgets/common.dart';
import '../orders/order_providers.dart';

/// Paymob checkout page. Paymob sends the customer to the redirection URL with `success=true|false`;
/// the order status itself is updated by the server webhook, so the app reloads the order afterwards.
class PaymentScreen extends ConsumerStatefulWidget {
  const PaymentScreen({super.key, required this.orderId, required this.url});
  final int orderId;
  final String url;

  @override
  ConsumerState<PaymentScreen> createState() => _PaymentScreenState();
}

class _PaymentScreenState extends ConsumerState<PaymentScreen> {
  WebViewController? _web;
  bool _loading = true;
  bool _finished = false;

  @override
  void initState() {
    super.initState();
    if (kIsWeb) {
      // The web build opens Paymob in a new tab.
      launchUrl(Uri.parse(widget.url), webOnlyWindowName: '_blank');
      _loading = false;
    } else {
      _web = WebViewController()
        ..setJavaScriptMode(JavaScriptMode.unrestricted)
        ..setNavigationDelegate(NavigationDelegate(
          onPageFinished: (_) => mounted ? setState(() => _loading = false) : null,
          onNavigationRequest: (r) {
            final success = Uri.tryParse(r.url)?.queryParameters['success'];
            if (success != null) {
              _finish(success == 'true');
              return NavigationDecision.prevent;
            }
            return NavigationDecision.navigate;
          },
        ))
        ..loadRequest(Uri.parse(widget.url));
    }
  }

  void _finish(bool? success) {
    if (_finished) return;
    _finished = true;
    final l = context.l10n;
    ref.invalidate(orderProvider(widget.orderId));
    ref.invalidate(myOrdersProvider);
    if (success != null) showMessage(context, success ? l.paymentSuccess : l.paymentFailed, error: !success);
    context.pop();
  }

  @override
  Widget build(BuildContext context) {
    final l = context.l10n;
    return Scaffold(
      appBar: AppBar(title: Text(l.paymentTitle)),
      body: Column(children: [
        if (_loading) const LinearProgressIndicator(),
        Expanded(
          child: _web != null
              ? WebViewWidget(controller: _web!)
              : Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(mainAxisSize: MainAxisSize.min, children: [
                      const Icon(Icons.open_in_new, size: 48),
                      const SizedBox(height: 12),
                      Text(l.paymentPendingNote, textAlign: TextAlign.center),
                      TextButton(
                        onPressed: () => launchUrl(Uri.parse(widget.url), webOnlyWindowName: '_blank'),
                        child: Text(l.payNow),
                      ),
                    ]),
                  ),
                ),
        ),
        SafeArea(
          top: false,
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: SizedBox(
              width: double.infinity,
              child: OutlinedButton(onPressed: () => _finish(null), child: Text(l.paymentDone)),
            ),
          ),
        ),
      ]),
    );
  }
}
