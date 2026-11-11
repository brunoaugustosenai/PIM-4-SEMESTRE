import React, { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native';
import { orders } from '../services/api';
import { colors } from '../theme/colors';
import type { OrderSummary } from '../types/models';
import { money } from '../utils/money';

type Props = { refreshKey: number };
export function OrdersScreen({ refreshKey }: Props) {
  const [items, setItems] = useState<OrderSummary[]>([]); const [loading, setLoading] = useState(true); const [refreshing, setRefreshing] = useState(false); const [error, setError] = useState('');
  const load = useCallback(async (refresh = false) => { refresh ? setRefreshing(true) : setLoading(true); try { setItems(await orders()); setError(''); } catch (e) { setError(String(e)); } finally { setLoading(false); setRefreshing(false); } }, []);
  useEffect(() => { void load(); }, [load, refreshKey]);
  if (loading) return <View style={styles.center}><ActivityIndicator color={colors.green} size="large" /></View>;
  return <View style={styles.root}>{error ? <View style={styles.errorBox}><Text style={styles.error}>{error}</Text><Pressable onPress={() => load()}><Text style={styles.retry}>Tentar novamente</Text></Pressable></View> : null}<FlatList data={items} keyExtractor={(item) => String(item.id)} contentContainerStyle={styles.list} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => load(true)} />} ListEmptyComponent={<View style={styles.center}><Text style={styles.empty}>Nenhum pedido encontrado.</Text></View>} renderItem={({ item }) => <View style={styles.card}><View style={styles.number}><Text style={styles.numberText}>#{item.number}</Text></View><View style={{ flex: 1 }}><Text style={styles.status}>{item.status.replaceAll('_', ' ')}</Text><Text style={styles.meta}>Pagamento: {item.financialStatus}</Text><Text style={styles.meta}>{new Date(item.createdAtUtc).toLocaleString('pt-BR')}</Text></View><Text style={styles.total}>{money(item.total)}</Text></View>} /></View>;
}
const styles = StyleSheet.create({ root: { flex: 1, backgroundColor: colors.background }, list: { padding: 14, gap: 10, paddingBottom: 100 }, card: { padding: 14, borderRadius: 16, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface, flexDirection: 'row', alignItems: 'center' }, number: { width: 48, height: 48, borderRadius: 24, backgroundColor: colors.greenSoft, alignItems: 'center', justifyContent: 'center', marginRight: 12 }, numberText: { color: colors.greenDark, fontWeight: '900', fontSize: 12 }, status: { color: colors.text, fontWeight: '900' }, meta: { color: colors.muted, fontSize: 12, marginTop: 3 }, total: { color: colors.greenDark, fontWeight: '900', marginLeft: 8 }, center: { flex: 1, minHeight: 240, alignItems: 'center', justifyContent: 'center', padding: 30 }, empty: { color: colors.muted }, errorBox: { margin: 14, padding: 14, borderRadius: 12, backgroundColor: '#FEE2E2' }, error: { color: colors.danger }, retry: { color: colors.greenDark, fontWeight: '800', marginTop: 8 } });
