import 'package:flutter/material.dart';

import 'screens/login_screen.dart';
import 'screens/shell_screen.dart';
import 'services/api_client.dart';
import 'services/cart_service.dart';
import 'services/session_store.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const NaxapaApp());
}

class NaxapaApp extends StatefulWidget {
  const NaxapaApp({super.key});

  @override
  State<NaxapaApp> createState() => _NaxapaAppState();
}

class _NaxapaAppState extends State<NaxapaApp> {
  final SessionStore store = SessionStore();
  final CartService cart = CartService();
  late final ApiClient api = ApiClient(store);

  bool? authenticated;

  @override
  void initState() {
    super.initState();
    _restoreSession();
  }

  Future<void> _restoreSession() async {
    final token = await store.token();
    if (!mounted) return;
    setState(() => authenticated = token != null);
  }

  void loggedIn() {
    if (!mounted) return;
    setState(() => authenticated = true);
  }

  Future<void> logout() async {
    await api.revoke();
    await store.clearSession();
    cart.clear();
    if (!mounted) return;
    setState(() => authenticated = false);
  }

  @override
  void dispose() {
    api.close();
    cart.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'NAXAPA',
      debugShowCheckedModeBanner: false,
      themeMode: ThemeMode.system,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF10B981),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
        inputDecorationTheme: const InputDecorationTheme(
          border: OutlineInputBorder(),
        ),
      ),
      darkTheme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF10B981),
          brightness: Brightness.dark,
        ),
        useMaterial3: true,
        inputDecorationTheme: const InputDecorationTheme(
          border: OutlineInputBorder(),
        ),
      ),
      home: authenticated == null
          ? const Scaffold(
              body: Center(child: CircularProgressIndicator()),
            )
          : authenticated!
              ? ShellScreen(
                  api: api,
                  store: store,
                  cart: cart,
                  onLogout: logout,
                )
              : LoginScreen(
                  api: api,
                  store: store,
                  onSuccess: loggedIn,
                ),
    );
  }
}
