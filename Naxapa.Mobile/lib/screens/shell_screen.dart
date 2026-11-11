import 'package:flutter/material.dart';

import '../services/api_client.dart';
import '../services/cart_service.dart';
import '../services/session_store.dart';
import 'cart_screen.dart';
import 'catalog_screen.dart';
import 'orders_screen.dart';
import 'settings_screen.dart';

class ShellScreen extends StatefulWidget {
  final ApiClient api;
  final SessionStore store;
  final CartService cart;
  final Future<void> Function() onLogout;

  const ShellScreen({
    super.key,
    required this.api,
    required this.store,
    required this.cart,
    required this.onLogout,
  });

  @override
  State<ShellScreen> createState() => _ShellScreenState();
}

class _ShellScreenState extends State<ShellScreen> {
  int index = 0;

  @override
  Widget build(BuildContext context) {
    final pages = <Widget>[
      CatalogScreen(api: widget.api, cart: widget.cart),
      CartScreen(api: widget.api, cart: widget.cart),
      OrdersScreen(api: widget.api),
      SettingsScreen(store: widget.store, onLogout: widget.onLogout),
    ];

    const destinations = <NavigationDestination>[
      NavigationDestination(
        icon: Icon(Icons.restaurant_menu),
        label: 'Cardápio',
      ),
      NavigationDestination(
        icon: Icon(Icons.shopping_cart),
        label: 'Carrinho',
      ),
      NavigationDestination(
        icon: Icon(Icons.receipt_long),
        label: 'Pedidos',
      ),
      NavigationDestination(
        icon: Icon(Icons.settings),
        label: 'Ajustes',
      ),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        return Scaffold(
          appBar: AppBar(
            title: const Text('NAXAPA'),
            actions: [
              AnimatedBuilder(
                animation: widget.cart,
                builder: (_, __) {
                  return Padding(
                    padding: const EdgeInsets.only(right: 16),
                    child: Center(
                      child: Badge(
                        isLabelVisible: widget.cart.count > 0,
                        label: Text('${widget.cart.count}'),
                        child: const Icon(Icons.shopping_cart_outlined),
                      ),
                    ),
                  );
                },
              ),
            ],
          ),
          body: constraints.maxWidth >= 800
              ? Row(
                  children: [
                    NavigationRail(
                      selectedIndex: index,
                      onDestinationSelected: (value) =>
                          setState(() => index = value),
                      labelType: NavigationRailLabelType.all,
                      destinations: destinations
                          .map(
                            (destination) => NavigationRailDestination(
                              icon: destination.icon,
                              label: Text(destination.label),
                            ),
                          )
                          .toList(growable: false),
                    ),
                    const VerticalDivider(width: 1),
                    Expanded(child: IndexedStack(index: index, children: pages)),
                  ],
                )
              : IndexedStack(index: index, children: pages),
          bottomNavigationBar: constraints.maxWidth >= 800
              ? null
              : NavigationBar(
                  selectedIndex: index,
                  onDestinationSelected: (value) =>
                      setState(() => index = value),
                  destinations: destinations,
                ),
        );
      },
    );
  }
}
