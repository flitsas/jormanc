import type { Page } from '@playwright/test';

export const MOCK_OT_ID = '770e8400-e29b-41d4-a716-446655440100';
export const MOCK_OT_SLUG = 'ot-acme-e2e';
export const MOCK_LABEL_ID = '770e8400-e29b-41d4-a716-446655440101';
export const MOCK_PROCEDURE_TYPE_ID = '00000000-0000-0000-0000-000000000060';
export const MOCK_DOC_TYPE_A = '00000000-0000-0000-0000-000000000061';
export const MOCK_DOC_TYPE_B = '00000000-0000-0000-0000-000000000062';

export const MOCK_TENANT_ADMIN = {
  id: '550e8400-e29b-41d4-a716-446655440000',
  email: 'operador@acme.com',
  name: 'Operador Tenant',
  roles: ['admin'],
  permissions: ['ot.read', 'ot.manage', 'tramites.read'],
  tenant_id: '550e8400-e29b-41d4-a716-446655440001',
  tenant_name: 'Acme',
};

export const MOCK_OT_ORGANISM = {
  id: MOCK_OT_ID,
  tenantId: MOCK_TENANT_ADMIN.tenant_id,
  slug: MOCK_OT_SLUG,
  name: 'OT Acme E2E',
  mode: 'dashboard' as const,
  quipuxEnabled: false,
  quipuxConfig: null,
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-01T00:00:00.000Z',
};

export const MOCK_DOCUMENT_ORDER = [
  {
    procedureTypeId: MOCK_PROCEDURE_TYPE_ID,
    orderedDocuments: [
      {
        orderIndex: 1,
        documentType: { id: MOCK_DOC_TYPE_A, name: 'Cédula vendedor' },
      },
      {
        orderIndex: 2,
        documentType: { id: MOCK_DOC_TYPE_B, name: 'Tarjeta de propiedad' },
      },
    ],
  },
];

export const MOCK_LABELS = [
  {
    id: MOCK_LABEL_ID,
    otId: MOCK_OT_ID,
    slug: 'original',
    displayName: 'Documento original',
    isActive: true,
    createdAt: '2026-01-01T00:00:00.000Z',
  },
];

export const MOCK_INTEGRATION_LOGS = {
  data: [
    {
      id: '880e8400-e29b-41d4-a716-446655440200',
      otId: MOCK_OT_ID,
      tenantId: MOCK_TENANT_ADMIN.tenant_id,
      eventType: 'status_changed',
      procedureRef: 'TRASP-01_ACM-0001',
      requestPayload: '{"event":"status_changed"}',
      responsePayload: '{"processed":true}',
      httpStatus: 200,
      durationMs: 42,
      loggedAt: '2026-03-01T12:00:00.000Z',
    },
  ],
  total: 1,
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

export async function mockOtRoutes(page: Page, organisms = [MOCK_OT_ORGANISM]) {
  await page.route('**/api/v1/ot-organisms**', async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (method === 'GET' && /\/ot-organisms\/?(\?|$)/.test(url)) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(organisms),
      });
      return;
    }

    if (method === 'GET' && url.includes('/integration-logs')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_INTEGRATION_LOGS),
      });
      return;
    }

    if (method === 'GET' && url.includes('/document-order')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_DOCUMENT_ORDER),
      });
      return;
    }

    if (method === 'GET' && url.includes('/labels') && !url.includes('/impact')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_LABELS),
      });
      return;
    }

    if (method === 'GET' && url.includes('/impact')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ impactCount: 3 }),
      });
      return;
    }

    if (
      method === 'GET' &&
      url.includes(`/ot-organisms/${MOCK_OT_ID}`) &&
      !url.includes('/integration-logs') &&
      !url.includes('/document-order') &&
      !url.includes('/labels')
    ) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(organisms[0]),
      });
      return;
    }

    if (method === 'PATCH' && url.includes('/mode')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...organisms[0], mode: 'qx', quipuxEnabled: true }),
      });
      return;
    }

    if (method === 'PUT' && url.includes('/quipux-config')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          ...organisms[0],
          quipuxEnabled: true,
          quipuxConfig: '{"endpoint":"https://quipux.example"}',
        }),
      });
      return;
    }

    if (method === 'PUT' && url.includes('/document-order/')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          procedureTypeId: MOCK_PROCEDURE_TYPE_ID,
          orderedDocuments: MOCK_DOCUMENT_ORDER[0]!.orderedDocuments,
          updated: true,
          message: 'Prelación actualizada',
        }),
      });
      return;
    }

    await route.fallback();
  });

  await page.route(`**/api/v1/procedure-types/${MOCK_PROCEDURE_TYPE_ID}**`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: MOCK_PROCEDURE_TYPE_ID,
        name: 'Traspaso E2E',
        family: 'traspasos',
      }),
    });
  });
}
