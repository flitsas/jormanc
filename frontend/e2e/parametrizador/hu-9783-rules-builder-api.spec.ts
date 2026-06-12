import { expect, test } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import {
  loginAndGetToken,
  loginAsSuperAdmin,
  QA_TENANT_ADMIN,
} from '../fixtures/qa-auth.js';

const PT_BASE = `${API_BASE}/procedure-types`;

const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

const OR_CONDITIONS = {
  operator: 'OR',
  nodes: [
    { field: 'actor.nature', op: '==', value: 'juridica' },
    { field: 'vehicle.restrictions', op: 'contains', value: 'EMBARGO' },
  ],
};

const AND_CONDITIONS = {
  operator: 'AND',
  nodes: [{ field: 'actor.nature', op: '==', value: 'juridica' }],
};

const VERIFICATION_TYPES = [
  'datos_persona',
  'simit',
  'rues',
  'restricciones',
  'liveness',
] as const;

function uniqueName(prefix: string) {
  return `${prefix} ${Date.now().toString().slice(-7)}`;
}

async function createProcedureType(
  request: import('@playwright/test').APIRequestContext,
  token: string,
) {
  const res = await request.post(`${PT_BASE}/`, {
    headers: auth(token),
    data: {
      name: uniqueName('E2E Rules UI'),
      family: 'traspasos',
      scope: 'global',
      vehicleQueryKey: 'placa',
    },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{ id: string }>;
}

async function seedFirmaNaturalField(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  typeId: string,
) {
  const stepRes = await request.post(`${PT_BASE}/${typeId}/steps`, {
    headers: auth(token),
    data: { name: 'Formulario', stepType: 'form', orderIndex: 1 },
  });
  const step = await stepRes.json();

  const sectionRes = await request.post(
    `${PT_BASE}/${typeId}/steps/${step.id}/sections`,
    {
      headers: auth(token),
      data: { slug: 'firmas', name: 'Firmas', orderIndex: 1 },
    },
  );
  const section = await sectionRes.json();

  await request.post(
    `${PT_BASE}/${typeId}/steps/${step.id}/sections/${section.id}/fields`,
    {
      headers: auth(token),
      data: {
        slug: 'firma_natural',
        fieldType: 'text',
        isRequired: false,
        orderIndex: 1,
      },
    },
  );
}

function ruleWithConditions(
  name: string,
  conditions: typeof OR_CONDITIONS,
  actionType: 'show' | 'hide',
) {
  return {
    name,
    conditions,
    actions: [{ type: actionType, target: 'field.firma_natural' }],
  };
}

function allVerificationsWithLiveness() {
  return VERIFICATION_TYPES.map((type, index) => ({
    type,
    is_active: type === 'liveness' || type === 'datos_persona' || type === 'simit',
    order: index + 1,
  }));
}

test.describe('HU #9783 — Diseñador de reglas AND/OR, coherencia y query-rules', () => {
  test('QA_TC01_PARAMETRIZADOR_CONFIG - Regla con árbol OR persiste conditions JSON', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);
    await seedFirmaNaturalField(request, token, created.id);

    const ruleRes = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(token),
      data: ruleWithConditions('Regla OR embargo', OR_CONDITIONS, 'show'),
    });
    expect(ruleRes.status()).toBe(201);
    const rule = await ruleRes.json();

    expect(rule.conditions.operator).toBe('OR');
    expect(rule.conditions.nodes).toHaveLength(2);
    expect(rule.conditions.nodes[0]).toMatchObject({
      field: 'actor.nature',
      op: '==',
      value: 'juridica',
    });
    expect(rule.conditions.nodes[1]).toMatchObject({
      field: 'vehicle.restrictions',
      op: 'contains',
      value: 'EMBARGO',
    });
  });

  test('QA_TC02_PARAMETRIZADOR_CONFIG - Simulación detecta conflicto show/hide idéntico', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);
    await seedFirmaNaturalField(request, token, created.id);

    const showRes = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(token),
      data: ruleWithConditions('Mostrar firma', AND_CONDITIONS, 'show'),
    });
    expect(showRes.status()).toBe(201);

    const simulateRes = await request.post(
      `${PT_BASE}/${created.id}/rules/simulate`,
      {
        headers: auth(token),
        data: ruleWithConditions('Ocultar firma', AND_CONDITIONS, 'hide'),
      },
    );
    expect(simulateRes.status()).toBe(200);
    const simulation = await simulateRes.json();

    expect(simulation.isCoherent).toBe(false);
    expect(Array.isArray(simulation.conflicts)).toBe(true);
    expect(simulation.conflicts.length).toBeGreaterThanOrEqual(1);
    expect(simulation.conflicts[0].description).toContain('field.firma_natural');

    const blocked = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(token),
      data: ruleWithConditions('Ocultar firma', AND_CONDITIONS, 'hide'),
    });
    expect(blocked.status()).toBe(409);
    expect((await blocked.json()).error).toBe('RULE_SET_CONFLICT');
  });

  test('QA_TC03_PARAMETRIZADOR_CONFIG - Query-rule persiste liveness is_active en JSONB', async ({
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
        isRequired: true,
        orderIndex: 0,
      },
    });
    expect(actorRes.status()).toBe(201);
    const vendedor = await actorRes.json();

    const verifications = allVerificationsWithLiveness();
    const ruleRes = await request.post(
      `${PT_BASE}/${created.id}/actors/${vendedor.id}/query-rules`,
      {
        headers: auth(token),
        data: {
          subjectType: 'persona_natural',
          entryKey: 'document_number',
          isBlocking: true,
          verifications,
        },
      },
    );
    expect(ruleRes.status()).toBe(201);
    const rule = await ruleRes.json();

    expect(rule.verifications).toHaveLength(5);
    const types = rule.verifications.map((v: { type: string }) => v.type);
    expect(types).toEqual([...VERIFICATION_TYPES]);

    const liveness = rule.verifications.find(
      (v: { type: string }) => v.type === 'liveness',
    );
    expect(liveness).toMatchObject({ type: 'liveness', is_active: true, order: 5 });
  });

  test('QA_TC04_PARAMETRIZADOR_CONFIG - Caso Borde Multitenant', async ({ request }) => {
    const adminToken = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, adminToken);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);

    const res = await request.post(`${PT_BASE}/${created.id}/rules`, {
      headers: auth(operadorToken),
      data: ruleWithConditions('Sin permiso', OR_CONDITIONS, 'show'),
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
        role: 'vendedor',
        allowedNature: 'natural',
        minCount: 1,
        maxCount: 1,
      },
    });
    const vendedor = await actorRes.json();

    const ruleRes = await request.post(
      `${PT_BASE}/${created.id}/actors/${vendedor.id}/query-rules`,
      {
        headers: auth(token),
        data: {
          subjectType: 'persona_natural',
          entryKey: 'document_number',
          isBlocking: false,
          verifications: allVerificationsWithLiveness(),
        },
      },
    );
    expect(ruleRes.status()).toBe(201);
    const rule = await ruleRes.json();

    expect(rule).toMatchObject({
      id: expect.any(String),
      actorDefinitionId: vendedor.id,
      tenantId: expect.any(String),
      subjectType: 'persona_natural',
      entryKey: 'document_number',
      isBlocking: false,
    });
    expect(Array.isArray(rule.verifications)).toBe(true);
    expect(rule.verifications).toHaveLength(5);
    expect(rule.createdAt).toBeTruthy();
  });
});
