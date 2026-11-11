import React, { useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, SafeAreaView, StatusBar, StyleSheet, Text, View } from 'react-native';
import { NaxapaHeader } from './src/components/NaxapaHeader';
import { CartScreen } from './src/screens/CartScreen';
import { CatalogScreen } from './src/screens/CatalogScreen';
import { LoginScreen } from './src/screens/LoginScreen';
import { OrdersScreen } from './src/screens/OrdersScreen';
import { ProductDetailScreen } from './src/screens/ProductDetailScreen';
import { SettingsScreen } from './src/screens/SettingsScreen';
import { revoke } from './src/services/api';
import { getSession } from './src/services/sessionStore';
import { colors } from './src/theme/colors';
import type { CartItem, Product, ProductOption, Session } from './src/types/models';

type Tab = 'catalog' | 'cart' | 'orders' | 'settings';
const tabs: { key: Tab; label: string; glyph: string }[] = [
  { key: 'catalog', label: 'Cardápio', glyph: '▦' },
  { key: 'cart', label: 'Carrinho', glyph: '🛒' },
  { key: 'orders', label: 'Pedidos', glyph: '≡' },
  { key: 'settings', label: 'Ajustes', glyph: '⚙' },
];

export default function App() {
  const [booting, setBooting] = useState(true);
  const [session, setSession] = useState<Session | null>(null);
  const [tab, setTab] = useState<Tab>('catalog');
  const [cart, setCart] = useState<CartItem[]>([]);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [orderRefresh, setOrderRefresh] = useState(0);

  useEffect(() => { getSession().then(setSession).finally(() => setBooting(false)); }, []);
  const cartCount = useMemo(() => cart.reduce((sum, item) => sum + item.quantity, 0), [cart]);

  function addToCart(product: Product, options: ProductOption[]) {
    setCart((items) => [...items, { key: `${Date.now()}-${Math.random()}`, product, quantity: 1, options: [...options] }]);
    setTab('cart');
  }

  async function logout() {
    await revoke(); setSession(null); setCart([]); setTab('catalog');
  }

  if (booting) return <View style={styles.boot}><ActivityIndicator color={colors.green} size="large" /><Text style={styles.bootText}>Iniciando NAXAPA...</Text></View>;
  if (!session) return <LoginScreen onLogin={(value) => { setSession(value); setTab('catalog'); }} />;

  return (
    <SafeAreaView style={styles.safe}>
      <StatusBar barStyle="dark-content" backgroundColor={colors.surface} />
      <NaxapaHeader cartCount={cartCount} />
      <View style={styles.body}>
        {tab === 'catalog' ? <CatalogScreen onOpen={setSelectedProduct} /> : null}
        {tab === 'cart' ? <CartScreen items={cart} onChange={setCart} onSent={() => { setOrderRefresh((v) => v + 1); setTab('orders'); }} /> : null}
        {tab === 'orders' ? <OrdersScreen refreshKey={orderRefresh} /> : null}
        {tab === 'settings' ? <SettingsScreen session={session} onLogout={logout} /> : null}
      </View>
      <View style={styles.nav}>{tabs.map((item) => { const active = tab === item.key; return <Pressable key={item.key} style={styles.navItem} onPress={() => setTab(item.key)}><Text style={[styles.glyph, active && styles.active]}>{item.glyph}</Text><Text style={[styles.navText, active && styles.active]}>{item.label}{item.key === 'cart' && cartCount ? ` (${cartCount})` : ''}</Text></Pressable>; })}</View>
      <ProductDetailScreen visible={!!selectedProduct} product={selectedProduct} onClose={() => setSelectedProduct(null)} onAdd={addToCart} />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({ safe: { flex: 1, backgroundColor: colors.surface }, body: { flex: 1 }, boot: { flex: 1, backgroundColor: colors.background, alignItems: 'center', justifyContent: 'center' }, bootText: { color: colors.muted, marginTop: 12 }, nav: { minHeight: 66, flexDirection: 'row', backgroundColor: colors.surface, borderTopWidth: 1, borderTopColor: colors.border, paddingBottom: 4 }, navItem: { flex: 1, alignItems: 'center', justifyContent: 'center', paddingVertical: 6 }, glyph: { fontSize: 20, color: colors.muted }, navText: { color: colors.muted, fontSize: 11, fontWeight: '700', marginTop: 2 }, active: { color: colors.greenDark } });
