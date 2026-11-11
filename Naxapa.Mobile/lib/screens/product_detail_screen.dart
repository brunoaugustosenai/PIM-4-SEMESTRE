import 'package:flutter/material.dart';

import '../models/models.dart';
import '../services/api_client.dart';

class ProductDetailScreen extends StatefulWidget {
  final ApiClient api;
  final Product product;

  const ProductDetailScreen({
    super.key,
    required this.api,
    required this.product,
  });

  @override
  State<ProductDetailScreen> createState() => _ProductDetailScreenState();
}

class _ProductDetailScreenState extends State<ProductDetailScreen> {
  ProductDetail? detail;
  String? error;
  final Map<int, Set<int>> selected = <int, Set<int>>{};

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final value = await widget.api.product(widget.product.id);
      if (!mounted) return;
      setState(() => detail = value);
    } catch (e) {
      if (!mounted) return;
      setState(() => error = e.toString());
    }
  }

  bool get valid {
    final value = detail;
    if (value == null) return false;

    for (final group in value.groups) {
      final count = selected[group.id]?.length ?? 0;
      if (count < group.minimum || count > group.maximum) return false;
      if (group.required && count == 0) return false;
    }
    return true;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.product.name)),
      body: error != null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(error!, textAlign: TextAlign.center),
                    const SizedBox(height: 12),
                    OutlinedButton.icon(
                      onPressed: () {
                        setState(() => error = null);
                        _load();
                      },
                      icon: const Icon(Icons.refresh),
                      label: const Text('Tentar novamente'),
                    ),
                  ],
                ),
              ),
            )
          : detail == null
              ? const Center(child: CircularProgressIndicator())
              : _body(context, detail!),
    );
  }

  Widget _body(BuildContext context, ProductDetail value) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        FutureBuilder<String?>(
          future: widget.api.resolveMediaUrl(value.product.imageUrl),
          builder: (context, snapshot) {
            final url = snapshot.data;
            if (url == null) return const SizedBox.shrink();
            return Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(16),
                child: AspectRatio(
                  aspectRatio: 16 / 9,
                  child: Image.network(
                    url,
                    fit: BoxFit.cover,
                    errorBuilder: (_, __, ___) => const SizedBox.shrink(),
                  ),
                ),
              ),
            );
          },
        ),
        Text(
          value.product.name,
          style: Theme.of(context)
              .textTheme
              .headlineSmall
              ?.copyWith(fontWeight: FontWeight.bold),
        ),
        if (value.description?.trim().isNotEmpty ?? false)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(value.description!),
          ),
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 12),
          child: Text(
            'R\$ ${value.product.price.toStringAsFixed(2)}',
            style: Theme.of(context)
                .textTheme
                .titleLarge
                ?.copyWith(fontWeight: FontWeight.bold),
          ),
        ),
        ...value.groups.map(_groupCard),
        const SizedBox(height: 16),
        FilledButton.icon(
          onPressed: valid ? _finish : null,
          icon: const Icon(Icons.add_shopping_cart),
          label: const Text('Adicionar ao carrinho'),
        ),
      ],
    );
  }

  Widget _groupCard(OptionGroup group) {
    final singleChoice = group.selectionType.toUpperCase() == 'U';

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              group.name,
              style: Theme.of(context)
                  .textTheme
                  .titleMedium
                  ?.copyWith(fontWeight: FontWeight.bold),
            ),
            Text(
              'Escolha ${group.minimum} a ${group.maximum}'
              '${group.required ? ' · obrigatório' : ''}',
            ),
            const SizedBox(height: 6),
            ...group.options.map((option) {
              final checked = selected[group.id]?.contains(option.id) ?? false;

              return CheckboxListTile(
                value: checked,
                title: Text(option.name),
                subtitle: _priceSubtitle(option),
                onChanged: (value) {
                  setState(() {
                    final set = selected.putIfAbsent(group.id, () => <int>{});
                    if (value == true) {
                      if (singleChoice) {
                        set
                          ..clear()
                          ..add(option.id);
                      } else if (set.length < group.maximum) {
                        set.add(option.id);
                      }
                    } else {
                      set.remove(option.id);
                    }
                  });
                },
              );
            }),
          ],
        ),
      ),
    );
  }

  Widget? _priceSubtitle(ProductOption option) {
    if (option.additionalValue <= 0) return null;
    return Text('+ R\$ ${option.additionalValue.toStringAsFixed(2)}');
  }

  void _finish() {
    final value = detail;
    if (value == null) return;

    final options = <ProductOption>[];
    for (final group in value.groups) {
      for (final option in group.options) {
        if (selected[group.id]?.contains(option.id) ?? false) {
          options.add(option);
        }
      }
    }

    Navigator.pop(context, options);
  }
}

