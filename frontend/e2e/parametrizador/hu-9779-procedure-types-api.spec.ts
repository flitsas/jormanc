import { expect, test } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import {
  loginAndGetToken,
  loginAsSuperAdmin,
  QA_TENANT_ADMIN,
} from '../fixtures/qa-auth.js';

const PT_BASE = `${API_BASE}/procedure-types`;
const PROCEDURES_BASE = `${API_BASE}/procedures`;
const COMPANIES_BASE = `${API_BASE}/admin/companies`;
const ACME_COMPANY_NIT = '900000001';

const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

function uniqueName(prefix: string) {
  return `${prefix} ${Date.now().toString().slice(-7)}`;
}

async function createProcedureType(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  name?: string,
) {
  const res = await request.post(`${PT_BASE}/`, {
    headers: auth(token),
    data: {
      name: name ?? uniqueName('E2E Traspaso'),
      family: 'traspasos',
      scope: 'global',
      vehicleQueryKey: 'placa',
    },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{ id: string; tenantId: string }>;
}

async function getAcmeCompanyId(
  request: import('@playwright/test').APIRequestContext,
  token: string,
) {
  const listRes = await request.get(`${COMPANIES_BASE}/index`, {
    headers: auth(token),
    params: { nit: ACME_COMPANY_NIT, page: '1', page_size: '10' },
  });
  expect(listRes.status()).toBe(200);
  const page = await listRes.json();
  const company = page.data.find(
    (c: { nit: string }) => c.nit === ACME_COMPANY_NIT,
  );
  expect(company).toBeTruthy();
  return company.id as string;
}

test.describe('HU #9779 — Parametrizador CRUD tipos de trámite', () => {
  test('QA_TC01_PARAMETRIZADOR_CONFIG - Creación de tipo de trámite con estructura anidada', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const typeName = uniqueName('Traspaso Simple');

    const created = await createProcedureType(request, token, typeName);

    const stepRes = await request.post(`${PT_BASE}/${created.id}/steps`, {
      headers: auth(token),
      data: { name: 'Datos del vehículo', stepType: 'form', orderIndex: 1 },
    });
    expect(stepRes.status()).toBe(201);
    const step = await stepRes.json();

    const sectionRes = await request.post(
      `${PT_BASE}/${created.id}/steps/${step.id}/sections`,
      {
        headers: auth(token),
        data: { slug: 'vehiculo', name: 'Vehículo', orderIndex: 1 },
      },
    );
    expect(sectionRes.status()).toBe(201);
    const section = await sectionRes.json();

    const fieldRes = await request.post(
      `${PT_BASE}/${created.id}/steps/${step.id}/sections/${section.id}/fields`,
      {
        headers: auth(token),
        data: {
          slug: 'placa',
          fieldType: 'text',
          isRequired: true,
          orderIndex: 1,
        },
      },
    );
    expect(fieldRes.status()).toBe(201);

    const getRes = await request.get(`${PT_BASE}/${created.id}`, {
      headers: auth(token),
    });
    expect(getRes.status()).toBe(200);
    const detail = await getRes.json();

    expect(detail.id).toBe(created.id);
    expect(detail.tenantId).toBe(created.tenantId);
    expect(detail.name).toBe(typeName);
    expect(detail.steps).toHaveLength(1);
    expect(detail.steps[0].name).toBe('Datos del vehículo');
    expect(detail.steps[0].sections).toHaveLength(1);
    expect(detail.steps[0].sections[0].slug).toBe('vehiculo');
    expect(detail.steps[0].sections[0].fields).toHaveLength(1);
    expect(detail.steps[0].sections[0].fields[0]).toMatchObject({
      slug: 'placa',
      fieldType: 'text',
      isRequired: true,
    });
  });

  test('QA_TC02_PARAMETRIZADOR_CONFIG - Campo tipo dropdown persiste config JSONB con opciones', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const created = await createProcedureType(request, token);

    const stepRes = await request.post(`${PT_BASE}/${created.id}/steps`, {
      headers: auth(token),
      data: { name: 'Formulario', stepType: 'form', orderIndex: 1 },
    });
    const step = await stepRes.json();

    const sectionRes = await request.post(
      `${PT_BASE}/${created.id}/steps/${step.id}/sections`,
      {
        headers: auth(token),
        data: { slug: 'opciones', name: 'Opciones', orderIndex: 1 },
      },
    );
    const section = await sectionRes.json();

    const fieldRes = await request.post(
      `${PT_BASE}/${created.id}/steps/${step.id}/sections/${section.id}/fields`,
      {
        headers: auth(token),
        data: {
          slug: 'tipo_doc',
          fieldType: 'dropdown',
          isRequired: true,
          orderIndex: 1,
          config: {
            options: [{ value: 'A', label: 'Opción A' }],
            allowMultiple: false,
          },
        },
      },
    );
    expect(fieldRes.status()).toBe(201);
    const field = await fieldRes.json();
    expect(field.config.options).toHaveLength(1);
    expect(field.config.options[0]).toEqual({ value: 'A', label: 'Opción A' });
    expect(field.config.allowMultiple).toBe(false);

    const getRes = await request.get(`${PT_BASE}/${created.id}`, {
      headers: auth(token),
    });
    const detail = await getRes.json();
    const dropdown = detail.steps[0].sections[0].fields[0];
    expect(dropdown.fieldType).toBe('dropdown');
    expect(dropdown.config.options[0].label).toBe('Opción A');
  });

  test('QA_TC03_PARAMETRIZADOR_CONFIG - Soft-delete bloqueado si hay trámites activos', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const created = await createProcedureType(request, superToken);
    const companyId = await getAcmeCompanyId(request, superToken);

    const procRes = await request.post(`${PROCEDURES_BASE}/`, {
      headers: auth(operadorToken),
      data: { procedureTypeId: created.id, companyId },
    });
    expect(procRes.status()).toBe(201);
    const procedure = await procRes.json();
    expect(procedure.status).toBe('draft');

    const deleteRes = await request.delete(`${PT_BASE}/${created.id}`, {
      headers: auth(superToken),
    });
    expect(deleteRes.status()).toBe(409);
    const body = await deleteRes.json();
    expect(body.code).toBe('PROCEDURE_TYPE_HAS_ACTIVE_PROCEDURES');

    const stillThere = await request.get(`${PT_BASE}/${created.id}`, {
      headers: auth(superToken),
    });
    expect(stillThere.status()).toBe(200);
  });

  test('QA_TC04_PARAMETRIZADOR_CONFIG - Caso Borde Multitenant', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);

    const res = await request.post(`${PT_BASE}/`, {
      headers: auth(token),
      data: {
        name: uniqueName('Sin permiso'),
        family: 'traspasos',
        scope: 'global',
        vehicleQueryKey: 'placa',
      },
    });

    expect(res.status()).toBe(403);
    const body = await res.json();
    expect(body.code).toBe('FORBIDDEN');
  });

  test('QA_TC05_PARAMETRIZADOR_CONFIG - Validacion Contrato API', async ({ request }) => {
    const token = await loginAsSuperAdmin(request);
    const typeName = uniqueName('Contrato API');
    const created = await createProcedureType(request, token, typeName);

    const getRes = await request.get(`${PT_BASE}/${created.id}`, {
      headers: auth(token),
    });
    expect(getRes.status()).toBe(200);
    const detail = await getRes.json();

    expect(detail).toMatchObject({
      id: created.id,
      tenantId: created.tenantId,
      name: typeName,
      family: 'traspasos',
      scope: 'global',
      vehicleQueryKey: 'placa',
      version: expect.any(Number),
      isActive: true,
    });
    expect(detail.slug).toMatch(/^[a-z0-9-]+$/);
    expect(detail.createdAt).toBeTruthy();
    expect(Array.isArray(detail.steps)).toBe(true);
    expect(Array.isArray(detail.apiConnectors)).toBe(true);
  });
});
