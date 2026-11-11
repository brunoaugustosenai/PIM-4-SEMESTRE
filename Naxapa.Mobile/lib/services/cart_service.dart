import 'package:flutter/foundation.dart';

import '../models/models.dart';

class CartService extends ChangeNotifier {
  final List<CartItem> _items = <CartItem>[];

  List<CartItem> get items => List<CartItem>.unmodifiable(_items);

  double get total => _items.fold<double>(
        0.0,
        (sum, item) => sum + item.total,
      );

  int get count => _items.fold<int>(
        0,
        (sum, item) => sum + item.quantity,
      );

  void add(
    Product product, {
    List<ProductOption> options = const <ProductOption>[],
  }) {
    _items.add(
      CartItem(
        product: product,
        options: List<ProductOption>.of(options),
      ),
    );
    notifyListeners();
  }

  void removeAt(int index) {
    if (index < 0 || index >= _items.length) return;
    _items.removeAt(index);
    notifyListeners();
  }

  void increase(int index) {
    if (index < 0 || index >= _items.length) return;
    _items[index].quantity++;
    notifyListeners();
  }

  void decrease(int index) {
    if (index < 0 || index >= _items.length) return;
    if (_items[index].quantity <= 1) {
      _items.removeAt(index);
    } else {
      _items[index].quantity--;
    }
    notifyListeners();
  }

  void clear() {
    if (_items.isEmpty) return;
    _items.clear();
    notifyListeners();
  }
}
