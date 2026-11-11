import 'package:flutter/material.dart';

import '../services/session_store.dart';

class SettingsScreen extends StatefulWidget {
  final SessionStore store;
  final Future<void> Function() onLogout;

  const SettingsScreen({
    super.key,
    required this.store,
    required this.onLogout,
  });

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  String base = '';
  String user = '';
  String establishment = '';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final values = await Future.wait<String>([
      widget.store.baseUrl(),
      widget.store.userName(),
      widget.store.establishmentName(),
    ]);
    if (!mounted) return;
    setState(() {
      base = values[0];
      user = values[1];
      establishment = values[2];
    });
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        ListTile(
          leading: const Icon(Icons.person_outline),
          title: Text(user.isEmpty ? 'Usuário' : user),
          subtitle: Text(establishment),
        ),
        ListTile(
          leading: const Icon(Icons.dns_outlined),
          title: const Text('API'),
          subtitle: Text(base),
        ),
        const Divider(),
        FilledButton.tonalIcon(
          onPressed: widget.onLogout,
          icon: const Icon(Icons.logout),
          label: const Text('Sair'),
        ),
      ],
    );
  }
}
