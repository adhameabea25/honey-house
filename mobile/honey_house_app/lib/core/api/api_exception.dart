import 'package:dio/dio.dart';

/// Error returned by the API as `application/problem+json` (already localized by the server),
/// or a network failure.
class ApiException implements Exception {
  ApiException({this.statusCode, this.code, this.message, this.fieldErrors = const {}, this.isNetwork = false});

  final int? statusCode;

  /// Stable error code from the API (e.g. `invalid_credentials`, `insufficient_stock`).
  final String? code;

  /// Human-readable message in the request language.
  final String? message;

  /// Validation errors per field (camelCase field names).
  final Map<String, List<String>> fieldErrors;
  final bool isNetwork;

  bool get isUnauthorized => statusCode == 401;

  String? fieldError(String field) => fieldErrors[field]?.firstOrNull;

  /// The most useful single message for a snack bar.
  String? get displayMessage {
    if (fieldErrors.isNotEmpty) return fieldErrors.values.expand((e) => e).firstOrNull ?? message;
    return message;
  }

  factory ApiException.fromDio(DioException e) {
    final response = e.response;
    if (response == null) return ApiException(isNetwork: true);
    final data = response.data;
    if (data is Map) {
      final errors = <String, List<String>>{};
      final raw = data['errors'];
      if (raw is Map) {
        raw.forEach((k, v) => errors[k.toString()] = (v as List).map((x) => x.toString()).toList());
      }
      return ApiException(
        statusCode: response.statusCode,
        code: data['code']?.toString(),
        message: data['title']?.toString() ?? data['detail']?.toString(),
        fieldErrors: errors,
      );
    }
    return ApiException(statusCode: response.statusCode);
  }

  @override
  String toString() => 'ApiException($statusCode, $code, $message)';
}
