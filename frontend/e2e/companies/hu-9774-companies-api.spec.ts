import { expect, test } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import {
  loginAndGetToken,
  loginAsSuperAdmin,
  QA_TENANT_ADMIN,
} from '../fixtures/qa-auth.js';

const COMPANIES_BASE = `${API_BASE}/admin/companies`;

function uniqueCompanyIds() {
  const suffix = Date.now().toString().slice(-7);
  return {
    nit: `900${suffix}`,
    name: `E2E Empresa ${suffix}`,
    tenantSlug: `e2e-co-${suffix}`,
  };
}

test.describe('HU #9774 — Compañías API (SuperAdmin)', () => {
  test('QA_TC01_COMPANIAS_COMPANIAS - Creacion de compania crea tenant asociado', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const { nit, name, tenantSlug } = uniqueCompanyIds();

    const res = await request.post(`${COMPANIES_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { nit, name, tenantSlug },
    });

    expect(res.status()).toBe(201);
    const body = await res.json();
    expect(body.id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
    expect(body.tenantId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
    expect(body.tenantSlug).toBe(tenantSlug);
    expect(body.nit).toBe(nit);
    expect(body.name).toBe(name);
    expect(body.status).toBe('active');
    expect(body.createdAt).toBeTruthy();
  });

  test('QA_TC02_COMPANIAS_COMPANIAS - Listado con filtros y paginacion server-side', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const { nit, name, tenantSlug } = uniqueCompanyIds();
    const nitPrefix = nit.slice(0, 6);

    await request.post(`${COMPANIES_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { nit, name, tenantSlug },
    });

    const res = await request.get(`${COMPANIES_BASE}/index`, {
      headers: { Authorization: `Bearer ${token}` },
      params: { nit: nitPrefix, page: '1', page_size: '20' },
    });

    expect(res.status()).toBe(200);
    const page = await res.json();
    expect(Array.isArray(page.data)).toBe(true);
    expect(page.data.length).toBeLessThanOrEqual(20);
    expect(page.page).toBe(1);
    expect(page.pageSize).toBe(20);
    expect(typeof page.total).toBe('number');
    expect(page.total).toBeGreaterThanOrEqual(1);
    for (const item of page.data) {
      expect(item.nit.startsWith(nitPrefix)).toBe(true);
    }
  });

  test('QA_TC03_COMPANIAS_COMPANIAS - NIT duplicado rechazado', async ({ request }) => {
    const token = await loginAsSuperAdmin(request);
    const { nit, name, tenantSlug } = uniqueCompanyIds();

    const first = await request.post(`${COMPANIES_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { nit, name, tenantSlug },
    });
    expect(first.status()).toBe(201);

    const duplicate = await request.post(`${COMPANIES_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        nit,
        name: `${name} Duplicada`,
        tenantSlug: `${tenantSlug}-dup`,
      },
    });

    expect(duplicate.status()).toBe(409);
    const body = await duplicate.json();
    expect(body.code).toBe('COMPANY_NIT_ALREADY_EXISTS');
  });

  test('QA_TC04_COMPANIAS_COMPANIAS - Caso Borde Multitenant', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { nit, name, tenantSlug } = uniqueCompanyIds();

    const res = await request.post(`${COMPANIES_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { nit, name, tenantSlug },
    });

    expect(res.status()).toBe(403);
    const body = await res.json();
    expect(body.code).toBe('FORBIDDEN');
  });

  test('QA_TC05_COMPANIAS_COMPANIAS - Validacion Contrato API', async ({ request }) => {
    const token = await loginAsSuperAdmin(request);
    const { nit, name, tenantSlug } = uniqueCompanyIds();

    const create = await request.post(`${COMPANIES_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { nit, name, tenantSlug },
    });
    expect(create.status()).toBe(201);
    const company = await create.json();

    const list = await request.get(`${COMPANIES_BASE}/index`, {
      headers: { Authorization: `Bearer ${token}` },
      params: { nit, page: '1', page_size: '10' },
    });
    expect(list.status()).toBe(200);
    const page = await list.json();

    expect(page).toMatchObject({
      total: expect.any(Number),
      page: 1,
      pageSize: 10,
    });
    expect(page.data.some((c: { id: string }) => c.id === company.id)).toBe(true);

    const item = page.data.find((c: { id: string }) => c.id === company.id);
    expect(item).toMatchObject({
      id: company.id,
      tenantId: company.tenantId,
      nit: company.nit,
      name: company.name,
      status: 'active',
      tenantSlug: company.tenantSlug,
    });
    expect(item.createdAt).toBeTruthy();
  });
});
