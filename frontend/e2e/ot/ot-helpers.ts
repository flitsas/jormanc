import { createHmac, createHash } from 'node:crypto';
import { expect } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import { auth } from '../procedures/procedures-helpers.js';

export const OT_BASE = `${API_BASE}/ot-organisms`;
export const QUIPUX_WEBHOOK_BASE = `${API_BASE}/ot/webhooks/quipux`;
export const WEBHOOK_TOKEN = 'E2EQuipuxToken123456';

export function uniqueOtIds() {
  const suffix = Date.now().toString().slice(-6);
  return {
    slug: `ot-e2e-${suffix}`,
    name: `OT E2E ${suffix}`,
  };
}

export function hashWebhookToken(token: string) {
  return createHash('sha256').update(token).digest('hex');
}

export function quipuxSignature(token: string, body: string) {
  return createHmac('sha256', token).update(body).digest('hex');
}

export async function createOtOrganism(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  options: { slug?: string; name?: string } = {},
) {
  const { slug, name } = options.slug
    ? { slug: options.slug, name: options.name ?? options.slug }
    : uniqueOtIds();

  const res = await request.post(`${OT_BASE}/`, {
    headers: auth(token),
    data: { slug, name },
  });
  expect(res.status()).toBe(201);
  const body = await res.json();
  return { ...body, slug, name } as {
    id: string;
    tenantId: string;
    slug: string;
    name: string;
    mode: string;
    quipuxEnabled: boolean;
  };
}

export async function enableQuipuxMode(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  otId: string,
  slug: string,
) {
  const modeRes = await request.patch(`${OT_BASE}/${otId}/mode`, {
    headers: auth(token),
    data: { mode: 'qx' },
  });
  expect(modeRes.status()).toBe(200);

  const configRes = await request.put(`${OT_BASE}/${otId}/quipux-config`, {
    headers: auth(token),
    data: { endpoint: 'https://quipux.example/webhook', webhookToken: WEBHOOK_TOKEN },
  });
  expect(configRes.status()).toBe(200);

  return { slug, token: WEBHOOK_TOKEN };
}

export function expectOtOrganismShape(body: Record<string, unknown>) {
  expect(body.id).toMatch(
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
  );
  expect(body.tenantId).toMatch(
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
  );
  expect(typeof body.slug).toBe('string');
  expect(typeof body.name).toBe('string');
  expect(['dashboard', 'qx']).toContain(body.mode);
  expect(typeof body.quipuxEnabled).toBe('boolean');
  expect(body.createdAt).toBeTruthy();
  expect(body.updatedAt).toBeTruthy();
}
