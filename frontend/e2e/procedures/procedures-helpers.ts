import { expect } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import { loginAndGetToken, loginAsSuperAdmin, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';

export const PT_BASE = `${API_BASE}/procedure-types`;
export const PROCEDURES_BASE = `${API_BASE}/procedures`;
export const COMPANIES_BASE = `${API_BASE}/admin/companies`;
export const ACME_COMPANY_NIT = '900000001';

export const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

export function uniqueName(prefix: string) {
  return `${prefix} ${Date.now().toString().slice(-7)}`;
}

export function uniqueCompanyIds() {
  const suffix = Date.now().toString().slice(-7);
  return {
    nit: `901${suffix}`,
    name: `E2E Otro Tenant ${suffix}`,
    tenantSlug: `e2e-otro-${suffix}`,
  };
}

export async function getAcmeCompanyId(
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

export async function createProcedureType(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  vehicleQueryKey = 'placa',
) {
  const res = await request.post(`${PT_BASE}/`, {
    headers: auth(token),
    data: {
      name: uniqueName('E2E Traspaso'),
      family: 'traspasos',
      scope: 'global',
      vehicleQueryKey,
    },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{ id: string }>;
}

export async function createDraftProcedure(
  request: import('@playwright/test').APIRequestContext,
  superToken: string,
  operadorToken?: string,
) {
  const opToken =
    operadorToken ?? (await loginAndGetToken(request, QA_TENANT_ADMIN));
  const procedureType = await createProcedureType(request, superToken);
  const companyId = await getAcmeCompanyId(request, superToken);

  const res = await request.post(`${PROCEDURES_BASE}/`, {
    headers: auth(opToken),
    data: { procedureTypeId: procedureType.id, companyId },
  });
  expect(res.status()).toBe(201);
  const procedure = await res.json();
  return { procedure, procedureType, companyId, operadorToken: opToken, superToken };
}

export async function seedActorDefinition(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  typeId: string,
  data: Record<string, unknown>,
) {
  const res = await request.post(`${PT_BASE}/${typeId}/actors`, {
    headers: auth(token),
    data,
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{ id: string; role: string }>;
}
