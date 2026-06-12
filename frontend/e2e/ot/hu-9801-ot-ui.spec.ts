import { expect, test } from '@playwright/test';
import {
  MOCK_OT_ORGANISM,
  mockOtRoutes,
  mockTenantAdminAuthMe,
  seedTenantAdminSession,
} from './ot-ui-mocks.js';

test.describe('HU #9801 — OT gestión UI', () => {
  test('QA_TC01_OT_REGLAS - OtModeSwitch en Modo QX muestra confirmación', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockOtRoutes(page);

    await page.goto('/admin/ot');
    await page.getByRole('button', { name: 'Configurar' }).click();

    await page.getByRole('switch', { name: /Cambiar a Modo QX/i }).click();
    await expect(page.getByRole('alertdialog')).toContainText(/Activar Modo QX/i);
    await page.getByRole('button', { name: /Confirmar Modo QX/i }).click();
    await expect(page.getByRole('switch', { name: /Cambiar a Modo Dashboard/i })).toBeVisible();
  });

  test('QA_TC02_OT_REGLAS - QuipuxConfigForm guarda sin recargar página', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockOtRoutes(page, [{ ...MOCK_OT_ORGANISM, mode: 'qx', quipuxEnabled: true }]);

    await page.goto('/admin/ot');
    await page.getByRole('button', { name: 'Configurar' }).click();
    await page.getByRole('tab', { name: 'Quipux', exact: true }).click();

    await page.getByLabel('Endpoint Quipux').fill('https://quipux.flit.dev/hook');
    await page.getByRole('button', { name: 'Guardar configuración' }).click();
    await expect(page.getByText('Configuración Quipux guardada')).toBeVisible();
  });

  test('QA_TC03_OT_REGLAS - OtIntegrationLogsTable muestra eventos expandibles', async ({
    page,
  }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockOtRoutes(page);

    await page.goto('/admin/ot');
    await page.getByRole('button', { name: 'Configurar' }).click();
    await page.getByRole('tab', { name: /Logs Quipux/i }).click();

    await expect(page.getByText('status_changed')).toBeVisible();
    await page.getByRole('button', { name: /status_changed — TRASP-01_ACM-0001/i }).click();
    await expect(page.getByText(/"processed": true/i)).toBeVisible();
  });

  test('QA_TC04_OT_REGLAS - Caso Borde lista vacía de OTs', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockOtRoutes(page, []);

    await page.goto('/admin/ot');
    await expect(page.getByText('No hay OTs configurados')).toBeVisible();
  });

  test('QA_TC05_OT_REGLAS - Validacion Contrato API — error con reintentar', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);

    await page.route('**/api/v1/ot-organisms**', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ code: 'INTERNAL_ERROR', message: 'Fallo simulado' }),
        });
        return;
      }
      await route.fallback();
    });

    await page.goto('/admin/ot');
    await expect(page.getByRole('alert')).toContainText(/No se pudo cargar el listado/i);
    await expect(page.getByRole('button', { name: /Reintentar/i })).toBeVisible();
  });
});
