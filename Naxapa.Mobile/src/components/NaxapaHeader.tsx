import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors } from '../theme/colors';

type Props = { cartCount?: number };

export function NaxapaHeader({ cartCount = 0 }: Props) {
  return (
    <View style={styles.header}>
      <View>
        <Text style={styles.logo}>NAXAPA</Text>
        <Text style={styles.subtitle}>Sua operação no ponto certo.</Text>
      </View>
      {cartCount > 0 ? (
        <View style={styles.badge}><Text style={styles.badgeText}>{cartCount}</Text></View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  header: { paddingHorizontal: 20, paddingTop: 18, paddingBottom: 14, backgroundColor: colors.surface, borderBottomWidth: 1, borderBottomColor: colors.border, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  logo: { fontSize: 24, fontWeight: '900', color: colors.greenDark, letterSpacing: 1.2 },
  subtitle: { color: colors.muted, marginTop: 2, fontSize: 12 },
  badge: { minWidth: 28, height: 28, borderRadius: 14, paddingHorizontal: 7, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.green },
  badgeText: { color: '#fff', fontWeight: '800' },
});
