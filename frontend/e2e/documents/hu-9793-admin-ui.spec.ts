import { expect, test } from '@playwright/test';
import path from 'node:path';
import {
  DOC_TYPE_ID,
  mockDocumentTemplates,
  mockSuperAdminAuthMe,
  seedSuperAdminSession,
} from './documents-ui-mocks.js';

test.describe('HU #9793 — Admin plantillas UI', () => {
  test('QA_TC01_DOCUMENTOS_ADMIN - TemplateUploadModal muestra marcadores detectados', async ({
    page,
  }) => {
    await seedSuperAdminSession(page);
    await mockSuperAdminAuthMe(page);
    await mockDocumentTemplates(page);

    await page.goto(`/admin/document-types/${DOC_TYPE_ID}/templates`);
    await page.getByRole('button', { name: /Subir nueva plantilla/i }).click();

    const htmlPath = path.join(
      process.cwd(),
      'e2e',
      'documents',
      'fixtures',
      'plantilla-marcadores.html',
    );
    await page.locator('input[type="file"]').setInputFiles(htmlPath);

    await expect(page.getByText('{{actor[vendedor].full_name}}')).toBeVisible();
    await expect(page.getByText('{{vehicle.plate}}')).toBeVisible();
    await expect(page.getByRole('button', { name: /Confirmar/i })).toBeEnabled();
  });

  test('QA_TC02_DOCUMENTOS_ADMIN - TemplateVersionsList badges Activa y Deprecada', async ({
    page,
  }) => {
    await seedSuperAdminSession(page);
    await mockSuperAdminAuthMe(page);
    await mockDocumentTemplates(page);

    await page.goto(`/admin/document-types/${DOC_TYPE_ID}/templates`);

    await expect(page.getByRole('cell', { name: 'v3' })).toBeVisible();
    await expect(page.getByText('Activa').first()).toBeVisible();
    await expect(page.locator('text=Deprecada')).toHaveCount(2);
    await expect(page.getByText('Versión vigente')).toBeVisible();
  });

  test('QA_TC03_DOCUMENTOS_ADMIN - HTML sin marcadores muestra advertencia y permite confirmar', async ({
    page,
  }) => {
    await seedSuperAdminSession(page);
    await mockSuperAdminAuthMe(page);
    await mockDocumentTemplates(page);

    await page.goto(`/admin/document-types/${DOC_TYPE_ID}/templates`);
    await page.getByRole('button', { name: /Subir nueva plantilla/i }).click();

    const htmlPath = path.join(process.cwd(), 'e2e', 'documents', 'fixtures', 'plantilla-vacia.html');
    await page.locator('input[type="file"]').setInputFiles(htmlPath);

    await expect(
      page.getByText(/La plantilla no contiene marcadores/i),
    ).toBeVisible();
    await expect(page.getByRole('button', { name: /Confirmar/i })).toBeEnabled();
  });

  test('QA_TC04_DOCUMENTOS_ADMIN - Selector valida UUID inválido', async ({ page }) => {
    await seedSuperAdminSession(page);
    await mockSuperAdminAuthMe(page);

    await page.goto('/admin/documents');
    await page.getByLabel(/ID del tipo de documento/i).fill('no-es-uuid');
    await page.getByRole('button', { name: /Gestionar plantillas/i }).click();

    await expect(page.getByRole('alert')).toContainText(/UUID válido/i);
  });

  test('QA_TC05_DOCUMENTOS_ADMIN - Navegación desde selector con UUID válido', async ({ page }) => {
    await seedSuperAdminSession(page);
    await mockSuperAdminAuthMe(page);
    await mockDocumentTemplates(page);

    await page.goto('/admin/documents');
    await page.getByLabel(/ID del tipo de documento/i).fill(DOC_TYPE_ID);
    await page.getByRole('button', { name: /Gestionar plantillas/i }).click();

    await expect(page).toHaveURL(new RegExp(`/admin/document-types/${DOC_TYPE_ID}/templates`));
    await expect(page.getByRole('heading', { name: /Plantillas del tipo de documento/i })).toBeVisible();
  });
});
