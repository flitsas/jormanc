import { expect, test } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import {
  loginAndGetToken,
  loginAsSuperAdmin,
  QA_TENANT_ADMIN,
} from '../fixtures/qa-auth.js';

const PT_BASE = `${API_BASE}/procedure-types`;

const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

const RULE_CONDITIONS = {
  operator: 'AND',
  nodes: [{ field: 'actor.nature', op: '==', value: 'juridica' }],
};

function uniqueName(prefix: string) {
  return `${prefix} ${Date.now().toString().slice(-7)}`;
}

function showRule(name: string) {
  return {
    name,
    conditions: RULE_CONDITIONS,
    actions: [{ type: 'show', target: 'field.documento_natural' }],
  };
}

function hideRule(name: string) {
  return {
    name,
    conditions: RULE_CONDITIONS,
    actions: [{ type: 'hide', target: 'field.documento_natural' }],
  };
}

async function createProcedureTypeWithForm(
  request: import('@playwright/test').APIRequestContext,
  token: string,
) {
  const createRes = await request.post(`${PT_BASE}/`, {
    headers: auth(token),
    data: {
      name: uniqueName('E2E Reglas'),
      family: 'traspasos',
      scope: 'global',
      vehicleQueryKey: 'placa',
    },
  });
  expect(createRes.status()).toBe(201);
  const created = await createRes.json();

  const stepRes = await request.post(`${PT_BASE}/${created.id}/steps`, {
    headers: auth(token),
    data: { name: 'Formulario', stepType: 'form', orderIndex: 1 },
  });
  const step = await stepRes.json();

  const sectionRes = await request.post(
    `${PT_BASE}/${created.id}/steps/${step.id}/sections`,
    {
      headers: auth(token),
      data: { slug: 'datos', name: 'Datos', orderIndex: 1 },
    },
  );
  const section = await sectionRes.json();

  await request.post(
    `${PT_BASE}/${created.id}/steps/${step.id}/sections/${section.id}/fields`,
    {
      headers: auth(token),
      data: {
        slug: 'documento_natural',
        fieldType: 'text',
        isRequired: false,
        orderIndex: 1,
      },
    },
  );

  return created as { id: string; version: number };
}

async function getProcedureTypeVersion(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  typeId: string,
) {
  const res = await request.get(`${PT_BASE}/${typeId}`, { headers: auth(token) });
  expect(res.status()).toBe(200);
  const body = await res.json();
  return body.version as number;
}

test.describe('HU #9780 — Motor de reglas y simulador de coherencia', () => {
  test('QA_TC01_PARAMETRIZADOR_CONFIG - Regla coherente se persiste correctamente', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureTypeWithForm(request, token);
    expect(created.version).toBe(1);

    const ruleRes = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(token),
      data: hideRule('Ocultar adjunto'),
    });
    expect(ruleRes.status()).toBe(201);
    const rule = await ruleRes.json();

    expect(rule.id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
    expect(rule.procedureTypeId).toBe(created.id);
    expect(rule.name).toBe('Ocultar adjunto');
    expect(rule.isActive).toBe(true);
    expect(rule.conditions.operator).toBe('AND');
    expect(rule.actions[0]).toMatchObject({
      type: 'hide',
      target: 'field.documento_natural',
    });
    expect(rule.createdAt).toBeTruthy();

    const versionAfter = await getProcedureTypeVersion(request, token, created.id);
    expect(versionAfter).toBe(2);
  });

  test('QA_TC02_PARAMETRIZADOR_CONFIG - Regla conflictiva rechazada con 409 y detalle', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureTypeWithForm(request, token);

    const first = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(token),
      data: showRule('Mostrar documento'),
    });
    expect(first.status()).toBe(201);

    const conflict = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(token),
      data: hideRule('Ocultar adjunto'),
    });
    expect(conflict.status()).toBe(409);
    const body = await conflict.json();
    expect(body.error).toBe('RULE_SET_CONFLICT');
    expect(Array.isArray(body.conflicts)).toBe(true);
    expect(body.conflicts.length).toBeGreaterThanOrEqual(1);
    expect(body.conflicts[0]).toMatchObject({
      rule1: 'Mostrar documento',
      rule2: 'Ocultar adjunto',
    });
    expect(body.conflicts[0].description).toContain('field.documento_natural');

    const versionAfter = await getProcedureTypeVersion(request, token, created.id);
    expect(versionAfter).toBe(2);
  });

  test('QA_TC03_PARAMETRIZADOR_CONFIG - Endpoint de simulación dry-run no persiste', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureTypeWithForm(request, token);

    const simulateRes = await request.post(
      `${PT_BASE}/${created.id}/rules/simulate`,
      {
        headers: auth(token),
        data: hideRule('Candidata simulación'),
      },
    );
    expect(simulateRes.status()).toBe(200);
    const simulation = await simulateRes.json();
    expect(simulation.isCoherent).toBe(true);
    expect(simulation.conflicts).toEqual([]);

    const versionAfterSimulate = await getProcedureTypeVersion(
      request,
      token,
      created.id,
    );
    expect(versionAfterSimulate).toBe(1);

    const createRes = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(token),
      data: hideRule('Ocultar adjunto'),
    });
    expect(createRes.status()).toBe(201);
    expect(await getProcedureTypeVersion(request, token, created.id)).toBe(2);
  });

  test('QA_TC04_PARAMETRIZADOR_CONFIG - Caso Borde Multitenant', async ({ request }) => {
    const adminToken = await loginAsSuperAdmin(request);
    const created = await createProcedureTypeWithForm(request, adminToken);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);

    const res = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(operadorToken),
      data: hideRule('Sin permiso'),
    });

    expect(res.status()).toBe(403);
    const body = await res.json();
    expect(body.code).toBe('FORBIDDEN');
  });

  test('QA_TC05_PARAMETRIZADOR_CONFIG - Validacion Contrato API', async ({ request }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureTypeWithForm(request, token);

    const ruleRes = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(token),
      data: hideRule('Contrato API'),
    });
    expect(ruleRes.status()).toBe(201);
    const rule = await ruleRes.json();

    expect(rule).toMatchObject({
      id: expect.any(String),
      procedureTypeId: created.id,
      tenantId: expect.any(String),
      name: 'Contrato API',
      isActive: true,
    });
    expect(rule.conditions).toMatchObject({ operator: 'AND' });
    expect(Array.isArray(rule.conditions.nodes)).toBe(true);
    expect(Array.isArray(rule.actions)).toBe(true);
    expect(rule.actions[0].type).toBe('hide');
    expect(rule.createdAt).toBeTruthy();
  });
});
