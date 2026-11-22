class AppUser {
  AppUser({required this.id, required this.fullName, required this.email, this.phoneNumber, required this.preferredLanguage});
  final String id;
  final String fullName;
  final String email;
  final String? phoneNumber;
  final String preferredLanguage;

  String get firstName => fullName.trim().split(RegExp(r'\s+')).first;

  factory AppUser.fromJson(Map<String, dynamic> j) => AppUser(
        id: j['id'] as String,
        fullName: j['fullName'] as String? ?? '',
        email: j['email'] as String? ?? '',
        phoneNumber: j['phoneNumber'] as String?,
        preferredLanguage: j['preferredLanguage'] as String? ?? 'ar',
      );
}
