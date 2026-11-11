import type {
  CartItem,
  OrderSummary,
  PageResult,
  Product,
  ProductDetail,
  Session,
} from '../types/models';
import { clearSession, getBaseUrl, getSession, saveSession, setBaseUrl } from './sessionStore';

export class ApiError extends Error {
  constructor(message: string, public readonly status?: number) {
    super(message);
    this.name = 'ApiError';
  }
}

const TIMEOUT_MS = 20000;

async function request<T>(
  path: string,
  init: RequestInit = {},
  auth = true,
): Promise<T> {
  const baseUrl = await getBaseUrl();
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), TIMEOUT_MS);

  try {
    const headers: Record<string, string> = {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      ...(init.headers as Record<string, string> | undefined),
    };

    if (auth) {
      const session = await getSession();
      if (!session) throw new ApiError('Sua sessão expirou. Entre novamente.', 401);
      headers.Authorization = `Bearer ${session.token}`;
    }

    const response = await fetch(`${baseUrl}${path}`, {
      ...init,
      headers,
      signal: controller.signal,
    });

    if (response.status === 401) {
      await clearSession();
      throw new ApiError('Sua sessão expirou. Entre novamente.', 401);
    }

    const text = await response.text();
    if (!response.ok) {
      let message = `Falha HTTP ${response.status}`;
      if (text) {
        try {
          const payload = JSON.parse(text) as { detail?: string; title?: string };
          message = payload.detail ?? payload.title ?? message;
        } catch {
          message = text;
        }
      }
      throw new ApiError(message, response.status);
    }

    return (text ? JSON.parse(text) : undefined) as T;
  } catch (error) {
    if (error instanceof ApiError) throw error;
    if (error instanceof Error && error.name === 'AbortError') {
      throw new ApiError('Tempo esgotado ao conectar com a API NAXAPA.');
    }
    throw new ApiError(`Não foi possível conectar com a API: ${String(error)}`);
  } finally {
    clearTimeout(timeout);
  }
}

export async function configureApi(baseUrl: string): Promise<void> {
  const normalized = baseUrl.trim().replace(/\/+$/, '');
  const valid = /^https?:\/\/[^/\s]+(?::\d+)?$/i.test(normalized);
  if (!valid) {
    throw new ApiError('Endereço inválido. Use, por exemplo: http://192.168.0.15:5180');
  }
  await setBaseUrl(normalized);
}

export async function ping(): Promise<void> {
  await request<unknown>('/health', { method: 'GET' }, false);
}

export async function login(
  loginValue: string,
  password: string,
  estabelecimentoId?: number,
): Promise<Session> {
  const session = await request<Session>(
    '/api/v1/auth/token',
    {
      method: 'POST',
      body: JSON.stringify({
        login: loginValue,
        password,
        estabelecimentoId: estabelecimentoId ?? null,
      }),
    },
    false,
  );
  await saveSession(session);
  return session;
}

export async function revoke(): Promise<void> {
  try {
    await request<void>('/api/v1/auth/revoke', { method: 'POST' }, true);
  } catch {
    // O logout local continua mesmo se a API estiver fora do ar.
  } finally {
    await clearSession();
  }
}

export async function products(query = ''): Promise<Product[]> {
  const params = new URLSearchParams({ q: query, page: '1', pageSize: '100' });
  const page = await request<PageResult<Product>>(`/api/v1/products?${params.toString()}`);
  return page.items ?? [];
}

export async function product(id: number): Promise<ProductDetail> {
  const detail = await request<ProductDetail>(`/api/v1/products/${id}`);
  return { ...detail, optionGroups: detail.optionGroups ?? [] };
}

export async function orders(): Promise<OrderSummary[]> {
  const page = await request<PageResult<OrderSummary>>('/api/v1/orders?page=1&pageSize=100');
  return page.items ?? [];
}

export async function sendCart(cart: CartItem[]): Promise<number> {
  if (!cart.length) throw new ApiError('Carrinho vazio.');

  const order = await request<OrderSummary>('/api/v1/orders', {
    method: 'POST',
    body: JSON.stringify({ serviceType: 'BALCAO', notes: null }),
  });

  for (const item of cart) {
    const createdItem = await request<{ pedidoItemId: number }>(`/api/v1/orders/${order.id}/items`, {
      method: 'POST',
      body: JSON.stringify({ productId: item.product.id, quantity: item.quantity }),
    });

    for (const option of item.options) {
      await request<void>(`/api/v1/orders/items/${createdItem.pedidoItemId}/options`, {
        method: 'POST',
        body: JSON.stringify({ optionId: option.id, quantity: 1 }),
      });
    }
  }

  await request<void>(`/api/v1/orders/${order.id}/status`, {
    method: 'POST',
    body: JSON.stringify({ status: 'PENDENTE' }),
  });
  return order.number;
}

export async function mediaUrl(value?: string | null): Promise<string | null> {
  if (!value?.trim()) return null;
  if (/^https?:\/\//i.test(value)) return value;
  const base = await getBaseUrl();
  return `${base}/${value.replace(/^\/+/, '')}`;
}
