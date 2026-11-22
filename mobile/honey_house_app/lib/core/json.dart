/// Small helpers for hand-written fromJson (no code generation needed).
double toDouble(Object? v) => v == null ? 0 : (v as num).toDouble();
double? toDoubleOrNull(Object? v) => v == null ? null : (v as num).toDouble();

/// API timestamps are UTC; some arrive without an offset, so treat those as UTC too.
DateTime parseUtc(Object? v) {
  final s = v as String;
  final hasZone = s.endsWith('Z') || RegExp(r'[+-]\d\d:\d\d$').hasMatch(s);
  return DateTime.parse(hasZone ? s : '${s}Z');
}

DateTime? parseUtcOrNull(Object? v) => v == null ? null : parseUtc(v);

List<T> listOf<T>(Object? v, T Function(Map<String, dynamic>) f) =>
    (v as List? ?? const []).map((e) => f(e as Map<String, dynamic>)).toList();

class Paged<T> {
  Paged(this.items, this.page, this.totalPages, this.totalCount);
  final List<T> items;
  final int page;
  final int totalPages;
  final int totalCount;
  bool get hasNext => page < totalPages;

  factory Paged.fromJson(Map<String, dynamic> j, T Function(Map<String, dynamic>) f) =>
      Paged(listOf(j['items'], f), j['page'] as int, j['totalPages'] as int, j['totalCount'] as int);
}
