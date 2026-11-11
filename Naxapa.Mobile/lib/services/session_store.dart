import 'package:shared_preferences/shared_preferences.dart';

class SessionStore {
  static const String _base = 'api_base_url';
  static const String _token = 'api_token';
  static const String _user = 'user_name';
  static const String _est = 'est_name';
  static const String _exp = 'token_exp';

  Future<SharedPreferences> get _prefs => SharedPreferences.getInstance();

  Future<String> baseUrl() async =>
      (await _prefs).getString(_base) ?? 'http://10.0.2.2:5180';

  Future<void> saveBaseUrl(String value) async {
    final normalized = value.trim().replaceAll(RegExp(r'/+$'), '');
    await (await _prefs).setString(_base, normalized);
  }

  Future<void> saveSession(
    String token,
    String user,
    String establishment,
    DateTime expiresAt,
  ) async {
    final prefs = await _prefs;
    await prefs.setString(_token, token);
    await prefs.setString(_user, user);
    await prefs.setString(_est, establishment);
    await prefs.setString(_exp, expiresAt.toUtc().toIso8601String());
  }

  Future<String?> token() async {
    final prefs = await _prefs;
    final token = prefs.getString(_token);
    final expiresAt = DateTime.tryParse(prefs.getString(_exp) ?? '');

    if (token == null ||
        expiresAt == null ||
        expiresAt.isBefore(DateTime.now().toUtc())) {
      await clearSession();
      return null;
    }

    return token;
  }

  Future<String> userName() async => (await _prefs).getString(_user) ?? '';

  Future<String> establishmentName() async =>
      (await _prefs).getString(_est) ?? '';

  Future<void> clearSession() async {
    final prefs = await _prefs;
    await prefs.remove(_token);
    await prefs.remove(_user);
    await prefs.remove(_est);
    await prefs.remove(_exp);
  }
}
