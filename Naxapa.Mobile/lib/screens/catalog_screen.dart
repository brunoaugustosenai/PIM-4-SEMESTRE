import 'dart:async';

import 'package:flutter/material.dart';

import '../models/models.dart';
import '../services/api_client.dart';
import '../services/cart_service.dart';
import '../services/local_database.dart';
import 'product_detail_screen.dart';

class CatalogScreen extends StatefulWidget {
  final ApiClient api;
  final CartService cart;

  const CatalogScreen({
    super.key,
    required this.api,
    required this.cart,
  });

  @override
  State<CatalogScreen> createState() => _CatalogScreenState();
}

class _CatalogScreenState extends State<CatalogScreen> {
  final TextEditingController search = TextEditingController();
  Timer? debounce;

  List<Product> products = <Product>[];
  bool loading = true;
  String? message;

  @override
  void initState() {
    super.initState();
    load();
  }

  @override
  void dispose() {
    debounce?.cancel();
    search.dispose();
    super.dispose();
  }

  Future<void> load({bool refresh = true}) async {
    if (mounted) setState(() => loading = true);

    try {
      if (refresh) {
        final remote = await widget.api.products(q: search.text);
        final menuProducts = remote.where((product) => product.available).toList();
        await LocalDatabase.instance.replaceProducts(menuProducts);
        products = menuProducts;
        message = null;
      } else {
        products = await LocalDatabase.instance.products(query: search.text);
      }
    } catch (e) {
      products = await LocalDatabase.instance.products(query: search.text);
      message = products.isEmpty
          ? e.toString()
          : 'Sem conexão com a API: exibindo o último cardápio salvo.';
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: () => load(),
      child: CustomScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        slivers: [
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
              child: TextField(
                controller: search,
                onChanged: (_) {
                  debounce?.cancel();
                  debounce = Timer(
                    const Duration(milliseconds: 300),
                    () => load(refresh: false),
                  );
                },
                decoration: InputDecoration(
                  prefixIcon: const Icon(Icons.search),
                  hintText: 'Pesquisar no cardápio',
                  suffixIcon: IconButton(
                    tooltip: 'Sincronizar com a API',
                    icon: const Icon(Icons.sync),
                    onPressed: () => load(),
                  ),
                ),
              ),
            ),
          ),
          if (message != null)
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
                child: Material(
                  color: Theme.of(context).colorScheme.errorContainer,
                  borderRadius: BorderRadius.circular(12),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Text(message!),
                  ),
                ),
              ),
            ),
          if (loading)
            const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(child: CircularProgressIndicator()),
            )
          else if (products.isEmpty)
            const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(child: Text('Nenhum produto disponível no cardápio.')),
            )
          else
            SliverPadding(
              padding: const EdgeInsets.all(12),
              sliver: SliverGrid(
                gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                  maxCrossAxisExtent: 420,
                  childAspectRatio: 1.45,
                  crossAxisSpacing: 12,
                  mainAxisSpacing: 12,
                ),
                delegate: SliverChildBuilderDelegate(
                  (context, index) {
                    final product = products[index];
                    return _ProductCard(
                      api: widget.api,
                      product: product,
                      onTap: () => _openProduct(product),
                    );
                  },
                  childCount: products.length,
                ),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _openProduct(Product product) async {
    final options = await Navigator.push<List<ProductOption>>(
      context,
      MaterialPageRoute(
        builder: (_) => ProductDetailScreen(
          api: widget.api,
          product: product,
        ),
      ),
    );

    if (!mounted || options == null) return;
    widget.cart.add(product, options: options);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('${product.name} adicionado ao carrinho.')),
    );
  }
}

class _ProductCard extends StatelessWidget {
  final ApiClient api;
  final Product product;
  final VoidCallback onTap;

  const _ProductCard({
    required this.api,
    required this.product,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Expanded(
              child: FutureBuilder<String?>(
                future: api.resolveMediaUrl(product.imageUrl),
                builder: (context, snapshot) {
                  final url = snapshot.data;
                  if (url == null) {
                    return Container(
                      color: Theme.of(context).colorScheme.surfaceContainerHighest,
                      child: const Center(
                        child: Icon(Icons.fastfood_outlined, size: 42),
                      ),
                    );
                  }
                  return Image.network(
                    url,
                    fit: BoxFit.cover,
                    errorBuilder: (_, __, ___) => Container(
                      color: Theme.of(context).colorScheme.surfaceContainerHighest,
                      child: const Center(
                        child: Icon(Icons.broken_image_outlined, size: 42),
                      ),
                    ),
                  );
                },
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    product.category,
                    style: Theme.of(context).textTheme.labelMedium,
                  ),
                  const SizedBox(height: 3),
                  Text(
                    product.name,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: Theme.of(context)
                        .textTheme
                        .titleMedium
                        ?.copyWith(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'R\$ ${product.price.toStringAsFixed(2)}',
                        style: const TextStyle(fontWeight: FontWeight.bold),
                      ),
                      const Icon(Icons.add_circle_outline),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
