import { expect, test } from '@playwright/test';

test.describe('Shell principal FLIT', () => {
  test('redirige a login cuando no hay sesion', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/login/, { timeout: 20_000 });
    await expect(page.getByRole('heading', { name: 'Iniciar sesión' })).toBeVisible();
  });

  test('muestra layout autenticado con token en localStorage', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('flit_access_token', 'e2e-mock-token');
      localStorage.setItem(
        'flit_user',
        JSON.stringify({
          id: '550e8400-e29b-41d4-a716-446655440000',
          email: 'admin@acme.com',
          name: 'Admin E2E',
          roles: ['admin'],
          permissions: ['dashboard:read'],
          tenant_id: '550e8400-e29b-41d4-a716-446655440001',
          tenant_name: 'Acme',
        }),
      );
    });

    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'FLIT', level: 1 })).toBeVisible({
      timeout: 20_000,
    });
    await expect(
      page.getByText(/Plataforma lista para desarrollar las nuevas funcionalidades/i),
    ).toBeVisible();
    await expect(page.getByRole('complementary')).toBeVisible();
  });
});
