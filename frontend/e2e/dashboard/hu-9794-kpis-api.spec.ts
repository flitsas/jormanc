import { expect, test } from '@playwright/test';
import { loginAndGetToken, loginAsSuperAdmin, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import { auth } from '../procedures/procedures-helpers.js';
import {
  createIsolatedTenant,
  DASHBOARD_BASE,
  expectSummaryShape,
  wideDateRange,
} from './dashboard-helpers.js';

test.describe('HU #9794 — Dashboard KPIs API', () => {
  test('QA_TC01_DASHBOARD_KPIS - GET summary retorna KPIs por familia y estado', async ({
    request,
  }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { from, to } = wideDateRange();

    const res = await request.get(`${DASHBOARD_BASE}/summary`, {
      headers: auth(token),
      params: { from, to },
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expectSummaryShape(body);

    const families = (body.summary.byFamily as Array<{ family: string }>).map((f) => f.family);
    expect(families).toEqual(
      expect.arrayContaining(['matricula_inicial', 'traspasos', 'otros']),
    );
  });

  test('QA_TC02_DASHBOARD_KPIS - Operador no puede especificar tenant_id ajeno', async ({
    request,
  }) => {
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const superToken = await loginAsSuperAdmin(request);
    const { tenantId } = await createIsolatedTenant(request, superToken);
    const { from, to } = wideDateRange();

    const res = await request.get(`${DASHBOARD_BASE}/summary`, {
      headers: auth(operadorToken),
      params: { from, to, tenant_id: tenantId },
    });
    expect(res.status()).toBe(403);
    expect((await res.json()).code).toBe('FORBIDDEN');
  });

  test('QA_TC03_DASHBOARD_KPIS - SuperAdmin puede consultar tenant_id específico', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const { tenantId } = await createIsolatedTenant(request, superToken);
    const { from, to } = wideDateRange();

    const res = await request.get(`${DASHBOARD_BASE}/summary`, {
      headers: auth(superToken),
      params: { from, to, tenant_id: tenantId },
    });
    expect(res.status()).toBe(200);
    expectSummaryShape(await res.json());
  });

  test('QA_TC04_DASHBOARD_KPIS - GET procedures paginado con filtro family', async ({
    request,
  }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { from, to } = wideDateRange();

    const res = await request.get(`${DASHBOARD_BASE}/procedures`, {
      headers: auth(token),
      params: { from, to, family: 'traspasos', page: '1', page_size: '10' },
    });
    expect(res.status()).toBe(200);
    const page = await res.json();
    expect(Array.isArray(page.data)).toBe(true);
    expect(page.page).toBe(1);
    expect(page.pageSize).toBe(10);
    expect(typeof page.total).toBe('number');
  });

  test('QA_TC05_DASHBOARD_KPIS - Validación from/to requeridos', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);

    const res = await request.get(`${DASHBOARD_BASE}/summary`, {
      headers: auth(token),
    });
    expect(res.status()).toBe(400);
    expect((await res.json()).code).toBe('VALIDATION_ERROR');
  });
});
