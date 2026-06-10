import { expect, test } from '@playwright/test';

test.describe('Shell principal FLIT', () => {
  test('muestra layout y mensaje de bienvenida en /', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'FLIT', level: 1 })).toBeVisible({
      timeout: 20_000,
    });
    await expect(
      page.getByRole('main').getByText(/Plataforma lista para desarrollar/i),
    ).toBeVisible();
    await expect(page.getByRole('complementary')).toBeVisible();
  });
});
