import type { Page } from '@playwright/test';

export const MOCK_TENANT_ADMIN = {
  id: '550e8400-e29b-41d4-a716-446655440000',
  email: 'operador@acme.com',
  name: 'Operador Tenant',
  roles: ['admin'],
  permissions: ['analytics.read', 'analytics.export', 'tramites.read'],
  tenant_id: '550e8400-e29b-41d4-a716-446655440001',
  tenant_name: 'Acme',
};

export const USER_MARIA = '660e8400-e29b-41d4-a716-446655440010';
export const USER_CARLOS = '660e8400-e29b-41d4-a716-446655440011';

export const MOCK_SUMMARY = {
  period: {
    from: '2026-01-01T00:00:00.000Z',
    to: '2026-06-30T23:59:59.999Z',
  },
  summary: {
    total: 505,
    byFamily: [
      {
        family: 'matricula_inicial',
        count: 120,
        pct: 23.76,
        byStatus: { draft: 10, submitted: 80, approved: 25, rejected: 5 },
      },
      {
        family: 'traspasos',
        count: 340,
        pct: 67.33,
        byStatus: { draft: 20, submitted: 200, approved: 100, rejected: 20 },
      },
      {
        family: 'otros',
        count: 45,
        pct: 8.91,
        byStatus: { draft: 5, submitted: 30, approved: 8, rejected: 2 },
      },
    ],
    byStatus: { draft: 35, submitted: 310, approved: 133, rejected: 27 },
  },
};

export const MOCK_TOP_USERS = {
  data: [
    {
      userId: USER_MARIA,
      fullName: 'María Pérez',
      count: 85,
      pctOfTotal: 16.83,
    },
    {
      userId: USER_CARLOS,
      fullName: 'Carlos López',
      count: 72,
      pctOfTotal: 14.26,
    },
    {
      userId: '660e8400-e29b-41d4-a716-446655440012',
      fullName: 'Ana Ruiz',
      count: 0,
      pctOfTotal: 0,
    },
  ],
};

export const MOCK_PROCEDURES_TRASPASOS = {
  data: [
    {
      id: 'TRASP-01_ACM-0042',
      submittedAt: '2026-03-15T10:30:00Z',
      status: 'submitted',
      plate: 'ABC123',
      ownerName: 'Juan Pérez',
      approvedAt: null,
      updatedAt: '2026-03-15T11:00:00Z',
    },
    {
      id: 'TRASP-01_ACM-0043',
      submittedAt: '2026-04-02T08:00:00Z',
      status: 'approved',
      plate: 'XYZ789',
      ownerName: 'María Gómez',
      approvedAt: '2026-04-05T14:00:00Z',
      updatedAt: '2026-04-05T14:00:00Z',
    },
  ],
  total: 2,
  page: 1,
  pageSize: 20,
};

export async function seedTenantAdminSession(page: Page) {
  await page.addInitScript((user) => {
    localStorage.setItem('flit_access_token', 'e2e-jwt-token');
    localStorage.setItem('flit_user', JSON.stringify(user));
  }, MOCK_TENANT_ADMIN);
}

export async function mockTenantAdminAuthMe(page: Page) {
  await page.route('**/api/v1/auth/me', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: MOCK_TENANT_ADMIN.id,
        email: MOCK_TENANT_ADMIN.email,
        name: MOCK_TENANT_ADMIN.name,
        roles: MOCK_TENANT_ADMIN.roles,
        permissions: MOCK_TENANT_ADMIN.permissions,
        tenantId: MOCK_TENANT_ADMIN.tenant_id,
        tenantName: MOCK_TENANT_ADMIN.tenant_name,
      }),
    });
  });
}

export async function mockDashboardRoutes(page: Page) {
  await page.route('**/api/v1/dashboard/summary**', async (route) => {
    if (route.request().method() !== 'GET') return route.fallback();
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_SUMMARY),
    });
  });

  await page.route('**/api/v1/dashboard/top-users**', async (route) => {
    if (route.request().method() !== 'GET') return route.fallback();
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_TOP_USERS),
    });
  });

  await page.route('**/api/v1/dashboard/procedures**', async (route) => {
    if (route.request().method() !== 'GET') return route.fallback();
    const url = new URL(route.request().url());
    const family = url.searchParams.get('family');
    if (family !== 'traspasos') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [], total: 0, page: 1, pageSize: 20 }),
      });
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_PROCEDURES_TRASPASOS),
    });
  });

  await page.route('**/api/v1/dashboard/export/excel**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      headers: { 'content-disposition': 'attachment; filename="dashboard-export.xlsx"' },
      body: Buffer.from('PK\x03\x04E2E mock xlsx'),
    });
  });

  await page.route('**/api/v1/dashboard/export/pdf**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/pdf',
      headers: { 'content-disposition': 'attachment; filename="resumen-ejecutivo.pdf"' },
      body: Buffer.from('%PDF-1.4 E2E mock pdf'),
    });
  });
}
