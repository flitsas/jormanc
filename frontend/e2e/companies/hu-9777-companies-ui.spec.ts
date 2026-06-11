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

function companyPage(total: number, data = [MOCK_COMPANY]) {
  return {
    data,
    total,
    page: 1,
    pageSize: 20,
  };
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

test.describe('HU #9777 — Consola Compañías UI', () => {
  test('QA_TC01_COMPANIAS_COMPANIAS - CompaniesTable con filtros reactivos y paginacion', async ({
    page,
  }) => {
    await seedSuperAdminSession(page);
    await mockAuthMe(page);

    const capturedParams: string[] = [];

    await page.route('**/api/v1/admin/companies/index**', async (route) => {
      const url = new URL(route.request().url());
      capturedParams.push(url.searchParams.get('nit') ?? '');
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(companyPage(25, Array.from({ length: 20 }, (_, i) => ({
          ...MOCK_COMPANY,
          id: `660e8400-e29b-41d4-a716-4466554400${String(i).padStart(2, '0')}`,
          nit: `900000${i}`,
          name: `Empresa ${i}`,
        })))),
      });
    });

    await page.goto('/admin/companies');
    await expect(page.getByRole('heading', { name: 'Compañías' })).toBeVisible();

    await page.getByLabel('Filtrar por NIT').fill('900123');
    await page.waitForTimeout(400);
    expect(capturedParams.some((p) => p === '900123')).toBe(true);

    await expect(page.getByText('Empresa 0')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Siguiente' })).toBeEnabled();
    await expect(page.getByText('Página 1 de 2')).toBeVisible();
  });

  test('QA_TC02_COMPANIAS_COMPANIAS - Formulario multi-pestaña guarda cada pestaña de formulario', async ({
    page,
  }) => {
    await seedSuperAdminSession(page);
    await mockAuthMe(page);

    const patchBodies: Record<string, unknown>[] = [];

    await page.route('**/api/v1/admin/companies/index**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(companyPage(1)),
      });
    });

    await page.route(`**/api/v1/admin/companies/${MOCK_COMPANY.id}/config`, async (route) => {
      if (route.request().method() === 'PATCH') {
        patchBodies.push(route.request().postDataJSON() as Record<string, unknown>);
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: '770e8400-e29b-41d4-a716-446655440020',
            companyId: MOCK_COMPANY.id,
            onlyOwnVehicles: false,
            baulFirmasEnabled: true,
            notificationTarget: 'admin',
            smtpMode: 'api_cliente',
          }),
        });
        return;
      }
      await route.continue();
    });

    await page.goto('/admin/companies');
    await page.getByRole('button', { name: 'Configurar' }).click();
    await expect(page.getByRole('heading', { name: MOCK_COMPANY.name })).toBeVisible();

    await page.getByLabel('Baúl de firmas habilitado').check();
    await page.getByRole('button', { name: 'Guardar' }).first().click();
    await expect(page.getByText('Configuración de empresa guardada')).toBeVisible();

    await page.getByRole('tab', { name: 'Notificaciones' }).click();
    const configDialog = page.getByRole('dialog').filter({ hasText: MOCK_COMPANY.name });
    const selects = configDialog.locator('select');
    await selects.nth(0).selectOption('all_users');
    await selects.nth(1).selectOption('smtp_flit');
    await configDialog.getByRole('button', { name: 'Guardar' }).click();
    await expect(page.getByText('Configuración de notificaciones guardada')).toBeVisible();

    expect(patchBodies.length).toBeGreaterThanOrEqual(2);
    expect(patchBodies[0]).toMatchObject({ baulFirmasEnabled: true });
    expect(patchBodies[1]).toMatchObject({
      notificationTarget: 'all_users',
      smtpMode: 'smtp_flit',
    });
  });

  test('QA_TC03_COMPANIAS_COMPANIAS - CompaniesTable muestra los 4 estados UI obligatorios', async ({
    page,
  }) => {
    await seedSuperAdminSession(page);
    await mockAuthMe(page);

    await page.route('**/api/v1/admin/companies/index**', async (route) => {
      await new Promise((r) => setTimeout(r, 1_200));
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(companyPage(0, [])),
      });
    });

    await page.goto('/admin/companies');
    await expect(page.getByLabel('Cargando compañías')).toBeVisible();
    await expect(page.getByText('No hay compañías registradas')).toBeVisible({ timeout: 15_000 });

    await page.unroute('**/api/v1/admin/companies/index**');
    await page.route('**/api/v1/admin/companies/index**', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Error interno simulado' }),
      });
    });
    await page.reload();
    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reintentar' })).toBeVisible();

    await page.unroute('**/api/v1/admin/companies/index**');
    await page.route('**/api/v1/admin/companies/index**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(companyPage(1)),
      });
    });
    await page.getByRole('button', { name: 'Reintentar' }).click();
    await expect(page.getByText(MOCK_COMPANY.name)).toBeVisible();
    await expect(page.getByText(MOCK_COMPANY.nit)).toBeVisible();
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

    await page.route('**/api/v1/admin/companies/index**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(companyPage(1)),
      });
    });

    await page.goto('/admin/companies');
    await page.getByRole('button', { name: 'Nueva compañía' }).click();
    const createDialog = page.getByRole('dialog');
    await expect(createDialog.getByRole('heading', { name: 'Nueva compañía' })).toBeVisible();

    await createDialog.getByRole('button', { name: 'Crear' }).click();
    await expect(createDialog).toContainText('NIT requerido');
    await expect(createDialog).toContainText('Nombre requerido');
    // Zod encadena min+regex: slug vacío muestra el mensaje del regex (último issue)
    await expect(createDialog).toContainText('Solo minúsculas, números y guiones');

    await createDialog.getByRole('textbox', { name: 'NIT' }).fill('900999888');
    await createDialog.getByRole('textbox', { name: 'Nombre' }).fill('Validación E2E');
    await createDialog.getByRole('textbox', { name: 'Slug tenant' }).fill('valid-e2e');

    await page.route('**/api/v1/admin/companies', async (route) => {
      if (route.request().method() === 'POST') {
        const body = route.request().postDataJSON() as Record<string, string>;
        expect(body).toMatchObject({
          nit: '900999888',
          name: 'Validación E2E',
          tenantSlug: 'valid-e2e',
        });
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: '880e8400-e29b-41d4-a716-446655440030',
            tenantId: '880e8400-e29b-41d4-a716-446655440031',
            tenantSlug: 'valid-e2e',
            nit: '900999888',
            name: 'Validación E2E',
            status: 'active',
            createdAt: '2026-06-11T00:00:00Z',
          }),
        });
        return;
      }
      await route.continue();
    });

    await createDialog.getByRole('button', { name: 'Crear' }).click();
    await expect(page.getByRole('heading', { name: 'Nueva compañía' })).toHaveCount(0);
  });
});
