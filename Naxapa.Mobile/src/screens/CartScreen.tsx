import React, { useMemo, useState } from 'react';
import { ActivityIndicator, Alert, FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { sendCart } from '../services/api';
import { colors } from '../theme/colors';
import type { CartItem } from '../types/models';
import { cartUnitTotal, money } from '../utils/money';

type Props = { items: CartItem[]; onChange: (items: CartItem[]) => void; onSent: () => void };

export function CartScreen({ items, onChange, onSent }: Props) {
  const [sending, setSending] = useState(false);
  const total = useMemo(() => items.reduce((sum, item) => sum + cartUnitTotal(item.product.price, item.options.map((o) => o.additionalValue)) * item.quantity, 0), [items]);

  function quantity(index: number, delta: number) {
    const next = items.map((item) => ({ ...item, options: [...item.options] }));
    const target = next[index]; if (!target) return;
    target.quantity += delta;
    onChange(target.quantity <= 0 ? next.filter((_, i) => i !== index) : next);
  }

  async function submit() {
    setSending(true);
    try {
      const number = await sendCart(items);
      onChange([]); onSent();
      Alert.alert('Pedido enviado', `Pedido #${number} enviado com sucesso.`);
    } catch (error) { Alert.alert('Falha ao enviar', String(error)); }
    finally { setSending(false); }
  }

  return (
    <View style={styles.root}>
      <FlatList data={items} keyExtractor={(item) => item.key} contentContainerStyle={styles.list}
        ListEmptyComponent={<View style={styles.empty}><Text style={styles.emptyTitle}>Seu carrinho está vazio</Text><Text style={styles.emptyText}>Escolha produtos no cardápio.</Text></View>}
        renderItem={({ item, index }) => {
          const unit = cartUnitTotal(item.product.price, item.options.map((o) => o.additionalValue));
          return <View style={styles.item}><View style={{ flex: 1 }}><Text style={styles.itemName}>{item.product.name}</Text>{item.options.length ? <Text style={styles.options}>{item.options.map((o) => o.name).join(', ')}</Text> : null}<Text style={styles.itemPrice}>{money(unit * item.quantity)}</Text></View><View style={styles.qty}><Pressable onPress={() => quantity(index, -1)} style={styles.qtyButton}><Text style={styles.qtyText}>−</Text></Pressable><Text style={styles.qtyNumber}>{item.quantity}</Text><Pressable onPress={() => quantity(index, 1)} style={styles.qtyButton}><Text style={styles.qtyText}>+</Text></Pressable></View></View>;
        }} />
      {items.length ? <View style={styles.footer}><View style={styles.totalRow}><Text style={styles.totalLabel}>Total</Text><Text style={styles.total}>{money(total)}</Text></View><Pressable style={styles.send} onPress={submit} disabled={sending}>{sending ? <ActivityIndicator color="#fff" /> : <Text style={styles.sendText}>Enviar pedido</Text>}</Pressable></View> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background }, list: { padding: 14, gap: 10, paddingBottom: 180 },
  item: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, borderRadius: 16, padding: 14, flexDirection: 'row', alignItems: 'center' },
  itemName: { color: colors.text, fontWeight: '900', fontSize: 16 }, options: { color: colors.muted, marginTop: 4, fontSize: 12 }, itemPrice: { color: colors.greenDark, fontWeight: '900', marginTop: 8 },
  qty: { flexDirection: 'row', alignItems: 'center', marginLeft: 12 }, qtyButton: { width: 36, height: 36, borderRadius: 10, backgroundColor: colors.greenSoft, alignItems: 'center', justifyContent: 'center' }, qtyText: { color: colors.greenDark, fontWeight: '900', fontSize: 20 }, qtyNumber: { minWidth: 36, textAlign: 'center', fontWeight: '900' },
  footer: { position: 'absolute', left: 0, right: 0, bottom: 0, backgroundColor: colors.surface, padding: 16, borderTopWidth: 1, borderTopColor: colors.border }, totalRow: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 12 }, totalLabel: { fontSize: 16, fontWeight: '800' }, total: { fontSize: 20, fontWeight: '900', color: colors.greenDark }, send: { backgroundColor: colors.green, borderRadius: 13, paddingVertical: 14, alignItems: 'center' }, sendText: { color: '#fff', fontWeight: '900', fontSize: 16 },
  empty: { marginTop: 130, alignItems: 'center', padding: 30 }, emptyTitle: { fontWeight: '900', fontSize: 19, color: colors.text }, emptyText: { marginTop: 6, color: colors.muted },
});
