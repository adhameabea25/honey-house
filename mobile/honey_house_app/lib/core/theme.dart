import 'package:material_ui/material_ui.dart';

class HoneyColors {
  static const honey = Color(0xFFE8A317);
  static const honeyDark = Color(0xFF5B3A06);
  static const cream = Color(0xFFFFF7E6);
  static const background = Color(0xFFFAF8F4);
}

ThemeData buildTheme() {
  final scheme = ColorScheme.fromSeed(
    seedColor: HoneyColors.honey,
    primary: HoneyColors.honeyDark,
    secondary: HoneyColors.honey,
    surface: Colors.white,
  );
  return ThemeData(
    colorScheme: scheme,
    scaffoldBackgroundColor: HoneyColors.background,
    appBarTheme: const AppBarTheme(backgroundColor: HoneyColors.background, foregroundColor: HoneyColors.honeyDark, centerTitle: true,
        elevation: 0, scrolledUnderElevation: 1),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: HoneyColors.honey,
        foregroundColor: const Color(0xFF2B1B00),
        minimumSize: const Size.fromHeight(50),
        textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size.fromHeight(48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
    ),
    cardTheme: CardThemeData(
      color: Colors.white,
      elevation: 0,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14), side: BorderSide(color: Colors.grey.shade200)),
      margin: EdgeInsets.zero,
    ),
    chipTheme: const ChipThemeData(selectedColor: HoneyColors.cream),
  );
}
