import React, { useEffect, useState } from 'react';
import { ActivityIndicator, Alert, KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { configureApi, login, ping } from '../services/api';
import { getBaseUrl } from '../services/sessionStore';
import { colors } from '../theme/colors';
import type { Session } from '../types/models';

type Props = { onLogin: (session: Session) => void };

export function LoginScreen({ onLogin }: Props) {
  const [baseUrl, setBaseUrl] = useState('');
  const [user, setUser] = useState('');
  const [password, setPassword] = useState('');
  const [establishment, setEstablishment] = useState('');
  const [loading, setLoading] = useState(false);
  const [testing, setTesting] = useState(false);

  useEffect(() => { getBaseUrl().then(setBaseUrl).catch(() => undefined); }, []);

  async function testConnection() {
    setTesting(true);
    try {
      await configureApi(baseUrl);
      await ping();
      Alert.alert('Conexão OK', 'API NAXAPA respondeu corretamente.');
    } catch (error) {
      Alert.alert('Falha na conexão', String(error));
    } finally { setTesting(false); }
  }

  async function submit() {
    if (!user.trim() || !password) {
      Alert.alert('Atenção', 'Informe usuário e senha.');
      return;
    }
    setLoading(true);
    try {
      await configureApi(baseUrl);
      const est = establishment.trim() ? Number(establishment) : undefined;
      const session = await login(user.trim(), password, Number.isFinite(est) ? est : undefined);
      onLogin(session);
    } catch (error) {
      Alert.alert('Não foi possível entrar', String(error));
    } finally { setLoading(false); }
  }

  return (
    <KeyboardAvoidingView style={styles.root} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
        <View style={styles.brandBox}>
          <Text style={styles.brand}>NAXAPA</Text>
          <Text style={styles.slogan}>Sua operação no ponto certo.</Text>
        </View>
        <View style={styles.card}>
          <Text style={styles.title}>Entrar</Text>
          <Text style={styles.help}>Use o mesmo usuário da Gestão Web.</Text>
          <Text style={styles.label}>API do NAXAPA</Text>
          <TextInput style={styles.input} autoCapitalize="none" autoCorrect={false} value={baseUrl} onChangeText={setBaseUrl} placeholder="http://192.168.0.15:5180" />
          <Pressable style={styles.secondaryButton} onPress={testConnection} disabled={testing}>
            {testing ? <ActivityIndicator color={colors.greenDark} /> : <Text style={styles.secondaryText}>Testar conexão</Text>}
          </Pressable>
          <Text style={styles.label}>Usuário ou e-mail</Text>
          <TextInput style={styles.input} autoCapitalize="none" value={user} onChangeText={setUser} placeholder="Seu usuário" />
          <Text style={styles.label}>Senha</Text>
          <TextInput style={styles.input} secureTextEntry value={password} onChangeText={setPassword} placeholder="Sua senha" />
          <Text style={styles.label}>ID do estabelecimento (opcional)</Text>
          <TextInput style={styles.input} keyboardType="number-pad" value={establishment} onChangeText={setEstablishment} placeholder="Ex.: 1" />
          <Pressable style={styles.primaryButton} onPress={submit} disabled={loading}>
            {loading ? <ActivityIndicator color="#fff" /> : <Text style={styles.primaryText}>Entrar no NAXAPA</Text>}
          </Pressable>
          <Text style={styles.tip}>No celular físico, use o IPv4 do seu computador. Não use localhost nem 10.0.2.2.</Text>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.background },
  content: { flexGrow: 1, justifyContent: 'center', padding: 22 },
  brandBox: { alignItems: 'center', marginBottom: 20 },
  brand: { fontSize: 36, fontWeight: '900', color: colors.greenDark, letterSpacing: 2 },
  slogan: { marginTop: 5, color: colors.muted },
  card: { backgroundColor: colors.surface, borderRadius: 22, padding: 20, borderWidth: 1, borderColor: colors.border },
  title: { fontSize: 24, fontWeight: '800', color: colors.text },
  help: { color: colors.muted, marginTop: 4, marginBottom: 18 },
  label: { fontWeight: '700', color: colors.text, marginTop: 12, marginBottom: 6 },
  input: { borderWidth: 1, borderColor: colors.border, borderRadius: 12, backgroundColor: '#fff', paddingHorizontal: 14, paddingVertical: 12, color: colors.text },
  primaryButton: { marginTop: 20, backgroundColor: colors.green, borderRadius: 12, paddingVertical: 14, alignItems: 'center' },
  primaryText: { color: '#fff', fontWeight: '800', fontSize: 16 },
  secondaryButton: { marginTop: 9, borderWidth: 1, borderColor: colors.green, borderRadius: 12, paddingVertical: 11, alignItems: 'center' },
  secondaryText: { color: colors.greenDark, fontWeight: '800' },
  tip: { marginTop: 14, color: colors.muted, fontSize: 12, lineHeight: 18 },
});
