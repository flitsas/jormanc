import { expect, test } from '@playwright/test';
import {
  mockDashboardRoutes,
  mockTenantAdminAuthMe,
  seedTenantAdminSession,
  USER_CARLOS,
  USER_MARIA,
} from './dashboard-ui-mocks.js';

test.describe('HU #9797 — Dashboard top users y exports UI', () => {
  test('QA_TC01_DASHBOARD_TOPUSERS - TopUsersCard muestra ranking con barras', async ({
    page,
  }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockDashboardRoutes(page);

    await page.goto('/dashboard');

    await expect(page.getByLabel('Top 5 radicadores')).toBeVisible();
    const ranking = page.getByLabel('Ranking de radicadores');
    await expect(ranking.getByText('María Pérez')).toBeVisible();
    await expect(ranking.getByText('Carlos López')).toBeVisible();
    await expect(page.getByTestId(`progress-${USER_MARIA}`)).toBeVisible();
  });

  test('QA_TC02_DASHBOARD_TOPUSERS - Multiselección muestra badge de filtro activo', async ({
    page,
  }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockDashboardRoutes(page);

    await page.goto('/dashboard');

    await page.locator(`#radicador-filter-${USER_MARIA}`).check();
    await page.locator(`#radicador-filter-${USER_CARLOS}`).check();

    await expect(page.getByText('2 radicadores seleccionados')).toBeVisible();
  });

  test('QA_TC03_DASHBOARD_TOPUSERS - Exportar Excel inicia descarga', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockDashboardRoutes(page);

    await page.goto('/dashboard');

    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name: /Exportar Excel/i }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/i);
  });

  test('QA_TC04_DASHBOARD_TOPUSERS - Usuario sin trámites muestra EmptyUserCard', async ({
    page,
  }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);
    await mockDashboardRoutes(page);

    await page.goto('/dashboard');
    await expect(
      page.getByText(/Este usuario no ha radicado ningún trámite/i),
    ).toBeVisible();
  });

  test('QA_TC05_DASHBOARD_TOPUSERS - Error export Excel muestra toast', async ({ page }) => {
    await seedTenantAdminSession(page);
    await mockTenantAdminAuthMe(page);

    await page.route('**/api/v1/dashboard/summary**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          period: { from: '2026-01-01T00:00:00.000Z', to: '2026-06-30T23:59:59.999Z' },
          summary: {
            total: 1,
            byFamily: [
              {
                family: 'traspasos',
                count: 1,
                pct: 100,
                byStatus: { draft: 0, submitted: 1, approved: 0, rejected: 0 },
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
            byStatus: { draft: 0, submitted: 1, approved: 0, rejected: 0 },
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
    await page.route('**/api/v1/dashboard/export/excel**', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'INTERNAL_ERROR', message: 'Export failed' }),
      });
    });

    await page.goto('/dashboard');
    await page.getByRole('button', { name: /Exportar Excel/i }).click();
    await expect(page.getByText(/Error al exportar/i)).toBeVisible();
  });
});
