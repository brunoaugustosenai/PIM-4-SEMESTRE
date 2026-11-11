import 'package:flutter_test/flutter_test.dart';
import 'package:naxapa_mobile/models/models.dart';
import 'package:naxapa_mobile/services/cart_service.dart';

void main() {
  test('carrinho calcula produto, adicional e quantidade', () {
    final cart = CartService();
    const product = Product(
      id: 1,
      name: 'X-Bacon',
      category: 'Lanches',
      price: 28.0,
      unit: 'UN',
      available: true,
    );

    cart.add(
      product,
      options: const [
        ProductOption(
          id: 1,
          name: 'Bacon extra',
          additionalValue: 5.0,
        ),
      ],
    );

    expect(cart.total, 33.0);
    expect(cart.count, 1);

    cart.increase(0);
    expect(cart.total, 66.0);
    expect(cart.count, 2);
  });
}
