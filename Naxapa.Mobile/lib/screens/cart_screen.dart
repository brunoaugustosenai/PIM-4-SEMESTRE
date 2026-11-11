import 'package:flutter/material.dart';

import '../services/api_client.dart';
import '../services/cart_service.dart';

class CartScreen extends StatefulWidget {
  final ApiClient api;
  final CartService cart;

  const CartScreen({
    super.key,
    required this.api,
    required this.cart,
  });

  @override
  State<CartScreen> createState() => _CartScreenState();
}

class _CartScreenState extends State<CartScreen> {
  bool sending = false;
  String? message;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: widget.cart,
      builder: (context, _) {
        final items = widget.cart.items;
        return Column(
          children: [
            Expanded(
              child: items.isEmpty
                  ? const Center(child: Text('Seu carrinho está vazio.'))
                  : ListView.separated(
                      padding: const EdgeInsets.all(16),
                      itemCount: items.length,
                      separatorBuilder: (_, __) => const Divider(),
                      itemBuilder: (context, index) {
                        final item = items[index];
                        return ListTile(
                          title: Text(item.product.name),
                          subtitle: Text(
                            [
                              if (item.options.isNotEmpty)
                                item.options.map((o) => o.name).join(', '),
                              '${item.quantity} × R\$ ${item.unitTotal.toStringAsFixed(2)}',
                            ].join('\n'),
                          ),
                          trailing: Wrap(
                            crossAxisAlignment: WrapCrossAlignment.center,
                            children: [
                              IconButton(
                                tooltip: 'Diminuir',
                                onPressed: () => widget.cart.decrease(index),
                                icon: const Icon(Icons.remove_circle_outline),
                              ),
                              Text('${item.quantity}'),
                              IconButton(
                                tooltip: 'Aumentar',
                                onPressed: () => widget.cart.increase(index),
                                icon: const Icon(Icons.add_circle_outline),
                              ),
                              const SizedBox(width: 8),
                              Text(
                                'R\$ ${item.total.toStringAsFixed(2)}',
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                            ],
                          ),
                        );
                      },
                    ),
            ),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (message != null)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: Text(message!),
                      ),
                    Text(
                      'Total: R\$ ${widget.cart.total.toStringAsFixed(2)}',
                      textAlign: TextAlign.end,
                      style: Theme.of(context)
                          .textTheme
                          .titleLarge
                          ?.copyWith(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 8),
                    FilledButton.icon(
                      onPressed: items.isEmpty || sending ? null : send,
                      icon: sending
                          ? const SizedBox.square(
                              dimension: 18,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(Icons.send),
                      label: const Text('Enviar pedido'),
                    ),
                  ],
                ),
              ),
            ),
          ],
        );
      },
    );
  }

  Future<void> send() async {
    setState(() {
      sending = true;
      message = null;
    });

    try {
      final number = await widget.api.sendCart(widget.cart.items);
      widget.cart.clear();
      if (!mounted) return;
      setState(() => message = 'Pedido #$number enviado com sucesso.');
    } catch (e) {
      if (!mounted) return;
      setState(() => message = e.toString());
    } finally {
      if (mounted) setState(() => sending = false);
    }
  }
}
