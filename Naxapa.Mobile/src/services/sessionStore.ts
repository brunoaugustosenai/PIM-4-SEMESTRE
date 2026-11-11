import * as SecureStore from 'expo-secure-store';
import type { Session } from '../types/models';

const KEYS = {
  baseUrl: 'naxapa.api.baseUrl',
  session: 'naxapa.api.session',
};

const DEFAULT_BASE_URL = 'http://192.168.0.100:5180';

export const normalizeBaseUrl = (value: string) => value.trim().replace(/\/+$/, '');

export async function getBaseUrl(): Promise<string> {
  return (await SecureStore.getItemAsync(KEYS.baseUrl)) ?? DEFAULT_BASE_URL;
}

export async function setBaseUrl(value: string): Promise<void> {
  await SecureStore.setItemAsync(KEYS.baseUrl, normalizeBaseUrl(value));
}

export async function saveSession(session: Session): Promise<void> {
  await SecureStore.setItemAsync(KEYS.session, JSON.stringify(session));
}

export async function getSession(): Promise<Session | null> {
  const raw = await SecureStore.getItemAsync(KEYS.session);
  if (!raw) return null;
  try {
    const session = JSON.parse(raw) as Session;
    const expiresAt = Date.parse(session.expiresAtUtc);
    if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
      await clearSession();
      return null;
    }
    return session;
  } catch {
    await clearSession();
    return null;
  }
}

export async function clearSession(): Promise<void> {
  await SecureStore.deleteItemAsync(KEYS.session);
}
