import { expect, test } from '@playwright/test';
import { loginAndGetToken, loginAsSuperAdmin, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import { auth, createProcedureType, getAcmeCompanyId } from '../procedures/procedures-helpers.js';
import { PROCEDURES_BASE } from '../documents/documents-helpers.js';
import {
  createOtOrganism,
  enableQuipuxMode,
  OT_BASE,
  QUIPUX_WEBHOOK_BASE,
  quipuxSignature,
  WEBHOOK_TOKEN,
} from './ot-helpers.js';

test.describe('HU #9800 — Quipux webhook API', () => {
  test('QA_TC01_OT_QUIPUX - Webhook Quipux con HMAC válido actualiza estado', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);
    const { slug } = await enableQuipuxMode(request, token, ot.id, ot.slug);

    const companyId = await getAcmeCompanyId(request, superToken);
    const procedureType = await createProcedureType(request, superToken);
    const procRes = await request.post(`${PROCEDURES_BASE}/`, {
      headers: auth(token),
      data: { procedureTypeId: procedureType.id, companyId },
    });
    expect(procRes.status()).toBe(201);
    const proc = await procRes.json();

    const payload = JSON.stringify({
      event: 'status_changed',
      procedure_ref: proc.compositeId,
      new_status: 'approved',
    });
    const signature = quipuxSignature(WEBHOOK_TOKEN, payload);

    const res = await request.post(`${QUIPUX_WEBHOOK_BASE}/${slug}`, {
      headers: {
        'X-Quipux-Token': WEBHOOK_TOKEN,
        'X-Quipux-Signature': signature,
        'Content-Type': 'application/json',
      },
      data: payload,
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.processed).toBe(true);
    expect(body.eventType).toBe('status_changed');
  });

  test('QA_TC02_OT_QUIPUX - HMAC inválido rechazado sin procesar payload', async ({
    request,
  }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);
    const { slug } = await enableQuipuxMode(request, token, ot.id, ot.slug);
    const payload = JSON.stringify({ event: 'heartbeat' });

    const res = await request.post(`${QUIPUX_WEBHOOK_BASE}/${slug}`, {
      headers: {
        'X-Quipux-Token': WEBHOOK_TOKEN,
        'X-Quipux-Signature': 'deadbeef',
        'Content-Type': 'application/json',
      },
      data: payload,
    });
    expect(res.status()).toBe(401);
  });

  test('QA_TC03_OT_QUIPUX - Logs de integración Quipux son paginables', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);
    const { slug } = await enableQuipuxMode(request, token, ot.id, ot.slug);
    const payload = JSON.stringify({ event: 'heartbeat' });
    const signature = quipuxSignature(WEBHOOK_TOKEN, payload);

    await request.post(`${QUIPUX_WEBHOOK_BASE}/${slug}`, {
      headers: {
        'X-Quipux-Token': WEBHOOK_TOKEN,
        'X-Quipux-Signature': signature,
        'Content-Type': 'application/json',
      },
      data: payload,
    });

    const res = await request.get(`${OT_BASE}/${ot.id}/integration-logs`, {
      headers: auth(token),
      params: { page: '1', page_size: '10' },
    });
    expect(res.status()).toBe(200);
    const page = await res.json();
    expect(Array.isArray(page.data)).toBe(true);
    expect(page.page).toBe(1);
    expect(page.pageSize).toBe(10);
    expect(typeof page.total).toBe('number');
    expect(page.total).toBeGreaterThanOrEqual(1);
  });

  test('QA_TC04_OT_QUIPUX - Caso Borde Multitenant', async ({ request }) => {
    const payload = JSON.stringify({ event: 'heartbeat' });
    const res = await request.post(`${QUIPUX_WEBHOOK_BASE}/ot-inexistente-xyz`, {
      headers: {
        'X-Quipux-Token': WEBHOOK_TOKEN,
        'X-Quipux-Signature': quipuxSignature(WEBHOOK_TOKEN, payload),
        'Content-Type': 'application/json',
      },
      data: payload,
    });
    expect(res.status()).toBe(404);
  });

  test('QA_TC05_OT_QUIPUX - Validacion Contrato API', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);
    const { slug } = await enableQuipuxMode(request, token, ot.id, ot.slug);
    const payload = JSON.stringify({ event: 'ping' });
    const signature = quipuxSignature(WEBHOOK_TOKEN, payload);

    const res = await request.post(`${QUIPUX_WEBHOOK_BASE}/${slug}`, {
      headers: {
        'X-Quipux-Token': WEBHOOK_TOKEN,
        'X-Quipux-Signature': signature,
        'Content-Type': 'application/json',
      },
      data: payload,
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body).toMatchObject({
      eventType: 'ping',
      processed: true,
    });
  });
});
