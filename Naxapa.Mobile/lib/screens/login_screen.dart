import 'package:flutter/material.dart';

import '../services/api_client.dart';
import '../services/session_store.dart';

class LoginScreen extends StatefulWidget {
  final ApiClient api;
  final SessionStore store;
  final VoidCallback onSuccess;

  const LoginScreen({
    super.key,
    required this.api,
    required this.store,
    required this.onSuccess,
  });

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final GlobalKey<FormState> formKey = GlobalKey<FormState>();
  final TextEditingController login = TextEditingController();
  final TextEditingController password = TextEditingController();
  final TextEditingController base = TextEditingController();
  final TextEditingController establishment = TextEditingController();

  bool loading = false;
  bool testing = false;
  bool obscurePassword = true;
  String? error;
  String? connectionMessage;

  @override
  void initState() {
    super.initState();
    _loadBaseUrl();
  }

  Future<void> _loadBaseUrl() async {
    final value = await widget.store.baseUrl();
    if (!mounted) return;
    base.text = value;
  }

  @override
  void dispose() {
    login.dispose();
    password.dispose();
    base.dispose();
    establishment.dispose();
    super.dispose();
  }

  String? _validateBaseUrl(String? value) {
    final uri = Uri.tryParse(value?.trim() ?? '');
    if (uri == null ||
        !uri.hasScheme ||
        (uri.scheme != 'http' && uri.scheme != 'https') ||
        uri.host.isEmpty) {
      return 'Informe a API. Ex.: http://10.0.2.2:5180';
    }
    return null;
  }

  Future<void> testConnection() async {
    if (_validateBaseUrl(base.text) != null) {
      formKey.currentState?.validate();
      return;
    }

    setState(() {
      testing = true;
      connectionMessage = null;
      error = null;
    });

    try {
      await widget.store.saveBaseUrl(base.text);
      await widget.api.ping();
      if (!mounted) return;
      setState(() => connectionMessage = 'API e NaxapaDB responderam corretamente.');
    } catch (e) {
      if (!mounted) return;
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => testing = false);
    }
  }

  Future<void> submit() async {
    if (!(formKey.currentState?.validate() ?? false)) return;

    setState(() {
      loading = true;
      error = null;
      connectionMessage = null;
    });

    try {
      await widget.store.saveBaseUrl(base.text);
      await widget.api.login(
        login.text.trim(),
        password.text,
        establishmentId: int.tryParse(establishment.text.trim()),
      );
      if (!mounted) return;
      widget.onSuccess();
    } catch (e) {
      if (!mounted) return;
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 460),
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(28),
                  child: Form(
                    key: formKey,
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text(
                          'NAXAPA',
                          style: Theme.of(context)
                              .textTheme
                              .headlineLarge
                              ?.copyWith(fontWeight: FontWeight.bold),
                        ),
                        const Text('Sua operação no ponto certo'),
                        const SizedBox(height: 24),
                        TextFormField(
                          controller: base,
                          keyboardType: TextInputType.url,
                          autocorrect: false,
                          validator: _validateBaseUrl,
                          decoration: const InputDecoration(
                            labelText: 'Endereço da API',
                            hintText: 'http://10.0.2.2:5180',
                            helperText: 'Emulador Android: 10.0.2.2. Celular: IP do PC.',
                            prefixIcon: Icon(Icons.dns_outlined),
                          ),
                        ),
                        const SizedBox(height: 12),
                        OutlinedButton.icon(
                          onPressed: testing || loading ? null : testConnection,
                          icon: testing
                              ? const SizedBox.square(
                                  dimension: 18,
                                  child: CircularProgressIndicator(strokeWidth: 2),
                                )
                              : const Icon(Icons.wifi_tethering),
                          label: const Text('Testar conexão com a API'),
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: login,
                          textInputAction: TextInputAction.next,
                          validator: (value) => (value?.trim().isEmpty ?? true)
                              ? 'Informe o usuário ou e-mail.'
                              : null,
                          decoration: const InputDecoration(
                            labelText: 'Usuário ou e-mail',
                            prefixIcon: Icon(Icons.person_outline),
                          ),
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: password,
                          obscureText: obscurePassword,
                          textInputAction: TextInputAction.done,
                          onFieldSubmitted: (_) => submit(),
                          validator: (value) => (value?.isEmpty ?? true)
                              ? 'Informe a senha.'
                              : null,
                          decoration: InputDecoration(
                            labelText: 'Senha',
                            prefixIcon: const Icon(Icons.lock_outline),
                            suffixIcon: IconButton(
                              onPressed: () => setState(
                                () => obscurePassword = !obscurePassword,
                              ),
                              icon: Icon(
                                obscurePassword
                                    ? Icons.visibility_outlined
                                    : Icons.visibility_off_outlined,
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: establishment,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'ID do estabelecimento (opcional)',
                            prefixIcon: Icon(Icons.store_outlined),
                          ),
                        ),
                        if (connectionMessage != null)
                          Padding(
                            padding: const EdgeInsets.only(top: 12),
                            child: Text(
                              connectionMessage!,
                              style: TextStyle(
                                color: Theme.of(context).colorScheme.primary,
                              ),
                            ),
                          ),
                        if (error != null)
                          Padding(
                            padding: const EdgeInsets.only(top: 12),
                            child: Text(
                              error!,
                              style: TextStyle(
                                color: Theme.of(context).colorScheme.error,
                              ),
                            ),
                          ),
                        const SizedBox(height: 18),
                        FilledButton.icon(
                          onPressed: loading || testing ? null : submit,
                          icon: loading
                              ? const SizedBox.square(
                                  dimension: 18,
                                  child: CircularProgressIndicator(strokeWidth: 2),
                                )
                              : const Icon(Icons.login),
                          label: const Text('Entrar'),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
