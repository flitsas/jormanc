import { expect, test } from '@playwright/test';
import { loginAndGetToken, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import { auth } from '../procedures/procedures-helpers.js';
import {
  createOtOrganism,
  expectOtOrganismShape,
  OT_BASE,
  uniqueOtIds,
} from './ot-helpers.js';

test.describe('HU #9798 — OT CRUD API', () => {
  test('QA_TC01_OT_DASHBOARD - Crear OT con slug único por tenant', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { slug, name } = uniqueOtIds();

    const res = await request.post(`${OT_BASE}/`, {
      headers: auth(token),
      data: { slug, name },
    });
    expect(res.status()).toBe(201);
    const body = await res.json();
    expectOtOrganismShape(body);
    expect(body.slug).toBe(slug);
    expect(body.name).toBe(name);
    expect(body.mode).toBe('dashboard');
    expect(body.quipuxEnabled).toBe(false);
  });

  test('QA_TC02_OT_DASHBOARD - Switch a Modo QX persiste y es reversible', async ({
    request,
  }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);

    const toQx = await request.patch(`${OT_BASE}/${ot.id}/mode`, {
      headers: auth(token),
      data: { mode: 'qx' },
    });
    expect(toQx.status()).toBe(200);
    const qxBody = await toQx.json();
    expect(qxBody.mode).toBe('qx');
    expect(qxBody.quipuxEnabled).toBe(true);

    const toDashboard = await request.patch(`${OT_BASE}/${ot.id}/mode`, {
      headers: auth(token),
      data: { mode: 'dashboard' },
    });
    expect(toDashboard.status()).toBe(200);
    const dashBody = await toDashboard.json();
    expect(dashBody.mode).toBe('dashboard');
    expect(dashBody.quipuxEnabled).toBe(false);
  });

  test('QA_TC03_OT_DASHBOARD - RLS garantiza aislamiento de OTs entre tenants', async ({
    request,
  }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const foreignId = '00000000-0000-0000-0000-000000000099';

    const res = await request.get(`${OT_BASE}/${foreignId}`, {
      headers: auth(token),
    });
    expect(res.status()).toBe(404);
    expect((await res.json()).code).toBe('OT_NOT_FOUND');
  });

  test('QA_TC04_OT_DASHBOARD - Caso Borde Multitenant', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { slug, name } = uniqueOtIds();

    const first = await request.post(`${OT_BASE}/`, {
      headers: auth(token),
      data: { slug, name },
    });
    expect(first.status()).toBe(201);

    const duplicate = await request.post(`${OT_BASE}/`, {
      headers: auth(token),
      data: { slug, name: `${name} duplicado` },
    });
    expect(duplicate.status()).toBe(409);
    expect((await duplicate.json()).code).toBe('OT_SLUG_ALREADY_EXISTS');
  });

  test('QA_TC05_OT_DASHBOARD - Validacion Contrato API', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);

    const list = await request.get(`${OT_BASE}/`, { headers: auth(token) });
    expect(list.status()).toBe(200);
    const items = await list.json();
    expect(Array.isArray(items)).toBe(true);
    expect(items.some((o: { id: string }) => o.id === ot.id)).toBe(true);

    const get = await request.get(`${OT_BASE}/${ot.id}`, { headers: auth(token) });
    expect(get.status()).toBe(200);
    expectOtOrganismShape(await get.json());
  });
});
