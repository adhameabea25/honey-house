import '../../core/json.dart';

class Governorate {
  Governorate(this.id, this.name, this.shippingFee);
  final int id;
  final String name;
  final double shippingFee;
  factory Governorate.fromJson(Map<String, dynamic> j) => Governorate(j['id'] as int, j['name'] as String? ?? '', toDouble(j['shippingFee']));
}

class Address {
  Address({required this.id, required this.governorateId, required this.governorateName, required this.shippingFee,
      required this.recipientName, required this.phone, required this.city, this.area, required this.street, this.building,
      this.floor, this.apartment, this.landmark, required this.isDefault});
  final int id;
  final int governorateId;
  final String governorateName;
  final double shippingFee;
  final String recipientName;
  final String phone;
  final String city;
  final String? area;
  final String street;
  final String? building;
  final String? floor;
  final String? apartment;
  final String? landmark;
  final bool isDefault;

  /// One-line address for lists and checkout.
  String get line => [street, if (building?.isNotEmpty ?? false) building, area, city, governorateName]
      .where((e) => e != null && e.isNotEmpty)
      .join('، ');

  factory Address.fromJson(Map<String, dynamic> j) => Address(
        id: j['id'] as int,
        governorateId: j['governorateId'] as int,
        governorateName: j['governorateName'] as String? ?? '',
        shippingFee: toDouble(j['shippingFee']),
        recipientName: j['recipientName'] as String? ?? '',
        phone: j['phone'] as String? ?? '',
        city: j['city'] as String? ?? '',
        area: j['area'] as String?,
        street: j['street'] as String? ?? '',
        building: j['building'] as String?,
        floor: j['floor'] as String?,
        apartment: j['apartment'] as String?,
        landmark: j['landmark'] as String?,
        isDefault: j['isDefault'] as bool? ?? false,
      );
}

class AddressInput {
  AddressInput({required this.governorateId, required this.recipientName, required this.phone, required this.city, this.area,
      required this.street, this.building, this.floor, this.apartment, this.landmark, this.isDefault = false});
  final int governorateId;
  final String recipientName;
  final String phone;
  final String city;
  final String? area;
  final String street;
  final String? building;
  final String? floor;
  final String? apartment;
  final String? landmark;
  final bool isDefault;

  Map<String, dynamic> toJson() => {
        'governorateId': governorateId, 'recipientName': recipientName, 'phone': phone, 'city': city, 'area': area,
        'street': street, 'building': building, 'floor': floor, 'apartment': apartment, 'landmark': landmark, 'isDefault': isDefault,
      };
}
