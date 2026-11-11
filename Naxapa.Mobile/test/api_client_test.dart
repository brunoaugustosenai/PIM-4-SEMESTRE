import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:naxapa_mobile/services/api_client.dart';
import 'package:naxapa_mobile/services/session_store.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues({
      'api_base_url': 'http://10.0.2.2:5180',
    });
  });

  test('login envia contrato correto e interpreta token', () async {
    final store = SessionStore();
    final httpClient = MockClient((request) async {
      expect(request.url.path, '/api/v1/auth/token');
      final body = jsonDecode(request.body) as Map<String, dynamic>;
      expect(body['login'], 'admin');
      expect(body['password'], 'SenhaTeste123!');

      return http.Response(
        jsonEncode({
          'token': 'token-de-teste-1234567890',
          'tokenType': 'Bearer',
          'expiresAtUtc': '2099-11-10T15:00:00Z',
          'userId': 1,
          'userName': 'Administrador',
          'estabelecimentoId': 1,
          'estabelecimento': 'NAXAPA',
        }),
        200,
        headers: {'content-type': 'application/json'},
      );
    });

    final api = ApiClient(store, client: httpClient);
    final session = await api.login('admin', 'SenhaTeste123!');

    expect(session.userId, 1);
    expect(await store.token(), 'token-de-teste-1234567890');
    api.close();
  });

  test('products interpreta ApiPagedResponse da API', () async {
    final store = SessionStore();
    await store.saveSession(
      'token-de-teste-1234567890',
      'Admin',
      'NAXAPA',
      DateTime.now().toUtc().add(const Duration(hours: 1)),
    );

    final httpClient = MockClient((request) async {
      expect(request.url.path, '/api/v1/products');
      expect(request.headers['Authorization'], startsWith('Bearer '));
      return http.Response(
        jsonEncode({
          'items': [
            {
              'id': 1,
              'code': '001',
              'name': 'Produto teste',
              'category': 'Categoria',
              'price': 12.5,
              'unit': 'UN',
              'available': true,
              'imageUrl': null,
            }
          ],
          'page': 1,
          'pageSize': 100,
          'total': 1,
        }),
        200,
        headers: {'content-type': 'application/json'},
      );
    });

    final api = ApiClient(store, client: httpClient);
    final products = await api.products();

    expect(products, hasLength(1));
    expect(products.single.price, 12.5);
    api.close();
  });
}
