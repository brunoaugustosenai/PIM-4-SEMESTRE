import React, { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { getBaseUrl } from '../services/sessionStore';
import { colors } from '../theme/colors';
import type { Session } from '../types/models';

type Props = { session: Session; onLogout: () => Promise<void> };
export function SettingsScreen({ session, onLogout }: Props) {
  const [base, setBase] = useState('');
  useEffect(() => { getBaseUrl().then(setBase).catch(() => undefined); }, []);
  return <View style={styles.root}><View style={styles.card}><Text style={styles.label}>Usuário</Text><Text style={styles.value}>{session.userName}</Text><Text style={styles.label}>Estabelecimento</Text><Text style={styles.value}>{session.estabelecimento}</Text><Text style={styles.label}>API</Text><Text style={styles.api}>{base}</Text></View><Pressable style={styles.logout} onPress={onLogout}><Text style={styles.logoutText}>Sair</Text></Pressable></View>;
}
const styles = StyleSheet.create({ root: { flex: 1, padding: 16, backgroundColor: colors.background }, card: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, borderRadius: 16, padding: 18 }, label: { color: colors.muted, fontSize: 12, marginTop: 10 }, value: { color: colors.text, fontWeight: '900', fontSize: 17, marginTop: 2 }, api: { color: colors.greenDark, fontWeight: '700', marginTop: 3 }, logout: { marginTop: 16, borderRadius: 12, borderWidth: 1, borderColor: colors.danger, paddingVertical: 13, alignItems: 'center' }, logoutText: { color: colors.danger, fontWeight: '900' } });
