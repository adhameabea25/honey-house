import '../../core/json.dart';

enum OrderStatus { pending, confirmed, processing, shipped, outForDelivery, delivered, cancelled, returned }

enum PaymentMethod { cashOnDelivery, online }

enum PaymentStatus { pending, paid, failed, refunded }

T _enum<T extends Enum>(List<T> values, Object? v) {
  final s = (v as String).toLowerCase();
  return values.firstWhere((e) => e.name.toLowerCase() == s);
}

String apiPaymentMethod(PaymentMethod m) => m == PaymentMethod.cashOnDelivery ? 'CashOnDelivery' : 'Online';

class CheckoutLine {
  CheckoutLine(this.productName, this.variantLabel, this.unitPrice, this.quantity, this.lineTotal);
  final String productName;
  final String variantLabel;
  final double unitPrice;
  final int quantity;
  final double lineTotal;
  factory CheckoutLine.fromJson(Map<String, dynamic> j) => CheckoutLine(j['productName'] as String? ?? '',
      j['variantLabel'] as String? ?? '', toDouble(j['unitPrice']), j['quantity'] as int, toDouble(j['lineTotal']));
}

class CheckoutSummary {
  CheckoutSummary({required this.items, required this.subtotal, required this.discountAmount, required this.shippingFee,
      required this.total, required this.currency, this.couponCode, this.couponError, required this.canPlaceOrder, this.blockingReason});
  final List<CheckoutLine> items;
  final double subtotal;
  final double discountAmount;
  final double shippingFee;
  final double total;
  final String currency;
  final String? couponCode;
  final String? couponError;
  final bool canPlaceOrder;
  final String? blockingReason;

  factory CheckoutSummary.fromJson(Map<String, dynamic> j) => CheckoutSummary(
        items: listOf(j['items'], CheckoutLine.fromJson),
        subtotal: toDouble(j['subtotal']),
        discountAmount: toDouble(j['discountAmount']),
        shippingFee: toDouble(j['shippingFee']),
        total: toDouble(j['total']),
        currency: j['currency'] as String? ?? 'EGP',
        couponCode: j['couponCode'] as String?,
        couponError: j['couponError'] as String?,
        canPlaceOrder: j['canPlaceOrder'] as bool? ?? false,
        blockingReason: j['blockingReason'] as String?,
      );
}

class OrderSummary {
  OrderSummary({required this.id, required this.orderNumber, required this.status, required this.statusName,
      required this.paymentMethod, required this.paymentStatus, required this.paymentStatusName, required this.total,
      required this.currency, required this.itemCount, required this.createdAt});
  final int id;
  final String orderNumber;
  final OrderStatus status;
  final String statusName;
  final PaymentMethod paymentMethod;
  final PaymentStatus paymentStatus;
  final String paymentStatusName;
  final double total;
  final String currency;
  final int itemCount;
  final DateTime createdAt;

  factory OrderSummary.fromJson(Map<String, dynamic> j) => OrderSummary(
        id: j['id'] as int,
        orderNumber: j['orderNumber'] as String,
        status: _enum(OrderStatus.values, j['status']),
        statusName: j['statusName'] as String? ?? '',
        paymentMethod: _enum(PaymentMethod.values, j['paymentMethod']),
        paymentStatus: _enum(PaymentStatus.values, j['paymentStatus']),
        paymentStatusName: j['paymentStatusName'] as String? ?? '',
        total: toDouble(j['total']),
        currency: j['currency'] as String? ?? 'EGP',
        itemCount: j['itemCount'] as int? ?? 0,
        createdAt: parseUtc(j['createdAt']),
      );
}

class OrderItem {
  OrderItem(this.productId, this.productName, this.variantLabel, this.unitPrice, this.quantity, this.lineTotal);
  final int productId;
  final String productName;
  final String variantLabel;
  final double unitPrice;
  final int quantity;
  final double lineTotal;
  factory OrderItem.fromJson(Map<String, dynamic> j) => OrderItem(j['productId'] as int, j['productName'] as String? ?? '',
      j['variantLabel'] as String? ?? '', toDouble(j['unitPrice']), j['quantity'] as int, toDouble(j['lineTotal']));
}

class StatusEntry {
  StatusEntry(this.status, this.statusName, this.note, this.changedAt);
  final OrderStatus status;
  final String statusName;
  final String? note;
  final DateTime changedAt;
  factory StatusEntry.fromJson(Map<String, dynamic> j) =>
      StatusEntry(_enum(OrderStatus.values, j['status']), j['statusName'] as String? ?? '', j['note'] as String?, parseUtc(j['changedAt']));
}

class ShippingAddress {
  ShippingAddress(this.recipientName, this.phone, this.governorate, this.city, this.addressLine, this.landmark);
  final String recipientName;
  final String phone;
  final String governorate;
  final String city;
  final String addressLine;
  final String? landmark;
  factory ShippingAddress.fromJson(Map<String, dynamic> j) => ShippingAddress(j['recipientName'] as String? ?? '',
      j['phone'] as String? ?? '', j['governorate'] as String? ?? '', j['city'] as String? ?? '', j['addressLine'] as String? ?? '',
      j['landmark'] as String?);
}

class OrderDetail {
  OrderDetail({required this.summary, required this.subtotal, required this.discountAmount, required this.shippingFee,
      this.couponCode, required this.shippingAddress, this.notes, this.trackingNumber, required this.items, required this.history,
      required this.canCancel});
  final OrderSummary summary;
  final double subtotal;
  final double discountAmount;
  final double shippingFee;
  final String? couponCode;
  final ShippingAddress shippingAddress;
  final String? notes;
  final String? trackingNumber;
  final List<OrderItem> items;
  final List<StatusEntry> history;
  final bool canCancel;

  int get id => summary.id;

  factory OrderDetail.fromJson(Map<String, dynamic> j) {
    final items = listOf(j['items'], OrderItem.fromJson);
    return OrderDetail(
      summary: OrderSummary.fromJson({...j, 'itemCount': items.fold<int>(0, (s, i) => s + i.quantity)}),
      subtotal: toDouble(j['subtotal']),
      discountAmount: toDouble(j['discountAmount']),
      shippingFee: toDouble(j['shippingFee']),
      couponCode: j['couponCode'] as String?,
      shippingAddress: ShippingAddress.fromJson(j['shippingAddress'] as Map<String, dynamic>),
      notes: j['notes'] as String?,
      trackingNumber: j['trackingNumber'] as String?,
      items: items,
      history: listOf(j['history'], StatusEntry.fromJson),
      canCancel: j['canCancel'] as bool? ?? false,
    );
  }
}

class PlacedOrder {
  PlacedOrder(this.order, this.paymentUrl);
  final OrderDetail order;
  final String? paymentUrl;
  factory PlacedOrder.fromJson(Map<String, dynamic> j) =>
      PlacedOrder(OrderDetail.fromJson(j['order'] as Map<String, dynamic>), j['paymentUrl'] as String?);
}
