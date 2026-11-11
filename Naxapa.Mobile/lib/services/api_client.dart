import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/models.dart';
import 'session_store.dart';

class ApiException implements Exception {
  final String message;
  final int? statusCode;

  ApiException(this.message, [this.statusCode]);

  @override
  String toString() => message;
}

class UnauthorizedException extends ApiException {
  UnauthorizedException()
      : super('Sua sessão expirou. Entre novamente.', 401);
}

class ApiClient {
  ApiClient(
    this.store, {
    http.Client? client,
    this.requestTimeout = const Duration(seconds: 20),
  }) : _http = client ?? http.Client();

  final SessionStore store;
  final http.Client _http;
  final Duration requestTimeout;

  Future<Uri> _uri(
    String path, [
    Map<String, String>? query,
  ]) async {
    final base = (await store.baseUrl()).trim().replaceAll(RegExp(r'/+$'), '');
    final baseUri = Uri.tryParse(base);
    if (baseUri == null ||
        !baseUri.hasScheme ||
        (baseUri.scheme != 'http' && baseUri.scheme != 'https') ||
        baseUri.host.isEmpty) {
      throw ApiException(
        'Endereço da API inválido. Exemplo: http://10.0.2.2:5180',
      );
    }

    return Uri.parse('$base$path').replace(queryParameters: query);
  }

  Future<Map<String, String>> _headers({bool auth = true}) async {
    final headers = <String, String>{
      'Accept': 'application/json',
      'Content-Type': 'application/json',
    };

    if (auth) {
      final token = await store.token();
      if (token == null) throw UnauthorizedException();
      headers['Authorization'] = 'Bearer $token';
    }

    return headers;
  }

  Future<http.Response> _get(Uri uri, {bool auth = true}) async {
    try {
      return await _http
          .get(uri, headers: await _headers(auth: auth))
          .timeout(requestTimeout);
    } on TimeoutException {
      throw ApiException('Tempo esgotado ao conectar com a API NAXAPA.');
    } on http.ClientException catch (e) {
      throw ApiException('Não foi possível conectar com a API: ${e.message}');
    }
  }

  Future<http.Response> _post(
    Uri uri, {
    bool auth = true,
    Object? body,
  }) async {
    try {
      return await _http
          .post(
            uri,
            headers: await _headers(auth: auth),
            body: body,
          )
          .timeout(requestTimeout);
    } on TimeoutException {
      throw ApiException('Tempo esgotado ao conectar com a API NAXAPA.');
    } on http.ClientException catch (e) {
      throw ApiException('Não foi possível conectar com a API: ${e.message}');
    }
  }

  Future<void> ping() async {
    final response = await _get(await _uri('/health'), auth: false);
    _decode(response);
  }

  Future<Session> login(
    String login,
    String password, {
    int? establishmentId,
  }) async {
    final response = await _post(
      await _uri('/api/v1/auth/token'),
      auth: false,
      body: jsonEncode({
        'login': login,
        'password': password,
        'estabelecimentoId': establishmentId,
      }),
    );

    final body = _decode(response) as Map<String, dynamic>;
    final session = Session.fromJson(body);
    await store.saveSession(
      session.token,
      session.userName,
      session.estabelecimento,
      session.expiresAtUtc,
    );
    return session;
  }

  Future<void> revoke() async {
    final token = await store.token();
    if (token == null) return;
    try {
      final response = await _post(await _uri('/api/v1/auth/revoke'));
      _decode(response, allowEmpty: true);
    } catch (_) {
      // Logout local deve continuar mesmo se a API estiver indisponível.
    }
  }

  Future<List<Product>> products({String q = ''}) async {
    final response = await _get(
      await _uri('/api/v1/products', {
        'q': q,
        'page': '1',
        'pageSize': '100',
      }),
    );
    final json = _decode(response) as Map<String, dynamic>;
    return (json['items'] as List<dynamic>)
        .map((item) => Product.fromJson(item as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<ProductDetail> product(int id) async {
    final response = await _get(await _uri('/api/v1/products/$id'));
    return ProductDetail.fromJson(
      _decode(response) as Map<String, dynamic>,
    );
  }

  Future<List<OrderSummary>> orders() async {
    final response = await _get(
      await _uri('/api/v1/orders', {
        'page': '1',
        'pageSize': '100',
      }),
    );
    final json = _decode(response) as Map<String, dynamic>;
    return (json['items'] as List<dynamic>)
        .map((item) => OrderSummary.fromJson(item as Map<String, dynamic>))
        .toList(growable: false);
  }

  Future<OrderSummary> createOrder({
    String serviceType = 'BALCAO',
    String? notes,
  }) async {
    final response = await _post(
      await _uri('/api/v1/orders'),
      body: jsonEncode({
        'serviceType': serviceType,
        'notes': notes,
      }),
    );
    return OrderSummary.fromJson(
      _decode(response) as Map<String, dynamic>,
    );
  }

  Future<void> addItem(int orderId, CartItem item) async {
    final response = await _post(
      await _uri('/api/v1/orders/$orderId/items'),
      body: jsonEncode({
        'productId': item.product.id,
        'quantity': item.quantity,
      }),
    );

    final json = _decode(response) as Map<String, dynamic>;
    final itemId = (json['pedidoItemId'] as num).toInt();

    for (final option in item.options) {
      final optionResponse = await _post(
        await _uri('/api/v1/orders/items/$itemId/options'),
        body: jsonEncode({
          'optionId': option.id,
          'quantity': 1,
        }),
      );
      _decode(optionResponse);
    }
  }

  Future<void> changeStatus(int id, String status) async {
    final response = await _post(
      await _uri('/api/v1/orders/$id/status'),
      body: jsonEncode({'status': status}),
    );
    _decode(response, allowEmpty: true);
  }

  Future<int> sendCart(List<CartItem> cart) async {
    if (cart.isEmpty) throw ApiException('Carrinho vazio.');

    final order = await createOrder();
    for (final item in cart) {
      await addItem(order.id, item);
    }
    await changeStatus(order.id, 'PENDENTE');
    return order.number;
  }

  Future<String?> resolveMediaUrl(String? value) async {
    if (value == null || value.trim().isEmpty) return null;
    final parsed = Uri.tryParse(value);
    if (parsed != null && parsed.hasScheme) return parsed.toString();

    final base = await store.baseUrl();
    final baseUri = Uri.tryParse(base);
    if (baseUri == null || !baseUri.hasScheme) return null;

    final normalizedBase = base.endsWith('/') ? base : '$base/';
    final relative = value.startsWith('/') ? value.substring(1) : value;
    return Uri.parse(normalizedBase).resolve(relative).toString();
  }

  dynamic _decode(
    http.Response response, {
    bool allowEmpty = false,
  }) {
    if (response.statusCode == 401) throw UnauthorizedException();

    if (response.statusCode < 200 || response.statusCode >= 300) {
      var message = 'Falha HTTP ${response.statusCode}';
      try {
        final json = jsonDecode(response.body) as Map<String, dynamic>;
        message = (json['detail'] ?? json['title'] ?? message).toString();
      } catch (_) {
        if (response.body.trim().isNotEmpty) {
          message = response.body.trim();
        }
      }
      throw ApiException(message, response.statusCode);
    }

    if (response.body.trim().isEmpty) {
      if (allowEmpty) return null;
      return null;
    }

    return jsonDecode(response.body);
  }

  void close() => _http.close();
}
