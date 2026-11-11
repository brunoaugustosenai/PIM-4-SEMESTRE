import 'package:flutter_test/flutter_test.dart';
import 'package:naxapa_mobile/models/models.dart';

void main() {
  test('Session interpreta o contrato da API NAXAPA', () {
    final session = Session.fromJson({
      'token': 'abc',
      'expiresAtUtc': '2026-11-10T15:00:00Z',
      'userId': 7,
      'userName': 'Administrador',
      'estabelecimentoId': 3,
      'estabelecimento': 'NAXAPA Teste',
    });

    expect(session.userId, 7);
    expect(session.estabelecimentoId, 3);
  });

  test('Product interpreta decimal enviado como inteiro ou double', () {
    final product = Product.fromJson({
      'id': 10,
      'code': '7891234567895',
      'name': 'Produto',
      'category': 'Categoria',
      'price': 15,
      'unit': 'UN',
      'available': true,
      'imageUrl': '/uploads/products/teste.jpg',
    });

    expect(product.price, 15.0);
    expect(product.available, isTrue);
  });

  test('CartItem soma adicionais usando double', () {
    const product = Product(
      id: 1,
      name: 'Produto',
      category: 'Categoria',
      price: 10.0,
      unit: 'UN',
      available: true,
    );
    final item = CartItem(
      product: product,
      quantity: 2,
      options: const [
        ProductOption(id: 1, name: 'Extra', additionalValue: 2.5),
      ],
    );

    expect(item.unitTotal, 12.5);
    expect(item.total, 25.0);
  });
}
