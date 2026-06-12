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

function uniqueCompanyIds() {
  const suffix = Date.now().toString().slice(-7);
  return {
    nit: `901${suffix}`,
    name: `E2E Otro Tenant ${suffix}`,
    tenantSlug: `e2e-otro-${suffix}`,
  };
}

async function createProcedureTypeWithVehicleStep(
  request: import('@playwright/test').APIRequestContext,
  token: string,
) {
  const typeRes = await request.post(`${PT_BASE}/`, {
    headers: auth(token),
    data: {
      name: uniqueName('E2E Traspaso'),
      family: 'traspasos',
      scope: 'global',
      vehicleQueryKey: 'placa',
    },
  });
  expect(typeRes.status()).toBe(201);
  const created = await typeRes.json();

  const stepRes = await request.post(`${PT_BASE}/${created.id}/steps`, {
    headers: auth(token),
    data: { name: 'Datos del vehículo', stepType: 'vehicle', orderIndex: 1 },
  });
  expect(stepRes.status()).toBe(201);

  return created as { id: string };
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

async function createDraftProcedure(
  request: import('@playwright/test').APIRequestContext,
  operadorToken: string,
  superToken: string,
) {
  const procedureType = await createProcedureTypeWithVehicleStep(
    request,
    superToken,
  );
  const companyId = await getAcmeCompanyId(request, superToken);

  const res = await request.post(`${PROCEDURES_BASE}/`, {
    headers: auth(operadorToken),
    data: { procedureTypeId: procedureType.id, companyId },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{
    id: string;
    compositeId: string;
    status: string;
    procedureTypeId: string;
    procedureTypeSnapshotId: string;
    companyId: string;
    steps: Array<{ name: string; stepType: string; orderIndex: number }>;
  }>;
}

test.describe('HU #9784 — Creación trámite draft y consulta vehículo RUNT', () => {
  test('QA_TC01_PROCEDURES_CREATION - POST crea trámite draft con composite_id y snapshot', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const companyId = await getAcmeCompanyId(request, superToken);
    const procedureType = await createProcedureTypeWithVehicleStep(
      request,
      superToken,
    );

    const firstRes = await request.post(`${PROCEDURES_BASE}/`, {
      headers: auth(operadorToken),
      data: { procedureTypeId: procedureType.id, companyId },
    });
    expect(firstRes.status()).toBe(201);
    const first = await firstRes.json();

    expect(first.status).toBe('draft');
    expect(first.compositeId).toMatch(/^TRASP-/);
    expect(first.procedureTypeId).toBe(procedureType.id);
    expect(first.procedureTypeSnapshotId).toBeTruthy();
    expect(first.companyId).toBe(companyId);
    expect(first.currentStepOrder).toBe(1);
    expect(Array.isArray(first.steps)).toBe(true);
    expect(first.steps.length).toBeGreaterThanOrEqual(1);
    expect(first.steps[0]).toMatchObject({
      name: 'Datos del vehículo',
      stepType: 'vehicle',
      orderIndex: 1,
    });

    const secondRes = await request.post(`${PROCEDURES_BASE}/`, {
      headers: auth(operadorToken),
      data: { procedureTypeId: procedureType.id, companyId },
    });
    expect(secondRes.status()).toBe(201);
    const second = await secondRes.json();

    expect(second.compositeId).not.toBe(first.compositeId);
    expect(second.procedureTypeSnapshotId).toBeTruthy();
  });

  test('QA_TC02_PROCEDURES_CREATION - PATCH vehicle retorna RUNT y warnings no bloquean draft', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const procedure = await createDraftProcedure(
      request,
      operadorToken,
      superToken,
    );

    const patchRes = await request.patch(
      `${PROCEDURES_BASE}/${procedure.id}/vehicle`,
      {
        headers: auth(operadorToken),
        data: { plate: 'AAA123' },
      },
    );
    expect(patchRes.status()).toBe(200);
    const capture = await patchRes.json();

    expect(capture.procedureId).toBe(procedure.id);
    expect(capture.status).toBe('draft');
    expect(capture.vehicle).toMatchObject({
      plate: 'AAA123',
      brand: expect.any(String),
      model: expect.any(String),
    });
    expect(Array.isArray(capture.warnings)).toBe(true);

    const detailRes = await request.get(`${PROCEDURES_BASE}/${procedure.id}`, {
      headers: auth(operadorToken),
    });
    expect(detailRes.status()).toBe(200);
    const detail = await detailRes.json();
    expect(detail.status).toBe('draft');
  });

  test('QA_TC03_PROCEDURES_CREATION - GET list filtra por tenant y status draft', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const procedure = await createDraftProcedure(
      request,
      operadorToken,
      superToken,
    );

    const listRes = await request.get(`${PROCEDURES_BASE}/`, {
      headers: auth(operadorToken),
      params: { status: 'draft', page: '1', page_size: '50' },
    });
    expect(listRes.status()).toBe(200);
    const page = await listRes.json();

    expect(page).toMatchObject({
      total: expect.any(Number),
      page: 1,
      pageSize: 50,
    });
    expect(Array.isArray(page.data)).toBe(true);
    expect(page.data.some((p: { id: string }) => p.id === procedure.id)).toBe(
      true,
    );

    const item = page.data.find(
      (p: { id: string }) => p.id === procedure.id,
    ) as {
      compositeId: string;
      status: string;
      procedureTypeId: string;
      companyId: string;
    };
    expect(item.compositeId).toBe(procedure.compositeId);
    expect(item.status).toBe('draft');
    expect(item.procedureTypeId).toBe(procedure.procedureTypeId);
    expect(item.companyId).toBe(procedure.companyId);
  });

  test('QA_TC04_PROCEDURES_CREATION - Caso Borde Multitenant compañía ajena', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const procedureType = await createProcedureTypeWithVehicleStep(
      request,
      superToken,
    );

    const { nit, name, tenantSlug } = uniqueCompanyIds();
    const otherCompanyRes = await request.post(`${COMPANIES_BASE}/`, {
      headers: auth(superToken),
      data: { nit, name, tenantSlug },
    });
    expect(otherCompanyRes.status()).toBe(201);
    const otherCompany = await otherCompanyRes.json();

    const blocked = await request.post(`${PROCEDURES_BASE}/`, {
      headers: auth(operadorToken),
      data: {
        procedureTypeId: procedureType.id,
        companyId: otherCompany.id,
      },
    });

    expect(blocked.status()).toBe(404);
    const body = await blocked.json();
    expect(body.code).toBe('COMPANY_NOT_FOUND');
  });

  test('QA_TC05_PROCEDURES_CREATION - Validacion Contrato API ProcedureDto', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const procedure = await createDraftProcedure(
      request,
      operadorToken,
      superToken,
    );

    const getRes = await request.get(`${PROCEDURES_BASE}/${procedure.id}`, {
      headers: auth(operadorToken),
    });
    expect(getRes.status()).toBe(200);
    const detail = await getRes.json();

    expect(detail).toMatchObject({
      id: procedure.id,
      compositeId: procedure.compositeId,
      status: 'draft',
      procedureTypeSnapshotId: procedure.procedureTypeSnapshotId,
      vehicleQueryKey: 'placa',
      currentStepOrder: 1,
    });
    expect(detail.snapshotConfig).toBeTruthy();
    expect(Array.isArray(detail.snapshotConfig.steps)).toBe(true);
    expect(detail.stepData).toBeTruthy();
  });
});
