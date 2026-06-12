import { expect, test } from '@playwright/test';
import {
  mockDashboardRoutes,
  mockTenantAdminAuthMe,
  seedTenantAdminSession,
} from './dashboard-ui-mocks.js';

test.describe('HU #9796 — Dashboard gráfico y detalle UI', () => {
  test('QA_TC01_DASHBOARD_CHART - FamilyPieChart muestra familias con leyenda', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockDashboardRoutes(page);

    await page.goto('/dashboard');

    await expect(page.getByRole('heading', { name: /^Dashboard$/i })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Trámites por familia' })).toBeVisible();

    const legend = page.getByLabel('Leyenda del gráfico');
    await expect(legend.getByRole('button', { name: /Matrículas/i })).toBeVisible();
    await expect(legend.getByRole('button', { name: /Traspasos/i })).toBeVisible();
    await expect(legend.getByRole('button', { name: /Otros/i })).toBeVisible();
    await expect(page.getByRole('img', { name: /Gráfico circular/i })).toBeVisible();
  });

  test('QA_TC02_DASHBOARD_CHART - Clic en Traspasos abre tabla de detalle', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockDashboardRoutes(page);

    await page.goto('/dashboard');
    await page.getByLabel('Leyenda del gráfico').getByRole('button', { name: /Traspasos/i }).click();

    await expect(page.getByRole('heading', { name: /Detalle — Traspasos/i })).toBeVisible();
    await expect(page.getByText('TRASP-01_ACM-0042')).toBeVisible();
    await expect(page.getByText('ABC123')).toBeVisible();
  });

  test('QA_TC03_DASHBOARD_CHART - DateRangeFilter cambia rango y dispara recarga', async ({
    page,
  }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);

    let summaryCalls = 0;
    await page.route('**/api/v1/dashboard/summary**', async (route) => {
      summaryCalls += 1;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          period: { from: '2026-01-01T00:00:00.000Z', to: '2026-01-31T23:59:59.999Z' },
          summary: {
            total: 10,
            byFamily: [
              {
                family: 'traspasos',
                count: 10,
                pct: 100,
                byStatus: { draft: 0, submitted: 10, approved: 0, rejected: 0 },
              },
              {
                family: 'matricula_inicial',
                count: 0,
                pct: 0,
                byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
              },
              {
                family: 'otros',
                count: 0,
                pct: 0,
                byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
              },
            ],
            byStatus: { draft: 0, submitted: 10, approved: 0, rejected: 0 },
          },
        }),
      });
    });
    await page.route('**/api/v1/dashboard/top-users**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      });
    });

    await page.goto('/dashboard');
    await expect.poll(() => summaryCalls).toBeGreaterThanOrEqual(1);

    await page.getByLabel('Fecha final del período').fill('2026-01-31');
    await expect.poll(() => summaryCalls).toBeGreaterThanOrEqual(2);
  });

  test('QA_TC04_DASHBOARD_CHART - Estado vacío sin trámites en período', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);

    await page.route('**/api/v1/dashboard/summary**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          period: { from: '2026-01-01T00:00:00.000Z', to: '2026-06-30T23:59:59.999Z' },
          summary: {
            total: 0,
            byFamily: [
              {
                family: 'matricula_inicial',
                count: 0,
                pct: 0,
                byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
              },
              {
                family: 'traspasos',
                count: 0,
                pct: 0,
                byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
              },
              {
                family: 'otros',
                count: 0,
                pct: 0,
                byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
              },
            ],
            byStatus: { draft: 0, submitted: 0, approved: 0, rejected: 0 },
          },
        }),
      });
    });
    await page.route('**/api/v1/dashboard/top-users**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      });
    });

    await page.goto('/dashboard');
    await expect(page.getByText(/Sin trámites en el período/i)).toBeVisible();
  });

  test('QA_TC05_DASHBOARD_CHART - Error en summary muestra reintentar', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);

    await page.route('**/api/v1/dashboard/summary**', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'INTERNAL_ERROR', message: 'Fallo simulado' }),
      });
    });
    await page.route('**/api/v1/dashboard/top-users**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] }),
      });
    });

    await page.goto('/dashboard');
    await expect(page.getByRole('alert')).toContainText(/No se pudo cargar el gráfico/i);
    await expect(page.getByRole('button', { name: /Reintentar/i })).toBeVisible();
  });
});
