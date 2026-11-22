import '../../core/json.dart';

class CartItem {
  CartItem({required this.id, required this.variantId, required this.productId, required this.productName, required this.variantLabel,
      this.imageUrl, required this.unitPrice, this.compareAtPrice, required this.quantity, required this.lineTotal,
      required this.isAvailable, required this.availableQuantity});
  final int id;
  final int variantId;
  final int productId;
  final String productName;
  final String variantLabel;
  final String? imageUrl;
  final double unitPrice;
  final double? compareAtPrice;
  final int quantity;
  final double lineTotal;
  final bool isAvailable;
  final int availableQuantity;

  factory CartItem.fromJson(Map<String, dynamic> j) => CartItem(
        id: j['id'] as int,
        variantId: j['variantId'] as int,
        productId: j['productId'] as int,
        productName: j['productName'] as String? ?? '',
        variantLabel: j['variantLabel'] as String? ?? '',
        imageUrl: j['imageUrl'] as String?,
        unitPrice: toDouble(j['unitPrice']),
        compareAtPrice: toDoubleOrNull(j['compareAtPrice']),
        quantity: j['quantity'] as int,
        lineTotal: toDouble(j['lineTotal']),
        isAvailable: j['isAvailable'] as bool? ?? true,
        availableQuantity: j['availableQuantity'] as int? ?? 0,
      );
}

class Cart {
  Cart({required this.items, required this.itemCount, required this.subtotal, required this.currency, required this.hasUnavailableItems});
  final List<CartItem> items;
  final int itemCount;
  final double subtotal;
  final String currency;
  final bool hasUnavailableItems;

  static final empty = Cart(items: const [], itemCount: 0, subtotal: 0, currency: 'EGP', hasUnavailableItems: false);

  factory Cart.fromJson(Map<String, dynamic> j) => Cart(
        items: listOf(j['items'], CartItem.fromJson),
        itemCount: j['itemCount'] as int? ?? 0,
        subtotal: toDouble(j['subtotal']),
        currency: j['currency'] as String? ?? 'EGP',
        hasUnavailableItems: j['hasUnavailableItems'] as bool? ?? false,
      );
}
