import { expect, test, type Page } from '@playwright/test';
import { QA_SEED } from '../fixtures/qa-seed.js';

const MOCK_USER = {
  id: '550e8400-e29b-41d4-a716-446655440000',
  email: QA_SEED.email,
  name: 'Admin',
  roles: ['admin'],
  permissions: ['users.read', 'dashboard:read'],
  tenant_id: '550e8400-e29b-41d4-a716-446655440001',
  tenant_name: 'Acme',
};

async function seedAuthenticatedSession(page: Page) {
  await page.addInitScript((user) => {
    localStorage.setItem('flit_access_token', 'e2e-jwt-token');
    localStorage.setItem('flit_user', JSON.stringify(user));
  }, MOCK_USER);
}

function mockLoginSuccess(page: Page) {
  return page.route('**/api/v1/auth/login', async (route) => {
    const body = route.request().postDataJSON() as { password?: string };
    if (body.password === QA_SEED.password) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          access_token: 'e2e-jwt-token',
          expires_in: 900,
          user: MOCK_USER,
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

test.describe('HU #9773 — Identidad UI extendida', () => {
  test('QA_TC02_IDENTIDAD_AUTH - Página de invitación maneja token inválido o expirado', async ({
    page,
  }) => {
    await page.route('**/api/v1/invitations/*/validate', async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'INVITATION_EXPIRED' }),
      });
    });

    await page.goto('/invite/token-expirado-e2e');
    await expect(
      page.getByRole('heading', { name: 'Invitación expirada o inválida' }),
    ).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Activar cuenta' })).toHaveCount(0);
  });

  test('QA_TC03_IDENTIDAD_AUTH - UsersTable exhibe los 4 estados UI obligatorios', async ({
    page,
  }) => {
    await seedAuthenticatedSession(page);

    await page.route('**/api/v1/users?**', async (route) => {
      await new Promise((r) => setTimeout(r, 1_500));
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], total: 0, page: 1, page_size: 20 }),
      });
    });
    await page.goto('/admin/users');
    await expect(page.getByLabel('Cargando usuarios')).toBeVisible();
    await expect(page.getByText('No hay usuarios en este tenant')).toBeVisible({
      timeout: 15_000,
    });

    await page.unroute('**/api/v1/users?**');
    await page.route('**/api/v1/users?**', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Error interno simulado' }),
      });
    });
    await page.reload();
    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reintentar' })).toBeVisible();

    await page.unroute('**/api/v1/users?**');
    await page.route('**/api/v1/users?**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: MOCK_USER.id,
              email: MOCK_USER.email,
              full_name: 'Ana García',
              status: 'active',
              must_reset_pwd: false,
              last_login_at: '2026-06-10T10:00:00Z',
              created_at: '2026-01-01T00:00:00Z',
              roles: [
                {
                  id: '660e8400-e29b-41d4-a716-446655440002',
                  name: 'Admin',
                  slug: 'admin',
                },
              ],
            },
          ],
          total: 1,
          page: 1,
          page_size: 20,
        }),
      });
    });
    await page.getByRole('button', { name: 'Reintentar' }).click();
    await expect(page.getByRole('table', { name: 'Lista de usuarios' })).toBeVisible();
    await expect(page.getByText('Ana García')).toBeVisible();
  });

  test('QA_TC04_IDENTIDAD_AUTH - Login exitoso redirige al dashboard y persiste token', async ({
    page,
  }) => {
    let authorizedRequest = false;

    await mockLoginSuccess(page);
    await page.route('**/api/v1/dashboard/**', async (route) => {
      const auth = route.request().headers()['authorization'] ?? '';
      if (auth.startsWith('Bearer ')) authorizedRequest = true;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          total: 0,
          by_family: [],
          items: [],
        }),
      });
    });

    await page.goto('/login');
    await page.getByLabel(/Correo electrónico/i).fill(QA_SEED.email);
    await page.getByLabel(/Contraseña/i).fill(QA_SEED.password);
    await page.getByLabel(/Organización/i).fill(QA_SEED.tenantSlug);
    await page.getByRole('button', { name: 'Ingresar' }).click();

    await expect(page).toHaveURL(/\/dashboard/, { timeout: 30_000 });

    const token = await page.evaluate(() => localStorage.getItem('flit_access_token'));
    expect(token).toBe('e2e-jwt-token');
    expect(authorizedRequest).toBe(true);
  });

  test('QA_TC05_IDENTIDAD_AUTH - UsersTable estado error muestra mensaje y Reintentar', async ({
    page,
  }) => {
    await seedAuthenticatedSession(page);

    await page.route('**/api/v1/users?**', async (route) => {
      await route.fulfill({
        status: 503,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Servicio no disponible' }),
      });
    });

    await page.goto('/admin/users');
    await expect(page.getByRole('alert')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText('No se pudo cargar el listado')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reintentar' })).toBeVisible();
  });
});
