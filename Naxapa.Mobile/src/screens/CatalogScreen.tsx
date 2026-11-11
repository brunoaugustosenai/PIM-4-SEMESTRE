import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, FlatList, Image, Pressable, RefreshControl, StyleSheet, Text, TextInput, View } from 'react-native';
import { mediaUrl, products } from '../services/api';
import { cachedProducts, replaceProducts } from '../services/productCache';
import { colors } from '../theme/colors';
import type { Product } from '../types/models';
import { money } from '../utils/money';

type Props = { onOpen: (product: Product) => void };

type ProductCardProps = { product: Product; onOpen: () => void };
function ProductCard({ product, onOpen }: ProductCardProps) {
  const [image, setImage] = useState<string | null>(null);
  useEffect(() => { mediaUrl(product.imageUrl).then(setImage).catch(() => setImage(null)); }, [product.imageUrl]);
  return (
    <Pressable style={styles.card} onPress={onOpen}>
      {image ? <Image source={{ uri: image }} style={styles.image} /> : <View style={[styles.image, styles.imageEmpty]}><Text style={styles.imageEmptyText}>NAXAPA</Text></View>}
      <View style={styles.cardBody}>
        <Text style={styles.category}>{product.category}</Text>
        <Text style={styles.name} numberOfLines={2}>{product.name}</Text>
        <Text style={styles.code}>{product.code || 'Sem código'} · {product.unit}</Text>
        <Text style={styles.price}>{money(product.price)}</Text>
      </View>
    </Pressable>
  );
}

export function CatalogScreen({ onOpen }: Props) {
  const [items, setItems] = useState<Product[]>([]);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [offline, setOffline] = useState(false);

  const load = useCallback(async (refresh = false) => {
    refresh ? setRefreshing(true) : setLoading(true);
    try {
      const value = (await products()).filter((p) => p.available);
      setItems(value);
      setOffline(false);
      await replaceProducts(value);
    } catch {
      setItems((await cachedProducts()).filter((p) => p.available));
      setOffline(true);
    } finally {
      setLoading(false); setRefreshing(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return items;
    return items.filter((item) => `${item.name} ${item.category} ${item.code ?? ''}`.toLowerCase().includes(q));
  }, [items, query]);

  return (
    <View style={styles.root}>
      <View style={styles.searchBox}>
        <TextInput value={query} onChangeText={setQuery} placeholder="Buscar produto, categoria ou código" style={styles.search} />
        {offline ? <Text style={styles.offline}>Modo offline · exibindo último catálogo salvo</Text> : null}
      </View>
      {loading ? <View style={styles.center}><ActivityIndicator color={colors.green} size="large" /></View> : (
        <FlatList
          data={filtered}
          keyExtractor={(item) => String(item.id)}
          contentContainerStyle={styles.list}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => load(true)} />}
          renderItem={({ item }) => <ProductCard product={item} onOpen={() => onOpen(item)} />}
          ListEmptyComponent={<View style={styles.center}><Text style={styles.empty}>Nenhum produto disponível no cardápio.</Text></View>}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  searchBox: { padding: 14, backgroundColor: colors.surface, borderBottomWidth: 1, borderBottomColor: colors.border },
  search: { borderWidth: 1, borderColor: colors.border, borderRadius: 12, paddingHorizontal: 14, paddingVertical: 11, backgroundColor: '#fff' },
  offline: { color: colors.warning, fontSize: 12, marginTop: 8 },
  list: { padding: 14, gap: 12, paddingBottom: 100 },
  card: { backgroundColor: colors.surface, borderRadius: 16, borderWidth: 1, borderColor: colors.border, overflow: 'hidden', flexDirection: 'row', minHeight: 120 },
  image: { width: 120, minHeight: 120, backgroundColor: '#E8F7F0' },
  imageEmpty: { alignItems: 'center', justifyContent: 'center' },
  imageEmptyText: { color: colors.greenDark, fontWeight: '900', fontSize: 12 },
  cardBody: { flex: 1, padding: 12 },
  category: { color: colors.greenDark, fontWeight: '800', fontSize: 12 },
  name: { color: colors.text, fontWeight: '800', fontSize: 16, marginTop: 3 },
  code: { color: colors.muted, fontSize: 12, marginTop: 5 },
  price: { color: colors.greenDark, fontWeight: '900', fontSize: 18, marginTop: 8 },
  center: { flex: 1, minHeight: 250, alignItems: 'center', justifyContent: 'center', padding: 30 },
  empty: { color: colors.muted, textAlign: 'center' },
});
