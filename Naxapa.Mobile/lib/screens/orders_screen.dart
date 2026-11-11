import 'package:flutter/material.dart';

import '../models/models.dart';
import '../services/api_client.dart';

class OrdersScreen extends StatefulWidget {
  final ApiClient api;

  const OrdersScreen({
    super.key,
    required this.api,
  });

  @override
  State<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends State<OrdersScreen> {
  List<OrderSummary>? orders;
  String? error;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      final value = await widget.api.orders();
      if (!mounted) return;
      setState(() {
        orders = value;
        error = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => error = e.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    final value = orders;

    return RefreshIndicator(
      onRefresh: load,
      child: error != null
          ? ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              children: [
                Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    children: [
                      Text(error!, textAlign: TextAlign.center),
                      const SizedBox(height: 12),
                      OutlinedButton.icon(
                        onPressed: load,
                        icon: const Icon(Icons.refresh),
                        label: const Text('Tentar novamente'),
                      ),
                    ],
                  ),
                ),
              ],
            )
          : value == null
              ? ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: const [
                    SizedBox(height: 240),
                    Center(child: CircularProgressIndicator()),
                  ],
                )
              : value.isEmpty
                  ? ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      children: const [
                        SizedBox(height: 240),
                        Center(child: Text('Nenhum pedido encontrado.')),
                      ],
                    )
                  : ListView.separated(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: const EdgeInsets.all(16),
                      itemCount: value.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 8),
                      itemBuilder: (context, index) {
                        final order = value[index];
                        return Card(
                          child: ListTile(
                            leading: CircleAvatar(child: Text('#${order.number}')),
                            title: Text(order.status.replaceAll('_', ' ')),
                            subtitle: Text(
                              'Pagamento: ${order.financialStatus}\n'
                              '${order.createdAtUtc.toLocal()}',
                            ),
                            trailing: Text(
                              'R\$ ${order.total.toStringAsFixed(2)}',
                              style: const TextStyle(fontWeight: FontWeight.bold),
                            ),
                          ),
                        );
                      },
                    ),
    );
  }
}
