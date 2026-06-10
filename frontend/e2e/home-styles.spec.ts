import { expect, test } from '@playwright/test';

const HOME_STYLE_STORAGE_KEY = 'flit-home-style';

test.describe('Estilos visuales — persistencia en localStorage', () => {
  test('initHomeStyleFromStorage aplica preset guardado al cargar /', async ({ page }) => {
    await page.goto('/');
    await page.evaluate(
      ([key, value]) => localStorage.setItem(key, value),
      [HOME_STYLE_STORAGE_KEY, 'corporate'],
    );
    await page.reload();

    await expect
      .poll(async () => page.evaluate(() => document.documentElement.dataset.homeStyle ?? ''))
      .toBe('corporate');
  });
});
