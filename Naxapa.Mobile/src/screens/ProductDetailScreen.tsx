import React, { useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Image, Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { mediaUrl, product as loadProduct } from '../services/api';
import { colors } from '../theme/colors';
import type { Product, ProductDetail, ProductOption } from '../types/models';
import { cartUnitTotal, money } from '../utils/money';

type Props = { visible: boolean; product: Product | null; onClose: () => void; onAdd: (product: Product, options: ProductOption[]) => void };

export function ProductDetailScreen({ visible, product, onClose, onAdd }: Props) {
  const [detail, setDetail] = useState<ProductDetail | null>(null);
  const [selected, setSelected] = useState<ProductOption[]>([]);
  const [image, setImage] = useState<string | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!visible || !product) return;
    setDetail(null); setSelected([]); setError('');
    loadProduct(product.id).then((value) => { setDetail(value); void mediaUrl(value.imageUrl).then(setImage); }).catch((e) => setError(String(e)));
  }, [visible, product]);

  const total = useMemo(() => detail ? cartUnitTotal(detail.price, selected.map((o) => o.additionalValue)) : 0, [detail, selected]);

  function toggle(option: ProductOption, groupOptionIds: number[], groupMax: number, selectionType: string) {
    setSelected((current) => {
      const exists = current.some((o) => o.id === option.id);
      if (exists) return current.filter((o) => o.id !== option.id);
      const groupIds = new Set(groupOptionIds);
      const selectedInGroup = current.filter((o) => groupIds.has(o.id));
      if (selectionType.toUpperCase().includes('UNICA') || groupMax === 1) {
        return [...current.filter((o) => !groupIds.has(o.id)), option];
      }
      if (groupMax > 0 && selectedInGroup.length >= groupMax) return current;
      return [...current, option];
    });
  }

  const selectionsValid = detail ? detail.optionGroups.every((group) => {
    const ids = new Set(group.options.map((o) => o.id));
    const count = selected.filter((o) => ids.has(o.id)).length;
    return count >= group.minimum && (group.maximum <= 0 || count <= group.maximum);
  }) : false;

  return (
    <Modal visible={visible} animationType="slide" onRequestClose={onClose}>
      <View style={styles.root}>
        <View style={styles.top}><Pressable onPress={onClose}><Text style={styles.close}>Fechar</Text></Pressable><Text style={styles.topTitle}>Produto</Text><View style={{ width: 50 }} /></View>
        {!detail && !error ? <View style={styles.center}><ActivityIndicator color={colors.green} size="large" /></View> : error ? <View style={styles.center}><Text style={styles.error}>{error}</Text></View> : detail ? (
          <ScrollView contentContainerStyle={styles.content}>
            {image ? <Image source={{ uri: image }} style={styles.hero} /> : null}
            <Text style={styles.category}>{detail.category}</Text>
            <Text style={styles.title}>{detail.name}</Text>
            {detail.description ? <Text style={styles.description}>{detail.description}</Text> : null}
            <Text style={styles.price}>{money(detail.price)}</Text>
            {detail.optionGroups.map((group) => (
              <View key={group.id} style={styles.group}>
                <Text style={styles.groupTitle}>{group.name}{group.required ? ' *' : ''}</Text>
                <Text style={styles.groupHint}>Escolha {group.minimum} a {group.maximum}</Text>
                {group.options.map((option) => {
                  const active = selected.some((item) => item.id === option.id);
                  return <Pressable key={option.id} style={[styles.option, active && styles.optionActive]} onPress={() => toggle(option, group.options.map((o) => o.id), group.maximum, group.selectionType)}>
                    <View style={[styles.check, active && styles.checkActive]} />
                    <Text style={styles.optionName}>{option.name}</Text>
                    <Text style={styles.optionPrice}>{option.additionalValue ? `+ ${money(option.additionalValue)}` : 'Incluso'}</Text>
                  </Pressable>;
                })}
              </View>
            ))}
            <Pressable style={[styles.add, !selectionsValid && styles.addDisabled]} disabled={!selectionsValid} onPress={() => { onAdd(detail, selected); onClose(); }}><Text style={styles.addText}>{selectionsValid ? `Adicionar · ${money(total)}` : 'Complete as opções obrigatórias'}</Text></Pressable>
          </ScrollView>
        ) : null}
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  top: { paddingTop: 18, paddingHorizontal: 18, paddingBottom: 14, flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', backgroundColor: colors.surface, borderBottomWidth: 1, borderBottomColor: colors.border },
  close: { color: colors.greenDark, fontWeight: '800' }, topTitle: { fontWeight: '900', fontSize: 17, color: colors.text },
  content: { padding: 18, paddingBottom: 50 }, hero: { width: '100%', height: 220, borderRadius: 18, backgroundColor: '#E8F7F0' },
  category: { marginTop: 18, color: colors.greenDark, fontWeight: '800' }, title: { marginTop: 4, color: colors.text, fontSize: 26, fontWeight: '900' },
  description: { marginTop: 8, color: colors.muted, lineHeight: 21 }, price: { marginTop: 10, fontSize: 22, fontWeight: '900', color: colors.greenDark },
  group: { marginTop: 24 }, groupTitle: { color: colors.text, fontSize: 17, fontWeight: '900' }, groupHint: { color: colors.muted, fontSize: 12, marginTop: 2, marginBottom: 8 },
  option: { flexDirection: 'row', alignItems: 'center', padding: 13, borderWidth: 1, borderColor: colors.border, borderRadius: 12, marginTop: 8, backgroundColor: colors.surface },
  optionActive: { borderColor: colors.green, backgroundColor: colors.greenSoft }, check: { width: 18, height: 18, borderRadius: 9, borderWidth: 2, borderColor: '#9CA3AF', marginRight: 10 }, checkActive: { borderColor: colors.greenDark, backgroundColor: colors.green },
  optionName: { flex: 1, color: colors.text, fontWeight: '700' }, optionPrice: { color: colors.greenDark, fontWeight: '700' },
  add: { marginTop: 26, borderRadius: 13, backgroundColor: colors.green, paddingVertical: 15, alignItems: 'center' }, addDisabled: { opacity: 0.45 }, addText: { color: '#fff', fontWeight: '900', fontSize: 16 },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 30 }, error: { color: colors.danger, textAlign: 'center' },
});
