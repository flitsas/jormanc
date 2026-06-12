import { expect, test } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import {
  loginAndGetToken,
  loginAsSuperAdmin,
  QA_TENANT_ADMIN,
} from '../fixtures/qa-auth.js';

const PT_BASE = `${API_BASE}/procedure-types`;

const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

function uniqueName(prefix: string) {
  return `${prefix} ${Date.now().toString().slice(-7)}`;
}

async function createProcedureType(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  vehicleQueryKey = 'placa',
) {
  const res = await request.post(`${PT_BASE}/`, {
    headers: auth(token),
    data: {
      name: uniqueName('E2E Actores'),
      family: 'traspasos',
      scope: 'global',
      vehicleQueryKey,
    },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{ id: string; version: number; vehicleQueryKey: string }>;
}

const VERIFICATIONS = [
  { type: 'datos_persona', is_active: true, order: 1 },
  { type: 'simit', is_active: true, order: 2 },
];

test.describe('HU #9781 — Actores, query-rules y vehicle-query', () => {
  test('QA_TC01_PARAMETRIZADOR_CONFIG - Crear actor con naturaleza jurídica y FK a representante legal', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);

    const compradorRes = await request.post(`${PT_BASE}/${created.id}/actors`, {
      headers: auth(token),
      data: {
        role: 'comprador',
        allowedNature: 'juridica',
        minCount: 1,
        maxCount: 1,
        isRequired: true,
        orderIndex: 0,
      },
    });
    expect(compradorRes.status()).toBe(201);
    const comprador = await compradorRes.json();

    expect(comprador.role).toBe('comprador');
    expect(comprador.allowedNature).toBe('juridica');
    expect(comprador.procedureTypeId).toBe(created.id);

    const repRes = await request.post(`${PT_BASE}/${created.id}/actors`, {
      headers: auth(token),
      data: {
        role: 'representante_legal',
        allowedNature: 'natural',
        minCount: 1,
        maxCount: 1,
        isRequired: true,
        orderIndex: 1,
        legalRepActorId: comprador.id,
      },
    });
    expect(repRes.status()).toBe(201);
    const representante = await repRes.json();

    expect(representante.role).toBe('representante_legal');
    expect(representante.legalRepActorId).toBe(comprador.id);
  });

  test('QA_TC02_PARAMETRIZADOR_CONFIG - Regla de consulta JSONB con verificaciones ordenadas', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);

    const actorRes = await request.post(`${PT_BASE}/${created.id}/actors`, {
      headers: auth(token),
      data: {
        role: 'vendedor',
        allowedNature: 'natural',
        minCount: 1,
        maxCount: 1,
      },
    });
    expect(actorRes.status()).toBe(201);
    const vendedor = await actorRes.json();

    const ruleRes = await request.post(
      `${PT_BASE}/${created.id}/actors/${vendedor.id}/query-rules`,
      {
        headers: auth(token),
        data: {
          subjectType: 'persona_natural',
          entryKey: 'document_number',
          isBlocking: true,
          verifications: VERIFICATIONS,
        },
      },
    );
    expect(ruleRes.status()).toBe(201);
    const rule = await ruleRes.json();

    expect(rule.subjectType).toBe('persona_natural');
    expect(rule.entryKey).toBe('document_number');
    expect(rule.isBlocking).toBe(true);
    expect(rule.actorDefinitionId).toBe(vendedor.id);
    expect(Array.isArray(rule.verifications)).toBe(true);
    expect(rule.verifications).toHaveLength(2);
    expect(rule.verifications[0]).toMatchObject({
      type: 'datos_persona',
      is_active: true,
      order: 1,
    });
    expect(rule.verifications[1]).toMatchObject({
      type: 'simit',
      is_active: true,
      order: 2,
    });
  });

  test('QA_TC03_PARAMETRIZADOR_CONFIG - SetVehicleQueryKey incrementa versión del tipo de trámite', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token, 'placa');
    expect(created.version).toBe(1);
    expect(created.vehicleQueryKey).toBe('placa');

    const putRes = await request.put(`${PT_BASE}/${created.id}/vehicle-query`, {
      headers: auth(token),
      data: { queryKey: 'vin' },
    });
    expect(putRes.status()).toBe(200);
    const updated = await putRes.json();

    expect(updated.vehicleQueryKey).toBe('vin');
    expect(updated.version).toBe(2);

    const getRes = await request.get(`${PT_BASE}/${created.id}`, {
      headers: auth(token),
    });
    expect(getRes.status()).toBe(200);
    const fetched = await getRes.json();
    expect(fetched.vehicleQueryKey).toBe('vin');
    expect(fetched.version).toBe(2);
  });

  test('QA_TC04_PARAMETRIZADOR_CONFIG - Caso Borde Multitenant', async ({ request }) => {
    const adminToken = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, adminToken);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);

    const res = await request.post(`${PT_BASE}/${created.id}/actors`, {
      headers: auth(operadorToken),
      data: { role: 'vendedor', allowedNature: 'natural' },
    });

    expect(res.status()).toBe(403);
    const body = await res.json();
    expect(body.code).toBe('FORBIDDEN');
  });

  test('QA_TC05_PARAMETRIZADOR_CONFIG - Validacion Contrato API', async ({ request }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);

    const actorRes = await request.post(`${PT_BASE}/${created.id}/actors`, {
      headers: auth(token),
      data: {
        role: 'comprador',
        allowedNature: 'juridica',
        minCount: 1,
        maxCount: 1,
        isRequired: true,
        orderIndex: 0,
      },
    });
    expect(actorRes.status()).toBe(201);
    const actor = await actorRes.json();

    expect(actor).toMatchObject({
      id: expect.any(String),
      procedureTypeId: created.id,
      tenantId: expect.any(String),
      role: 'comprador',
      allowedNature: 'juridica',
      minCount: 1,
      maxCount: 1,
      isRequired: true,
      orderIndex: 0,
    });
    expect(actor.createdAt).toBeTruthy();

    const ruleRes = await request.post(
      `${PT_BASE}/${created.id}/actors/${actor.id}/query-rules`,
      {
        headers: auth(token),
        data: {
          subjectType: 'persona_natural',
          entryKey: 'document_number',
          isBlocking: true,
          verifications: VERIFICATIONS,
        },
      },
    );
    expect(ruleRes.status()).toBe(201);
    const rule = await ruleRes.json();

    expect(rule).toMatchObject({
      id: expect.any(String),
      actorDefinitionId: actor.id,
      tenantId: expect.any(String),
      subjectType: 'persona_natural',
      entryKey: 'document_number',
      isBlocking: true,
    });
    expect(Array.isArray(rule.verifications)).toBe(true);
    expect(rule.createdAt).toBeTruthy();
  });
});
