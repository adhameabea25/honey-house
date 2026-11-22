import 'package:intl/intl.dart';

/// Prices and dates use Latin digits in both languages (matches the dashboard and receipts).
class Fmt {
  static final _money = NumberFormat('#,##0.##', 'en');

  static String money(num value, String currency) => '${_money.format(value)} $currency';

  static String number(num value) => _money.format(value);

  /// API timestamps are UTC; shown in the device's local time (Cairo for customers in Egypt).
  static String dateTime(DateTime utc) => DateFormat('yyyy-MM-dd  HH:mm', 'en').format(utc.toLocal());

  static String date(DateTime utc) => DateFormat('yyyy-MM-dd', 'en').format(utc.toLocal());
}
