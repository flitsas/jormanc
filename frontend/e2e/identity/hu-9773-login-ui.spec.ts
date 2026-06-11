import { expect, test } from '@playwright/test';
import { QA_SEED } from '../fixtures/qa-seed.js';

function mockAuthApi(page: import('@playwright/test').Page, passwordOk: boolean) {
  return page.route('**/api/v1/auth/login', async (route) => {
    const body = route.request().postDataJSON() as { password?: string };
    if (passwordOk && body.password === QA_SEED.password) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          access_token: 'e2e-jwt-token',
          expires_in: 900,
          user: {
            id: '550e8400-e29b-41d4-a716-446655440000',
            email: QA_SEED.email,
            name: 'Admin',
            roles: ['admin'],
            permissions: ['dashboard:read'],
            tenant_id: '550e8400-e29b-41d4-a716-446655440001',
            tenant_name: 'Acme',
          },
        }),
      });
      return;
    }
    await route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: JSON.stringify({ error: 'INVALID_CREDENTIALS' }),
    });
  });
}

test.describe('HU #9773 — Login UI', () => {
  test('QA_TC01_IDENTIDAD_AUTH - Login UI redirige al dashboard', async ({ page }) => {
    await mockAuthApi(page, true);
    await page.goto('/login');

    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
    await page.getByLabel(/Correo electrónico/i).fill(QA_SEED.email);
    await page.getByLabel(/Contraseña/i).fill(QA_SEED.password);
    await page.getByLabel(/Organización/i).fill(QA_SEED.tenantSlug);
    await page.getByRole('button', { name: 'Ingresar' }).click();

    await expect(page).toHaveURL(/\/dashboard/, { timeout: 30_000 });
    await expect(page.getByRole('heading', { name: /Dashboard|FLIT/i }).first()).toBeVisible();
  });

  test('QA_TC02_IDENTIDAD_AUTH - Login UI muestra error con password invalido', async ({
    page,
  }) => {
    await mockAuthApi(page, false);
    await page.goto('/login');
    await page.getByLabel(/Correo electrónico/i).fill(QA_SEED.email);
    await page.getByLabel(/Contraseña/i).fill('WrongPassword!');
    await page.getByLabel(/Organización/i).fill(QA_SEED.tenantSlug);
    await page.getByRole('button', { name: 'Ingresar' }).click();

    await expect(page.getByRole('alert')).toBeVisible({ timeout: 15_000 });
    await expect(page).toHaveURL(/\/login/);
  });
});
