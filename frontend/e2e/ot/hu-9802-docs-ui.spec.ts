import { expect, test } from '@playwright/test';
import {
  MOCK_LABELS,
  MOCK_OT_ID,
  MOCK_OT_ORGANISM,
  MOCK_OT_SLUG,
  mockOtRoutes,
  mockTenantAdminAuthMe,
  seedTenantAdminSession,
} from './ot-ui-mocks.js';

test.describe('HU #9802 — OT prelación y etiquetas UI', () => {
  test('QA_TC01_OT_DOCS - DocumentOrderEditor muestra documentos ordenados', async ({
    page,
  }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockOtRoutes(page);

    await page.goto('/admin/ot');
    await page.getByRole('button', { name: 'Configurar' }).click();
    await page.getByRole('tab', { name: /Prelación/i }).click();

    await expect(page.getByText('Cédula vendedor')).toBeVisible();
    await expect(page.getByText('Tarjeta de propiedad')).toBeVisible();
  });

  test('QA_TC02_OT_DOCS - DeleteLabelModal muestra conteo de impacto', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockOtRoutes(page);

    await page.goto('/admin/ot');
    await page.getByRole('button', { name: 'Configurar' }).click();
    await page.getByRole('tab', { name: /Prelación/i }).click();

    await page.getByRole('button', { name: /Eliminar etiqueta Documento original/i }).click();
    await expect(page.getByText(/3 adjuntos/i)).toBeVisible();
  });

  test('QA_TC03_OT_DOCS - OtLabelsManager exhibe estados vacío y lleno', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);

    await page.route('**/api/v1/ot-organisms**', async (route) => {
      const url = route.request().url();
      if (route.request().method() === 'GET' && url.endsWith('/labels')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([]),
        });
        return;
      }
      if (route.request().method() === 'GET' && url.includes(`/ot-organisms/${MOCK_OT_ID}`)) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(MOCK_OT_ORGANISM),
        });
        return;
      }
      if (route.request().method() === 'GET' && /\/ot-organisms\/?(\?|$)/.test(url)) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([MOCK_OT_ORGANISM]),
        });
        return;
      }
      await route.fallback();
    });

    await page.goto('/admin/ot');
    await page.getByRole('button', { name: 'Configurar' }).click();
    await page.getByRole('tab', { name: /Prelación/i }).click();
    await expect(page.getByText(/Sin etiquetas personalizadas/i)).toBeVisible();

    await page.unroute('**/api/v1/ot-organisms**');
    await mockOtRoutes(page);
    await page.reload();
    await page.getByRole('button', { name: 'Configurar' }).click();
    await page.getByRole('tab', { name: /Prelación/i }).click();
    await expect(page.getByText(MOCK_LABELS[0]!.displayName)).toBeVisible();
  });

  test('QA_TC04_OT_DOCS - Caso Borde Multitenant en UI', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockOtRoutes(page, [
      {
        ...MOCK_OT_ORGANISM,
        tenantId: '99999999-9999-9999-9999-999999999999',
        name: 'OT Otro Tenant',
        slug: 'ot-otro-tenant',
      },
    ]);

    await page.goto('/admin/ot');
    await expect(page.getByText('OT Otro Tenant')).toBeVisible();
    await expect(page.getByText('ot-otro-tenant')).toBeVisible();
  });

  test('QA_TC05_OT_DOCS - Validacion Contrato API en listado UI', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockOtRoutes(page);

    await page.goto('/admin/ot');
    await expect(page.getByRole('columnheader', { name: 'Slug' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: 'Modo' })).toBeVisible();
    await expect(page.getByText(MOCK_OT_SLUG)).toBeVisible();
    await expect(page.locator('#main-content').getByText('Dashboard', { exact: true })).toBeVisible();
  });
});
