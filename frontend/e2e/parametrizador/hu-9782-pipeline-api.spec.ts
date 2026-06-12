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
) {
  const res = await request.post(`${PT_BASE}/`, {
    headers: auth(token),
    data: {
      name: uniqueName('E2E Pipeline'),
      family: 'traspasos',
      scope: 'global',
      vehicleQueryKey: 'placa',
    },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{ id: string; tenantId: string }>;
}

async function createStep(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  typeId: string,
  name: string,
  orderIndex: number,
) {
  const res = await request.post(`${PT_BASE}/${typeId}/steps`, {
    headers: auth(token),
    data: { name, stepType: 'form', orderIndex },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{ id: string; orderIndex: number; name: string }>;
}

async function createFieldFixture(
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
      data: { slug: 'datos', name: 'Datos', orderIndex: 1 },
    },
  );
  const section = await sectionRes.json();

  const fieldRes = await request.post(
    `${PT_BASE}/${typeId}/steps/${step.id}/sections/${section.id}/fields`,
    {
      headers: auth(token),
      data: {
        slug: 'tipo_doc',
        name: 'Tipo documento',
        fieldType: 'text',
        isRequired: true,
        orderIndex: 1,
      },
    },
  );
  expect(fieldRes.status()).toBe(201);
  const field = await fieldRes.json();

  return { stepId: step.id as string, sectionId: section.id as string, fieldId: field.id as string };
}

async function createApiConnector(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  typeId: string,
) {
  const res = await request.post(`${PT_BASE}/${typeId}/api-connectors`, {
    headers: auth(token),
    data: {
      name: 'Consulta RUNT',
      endpoint: '/runt/vehicle',
      httpVerb: 'GET',
      stepOrder: 2,
      paramBindings: {},
    },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{
    id: string;
    name: string;
    endpoint: string;
    httpVerb: string;
    stepOrder: number;
  }>;
}

test.describe('HU #9782 — Pipeline builder, FieldEditor y ApiConnectors (API)', () => {
  test('QA_TC01_PARAMETRIZADOR_CONFIG - PipelineBuilder reordena pasos vía PUT order_index', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);

    const steps = [];
    for (let i = 1; i <= 4; i++) {
      steps.push(await createStep(request, token, created.id, `Paso ${i}`, i));
    }

    const paso3 = steps[2];
    const reorderRes = await request.put(
      `${PT_BASE}/${created.id}/steps/${paso3.id}`,
      {
        headers: auth(token),
        data: { orderIndex: 1 },
      },
    );
    expect(reorderRes.status()).toBe(200);
    const updated = await reorderRes.json();
    expect(updated.orderIndex).toBe(1);

    const getRes = await request.get(`${PT_BASE}/${created.id}`, {
      headers: auth(token),
    });
    expect(getRes.status()).toBe(200);
    const detail = await getRes.json();

    expect(detail.steps).toHaveLength(4);
    expect(detail.steps[0].id).toBe(paso3.id);
    expect(detail.steps[0].name).toBe('Paso 3');
    expect(detail.steps.map((s: { orderIndex: number }) => s.orderIndex)).toEqual([
      1, 2, 3, 4,
    ]);
  });

  test('QA_TC02_PARAMETRIZADOR_CONFIG - FieldEditor actualiza campo a dropdown con opciones', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);
    const { stepId, sectionId, fieldId } = await createFieldFixture(
      request,
      token,
      created.id,
    );

    const putRes = await request.put(
      `${PT_BASE}/${created.id}/steps/${stepId}/sections/${sectionId}/fields/${fieldId}`,
      {
        headers: auth(token),
        data: {
          name: 'Tipo documento',
          fieldType: 'dropdown',
          isRequired: true,
          config: {
            options: [
              { value: 'cc', label: 'Cédula' },
              { value: 'ce', label: 'Cédula extranjería' },
            ],
            allowMultiple: false,
          },
        },
      },
    );
    expect(putRes.status()).toBe(200);
    const field = await putRes.json();
    expect(field.fieldType).toBe('dropdown');
    expect(field.config.options).toHaveLength(2);
    expect(field.config.options[0]).toEqual({ value: 'cc', label: 'Cédula' });

    const getRes = await request.get(`${PT_BASE}/${created.id}`, {
      headers: auth(token),
    });
    const detail = await getRes.json();
    const stored = detail.steps[0].sections[0].fields[0];
    expect(stored.fieldType).toBe('dropdown');
    expect(stored.config.options[1].label).toBe('Cédula extranjería');
  });

  test('QA_TC03_PARAMETRIZADOR_CONFIG - ApiConnectorsPanel actualiza param_bindings', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);
    const connector = await createApiConnector(request, token, created.id);

    const getRes = await request.get(`${PT_BASE}/${created.id}`, {
      headers: auth(token),
    });
    const detail = await getRes.json();
    expect(detail.apiConnectors).toHaveLength(1);
    expect(detail.apiConnectors[0]).toMatchObject({
      id: connector.id,
      name: 'Consulta RUNT',
      endpoint: '/runt/vehicle',
      httpVerb: 'GET',
      stepOrder: 2,
    });

    const putRes = await request.put(
      `${PT_BASE}/${created.id}/api-connectors/${connector.id}`,
      {
        headers: auth(token),
        data: { paramBindings: { placa: 'step_1.field_placa' } },
      },
    );
    expect(putRes.status()).toBe(200);
    const updated = await putRes.json();
    expect(updated.paramBindings).toEqual({ placa: 'step_1.field_placa' });
  });

  test('QA_TC04_PARAMETRIZADOR_CONFIG - Caso Borde Multitenant', async ({ request }) => {
    const adminToken = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, adminToken);
    const step = await createStep(request, adminToken, created.id, 'Paso 1', 1);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);

    const res = await request.put(`${PT_BASE}/${created.id}/steps/${step.id}`, {
      headers: auth(operadorToken),
      data: { orderIndex: 1 },
    });

    expect(res.status()).toBe(403);
    const body = await res.json();
    expect(body.code).toBe('FORBIDDEN');
  });

  test('QA_TC05_PARAMETRIZADOR_CONFIG - Validacion Contrato API', async ({ request }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);
    const connector = await createApiConnector(request, token, created.id);

    const putRes = await request.put(
      `${PT_BASE}/${created.id}/api-connectors/${connector.id}`,
      {
        headers: auth(token),
        data: { paramBindings: { placa: 'step_1.field_placa' } },
      },
    );
    expect(putRes.status()).toBe(200);
    const body = await putRes.json();

    expect(body).toMatchObject({
      id: connector.id,
      procedureTypeId: created.id,
      tenantId: expect.any(String),
      name: 'Consulta RUNT',
      endpoint: '/runt/vehicle',
      httpVerb: 'GET',
      stepOrder: 2,
      isActive: true,
    });
    expect(body.paramBindings).toEqual({ placa: 'step_1.field_placa' });
    expect(typeof body.responseMappings).toBe('object');
    expect(body.createdAt).toBeTruthy();
  });
});
