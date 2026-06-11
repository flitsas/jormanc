import { expect, test, type Page } from '@playwright/test';
import { QA_SEED } from '../fixtures/qa-seed.js';

const MOCK_SUPERADMIN = {
  id: '550e8400-e29b-41d4-a716-446655440000',
  email: QA_SEED.email,
  name: 'Super Admin',
  roles: ['superadmin', 'admin'],
  permissions: ['users.read'],
  tenant_id: '550e8400-e29b-41d4-a716-446655440001',
  tenant_name: 'Acme',
};

const MOCK_COMPANY = {
  id: '660e8400-e29b-41d4-a716-446655440010',
  tenantId: '660e8400-e29b-41d4-a716-446655440011',
  nit: '900123456',
  name: 'Empresa Demo E2E',
  status: 'active',
  tenantSlug: 'demo-e2e',
  createdAt: '2026-01-01T00:00:00Z',
};

const MOCK_USER_ID = '550e8400-e29b-41d4-a716-446655440099';

const MOCK_LOG = {
  id: '00000000-0000-0000-0000-000000000001',
  tenantId: MOCK_COMPANY.tenantId,
  connectorType: 'runt',
  operation: 'runt.vehicle.plate',
  provider: 'mock',
  requestPayload: '{"plate":"ABC123"}',
  responsePayload: '{"status":"ok","found":true}',
  httpStatus: 200,
  durationMs: 42,
  loggedAt: '2026-06-11T10:00:00Z',
};

function companyPage(total = 1) {
  return { data: [MOCK_COMPANY], total, page: 1, pageSize: 20 };
}

async function seedSuperAdminSession(page: Page) {
  await page.addInitScript((user) => {
    localStorage.setItem('flit_access_token', 'e2e-jwt-token');
    localStorage.setItem('flit_user', JSON.stringify(user));
  }, MOCK_SUPERADMIN);
}

async function mockAuthMe(page: Page) {
  await page.route('**/api/v1/auth/me', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: MOCK_SUPERADMIN.id,
        email: MOCK_SUPERADMIN.email,
        name: MOCK_SUPERADMIN.name,
        roles: MOCK_SUPERADMIN.roles,
        permissions: MOCK_SUPERADMIN.permissions,
        tenantId: MOCK_SUPERADMIN.tenant_id,
        tenantName: MOCK_SUPERADMIN.tenant_name,
      }),
    });
  });
}

async function mockCompaniesList(page: Page) {
  await page.route('**/api/v1/admin/companies/index**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(companyPage()),
    });
  });
}

async function openConfigDialog(page: Page) {
  await page.goto('/admin/companies');
  await page.getByRole('button', { name: 'Configurar' }).click();
  const dialog = page.getByRole('dialog').filter({ hasText: MOCK_COMPANY.name });
  await expect(dialog.getByRole('heading', { name: MOCK_COMPANY.name })).toBeVisible();
  return dialog;
}

