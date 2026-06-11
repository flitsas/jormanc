import type { APIRequestContext } from '@playwright/test';
import { API_BASE, QA_SEED } from './qa-seed.js';

export const QA_TENANT_ADMIN = {
  email: 'operador@acme.com',
  password: QA_SEED.password,
  tenantSlug: QA_SEED.tenantSlug,
} as const;

export async function loginAndGetToken(
  request: APIRequestContext,
  creds: { email: string; password: string; tenantSlug: string } = QA_SEED,
): Promise<string> {
  const res = await request.post(`${API_BASE}/auth/login`, {
    data: {
      email: creds.email,
      password: creds.password,
      tenantSlug: creds.tenantSlug,
    },
  });
  if (res.status() !== 200) {
    throw new Error(`Login failed (${res.status()}): ${await res.text()}`);
  }
  const body = await res.json();
  return body.accessToken as string;
}

export async function loginAsSuperAdmin(request: APIRequestContext): Promise<string> {
  return loginAndGetToken(request, QA_SEED);
}
