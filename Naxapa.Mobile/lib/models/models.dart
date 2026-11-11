class Session {
  final String token;
  final DateTime expiresAtUtc;
  final int userId;
  final String userName;
  final int estabelecimentoId;
  final String estabelecimento;

  const Session({
    required this.token,
    required this.expiresAtUtc,
    required this.userId,
    required this.userName,
    required this.estabelecimentoId,
    required this.estabelecimento,
  });

  factory Session.fromJson(Map<String, dynamic> json) => Session(
        token: json['token'] as String,
        expiresAtUtc: DateTime.parse(json['expiresAtUtc'] as String),
        userId: (json['userId'] as num).toInt(),
        userName: json['userName'] as String,
        estabelecimentoId: (json['estabelecimentoId'] as num).toInt(),
        estabelecimento: json['estabelecimento'] as String,
      );
}

class Product {
  final int id;
  final String? code;
  final String name;
  final String category;
  final double price;
  final String unit;
  final bool available;
  final String? imageUrl;

  const Product({
    required this.id,
    this.code,
    required this.name,
    required this.category,
    required this.price,
    required this.unit,
    required this.available,
    this.imageUrl,
  });

  factory Product.fromJson(Map<String, dynamic> json) => Product(
        id: (json['id'] as num).toInt(),
        code: json['code'] as String?,
        name: json['name'] as String,
        category: json['category'] as String,
        price: (json['price'] as num).toDouble(),
        unit: json['unit'] as String,
        available: json['available'] as bool,
        imageUrl: json['imageUrl'] as String?,
      );

  Map<String, Object?> toDb() => {
        'id': id,
        'code': code,
        'name': name,
        'category': category,
        'price': price,
        'unit': unit,
        'available': available ? 1 : 0,
        'image_url': imageUrl,
        'updated_at': DateTime.now().toUtc().toIso8601String(),
      };

  factory Product.fromDb(Map<String, Object?> row) => Product(
        id: (row['id'] as num).toInt(),
        code: row['code'] as String?,
        name: row['name'] as String,
        category: row['category'] as String,
        price: (row['price'] as num).toDouble(),
        unit: row['unit'] as String,
        available: (row['available'] as num).toInt() == 1,
        imageUrl: row['image_url'] as String?,
      );
}

class ProductDetail {
  final Product product;
  final String? description;
  final List<OptionGroup> groups;

  const ProductDetail(this.product, this.description, this.groups);

  factory ProductDetail.fromJson(Map<String, dynamic> json) {
    final product = Product(
      id: (json['id'] as num).toInt(),
      code: json['code'] as String?,
      name: json['name'] as String,
      category: json['category'] as String,
      price: (json['price'] as num).toDouble(),
      unit: json['unit'] as String,
      available: true,
      imageUrl: json['imageUrl'] as String?,
    );

    final rawGroups = (json['optionGroups'] as List<dynamic>?) ?? const [];
    return ProductDetail(
      product,
      json['description'] as String?,
      rawGroups
          .map((item) => OptionGroup.fromJson(item as Map<String, dynamic>))
          .toList(growable: false),
    );
  }
}

class OptionGroup {
  final int id;
  final String name;
  final String selectionType;
  final int minimum;
  final int maximum;
  final bool required;
  final List<ProductOption> options;

  const OptionGroup({
    required this.id,
    required this.name,
    required this.selectionType,
    required this.minimum,
    required this.maximum,
    required this.required,
    required this.options,
  });

  factory OptionGroup.fromJson(Map<String, dynamic> json) => OptionGroup(
        id: (json['id'] as num).toInt(),
        name: json['name'] as String,
        selectionType: json['selectionType'] as String,
        minimum: (json['minimum'] as num).toInt(),
        maximum: (json['maximum'] as num).toInt(),
        required: json['required'] as bool,
        options: (json['options'] as List<dynamic>)
            .map((item) =>
                ProductOption.fromJson(item as Map<String, dynamic>))
            .toList(growable: false),
      );
}

class ProductOption {
  final int id;
  final String name;
  final double additionalValue;

  const ProductOption({
    required this.id,
    required this.name,
    required this.additionalValue,
  });

  factory ProductOption.fromJson(Map<String, dynamic> json) => ProductOption(
        id: (json['id'] as num).toInt(),
        name: json['name'] as String,
        additionalValue: (json['additionalValue'] as num).toDouble(),
      );
}

class CartItem {
  final Product product;
  int quantity;
  final List<ProductOption> options;

  CartItem({
    required this.product,
    this.quantity = 1,
    List<ProductOption>? options,
  }) : options = options ?? <ProductOption>[];

  double get unitTotal => product.price +
      options.fold<double>(
        0.0,
        (sum, option) => sum + option.additionalValue,
      );

  double get total => quantity * unitTotal;
}

class OrderSummary {
  final int id;
  final int number;
  final String status;
  final String financialStatus;
  final double total;
  final DateTime createdAtUtc;

  const OrderSummary({
    required this.id,
    required this.number,
    required this.status,
    required this.financialStatus,
    required this.total,
    required this.createdAtUtc,
  });

  factory OrderSummary.fromJson(Map<String, dynamic> json) => OrderSummary(
        id: (json['id'] as num).toInt(),
        number: (json['number'] as num).toInt(),
        status: json['status'] as String,
        financialStatus: json['financialStatus'] as String,
        total: (json['total'] as num).toDouble(),
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
      );
}