test.describe('HU #9778 — Matriz firmas, excepciones y logs UI', () => {
  test('QA_TC01_COMPANIAS_COMPANIAS - SignatureMatrixEditor persiste cambios al guardar', async ({
    page,
  }) => {
    await seedSuperAdminSession(page);
    await mockAuthMe(page);
    await mockCompaniesList(page);

    let putBody: Record<string, unknown> | null = null;

    await page.route(
      `**/api/v1/admin/companies/${MOCK_COMPANY.id}/config/signature-matrix`,
      async (route) => {
        if (route.request().method() === 'GET') {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify([
              {
                id: 'aa0e8400-e29b-41d4-a716-446655440001',
                companyId: MOCK_COMPANY.id,
                actorRole: 'vendedor',
                signatureType: 'identidad_digital',
                isActive: true,
              },
            ]),
          });
          return;
        }
        if (route.request().method() === 'PUT') {
          putBody = route.request().postDataJSON() as Record<string, unknown>;
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify([
              {
                id: 'aa0e8400-e29b-41d4-a716-446655440001',
                companyId: MOCK_COMPANY.id,
                actorRole: 'vendedor',
                signatureType: 'firma_pantalla',
                isActive: true,
              },
              {
                id: 'aa0e8400-e29b-41d4-a716-446655440002',
                companyId: MOCK_COMPANY.id,
                actorRole: 'comprador',
                signatureType: 'identidad_digital',
                isActive: true,
              },
              {
                id: 'aa0e8400-e29b-41d4-a716-446655440003',
                companyId: MOCK_COMPANY.id,
                actorRole: 'representante_legal',
                signatureType: 'identidad_digital',
                isActive: true,
              },
            ]),
          });
          return;
        }
        await route.continue();
      },
    );

    const dialog = await openConfigDialog(page);
    await dialog.getByRole('tab', { name: 'Matriz de Firmas' }).click();
    await dialog.getByLabel('Tipo de firma para vendedor').selectOption('firma_pantalla');
    await dialog.getByRole('button', { name: 'Guardar matriz' }).click();

    await expect(page.getByText('Matriz de firmas actualizada')).toBeVisible();
    expect(putBody).toMatchObject({
      entries: expect.arrayContaining([
        expect.objectContaining({ actorRole: 'vendedor', signatureType: 'firma_pantalla' }),
      ]),
    });
    await expect(dialog.getByLabel('Tipo de firma para vendedor')).toHaveValue('firma_pantalla');
  });

  test('QA_TC02_COMPANIAS_COMPANIAS - IntegrationLogsTable muestra JSON colapsable por log', async ({
    page,
  }) => {
    await seedSuperAdminSession(page);
    await mockAuthMe(page);
    await mockCompaniesList(page);

    await page.route('**/api/v1/admin/integration-logs**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [MOCK_LOG],
          total: 1,
          page: 1,
          pageSize: 20,
        }),
      });
    });

    const dialog = await openConfigDialog(page);
    await dialog.getByRole('tab', { name: 'Logs RUNT' }).click();

    await expect(dialog.getByText('mock')).toBeVisible();
    await expect(dialog.getByText('200')).toBeVisible();
    await expect(dialog.getByText('42 ms')).toBeVisible();

    await dialog.getByRole('button', { name: /mock/i }).click();
    await expect(dialog.getByText(/"plate"/)).toBeVisible();
    await expect(dialog.getByText(/"status"/)).toBeVisible();
    await expect(dialog.getByText('Request')).toBeVisible();
    await expect(dialog.getByText('Response')).toBeVisible();
  });

  test('QA_TC03_COMPANIAS_COMPANIAS - UserExceptionsManager agrega y elimina usuarios de lista blanca', async ({
    page,
  }) => {
    await seedSuperAdminSession(page);
    await mockAuthMe(page);
    await mockCompaniesList(page);

    let exceptions: { id: string; companyId: string; userId: string; addedAt: string }[] = [];

    await page.route(
      `**/api/v1/admin/companies/${MOCK_COMPANY.id}/user-exceptions**`,
      async (route) => {
        const method = route.request().method();
        if (method === 'GET') {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify(exceptions),
          });
          return;
        }
        if (method === 'POST') {
          const body = route.request().postDataJSON() as { userId: string };
          const created = {
            id: 'bb0e8400-e29b-41d4-a716-446655440001',
            companyId: MOCK_COMPANY.id,
            userId: body.userId,
            addedAt: '2026-06-11T12:00:00Z',
          };
          exceptions = [created];
          await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(created) });
          return;
        }
        if (method === 'DELETE') {
          exceptions = [];
          await route.fulfill({ status: 204, body: '' });
          return;
        }
        await route.continue();
      },
    );

    const dialog = await openConfigDialog(page);
    await dialog.getByRole('tab', { name: 'Excepciones' }).click();
    await expect(dialog.getByText('No hay usuarios en la lista blanca.')).toBeVisible();

    await dialog.getByLabel('ID de usuario para lista blanca').fill(MOCK_USER_ID);
    await dialog.getByRole('button', { name: 'Agregar' }).click();
    await expect(page.getByText('Usuario agregado a lista blanca')).toBeVisible();
    await expect(dialog.getByText(MOCK_USER_ID)).toBeVisible();

    await dialog.getByRole('button', { name: 'Eliminar' }).click();
    await expect(page.getByText('Usuario eliminado de lista blanca')).toBeVisible();
    await expect(dialog.getByText('No hay usuarios en la lista blanca.')).toBeVisible();
  });

  test('QA_TC04_COMPANIAS_COMPANIAS - Caso Borde Multitenant', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('flit_access_token', 'e2e-tenant-token');
      localStorage.setItem(
        'flit_user',
        JSON.stringify({
          id: '550e8400-e29b-41d4-a716-446655440099',
          email: 'operador@acme.com',
          name: 'Operador',
          roles: ['admin'],
          permissions: [],
          tenant_id: '550e8400-e29b-41d4-a716-446655440001',
          tenant_name: 'Acme',
        }),
      );
    });

    await page.route('**/api/v1/admin/companies/index**', async (route) => {
      await route.fulfill({
        status: 403,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'FORBIDDEN', message: 'Se requiere superadmin' }),
      });
    });

    await page.goto('/admin/companies');
    await expect(page).toHaveURL(/\/login\?reason=forbidden/, { timeout: 15_000 });
  });

  test('QA_TC05_COMPANIAS_COMPANIAS - Validacion Contrato API', async ({ page }) => {
    await seedSuperAdminSession(page);
    await mockAuthMe(page);
    await mockCompaniesList(page);

    const captured: { signature?: unknown; exception?: unknown } = {};

    await page.route(
      `**/api/v1/admin/companies/${MOCK_COMPANY.id}/config/signature-matrix`,
      async (route) => {
        if (route.request().method() === 'GET') {
          await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
          return;
        }
        if (route.request().method() === 'PUT') {
          captured.signature = route.request().postDataJSON();
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify([
              {
                id: 'aa0e8400-e29b-41d4-a716-446655440001',
                companyId: MOCK_COMPANY.id,
                actorRole: 'vendedor',
                signatureType: 'preasignada',
                isActive: true,
              },
            ]),
          });
          return;
        }
        await route.continue();
      },
    );

    await page.route(
      `**/api/v1/admin/companies/${MOCK_COMPANY.id}/user-exceptions**`,
      async (route) => {
        if (route.request().method() === 'GET') {
          await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
          return;
        }
        if (route.request().method() === 'POST') {
          captured.exception = route.request().postDataJSON();
          await route.fulfill({
            status: 201,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 'bb0e8400-e29b-41d4-a716-446655440001',
              companyId: MOCK_COMPANY.id,
              userId: MOCK_USER_ID,
              addedAt: '2026-06-11T12:00:00Z',
            }),
          });
          return;
        }
        await route.continue();
      },
    );

    await page.route('**/api/v1/admin/integration-logs**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [MOCK_LOG], total: 1, page: 1, pageSize: 20 }),
      });
    });

    const dialog = await openConfigDialog(page);

    await dialog.getByRole('tab', { name: 'Matriz de Firmas' }).click();
    await dialog.getByLabel('Tipo de firma para vendedor').selectOption('preasignada');
    await dialog.getByRole('button', { name: 'Guardar matriz' }).click();

    expect(captured.signature).toMatchObject({
      entries: expect.arrayContaining([
        expect.objectContaining({
          actorRole: expect.any(String),
          signatureType: 'preasignada',
          isActive: true,
        }),
      ]),
    });

    await dialog.getByRole('tab', { name: 'Excepciones' }).click();
    await dialog.getByLabel('ID de usuario para lista blanca').fill(MOCK_USER_ID);
    await dialog.getByRole('button', { name: 'Agregar' }).click();
    expect(captured.exception).toMatchObject({ userId: MOCK_USER_ID });

    await dialog.getByRole('tab', { name: 'Logs RUNT' }).click();
    await expect(dialog.getByText('mock')).toBeVisible();
    await expect(dialog.getByText('42 ms')).toBeVisible();
  });
});
