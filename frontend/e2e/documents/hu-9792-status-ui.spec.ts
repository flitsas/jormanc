import { expect, test } from '@playwright/test';
import {
  DOC_PROCEDURE_ID,
  mockAuthMe,
  mockConsolidatedPdfDownload,
  mockDocumentsStatus,
  mockProcedureDetailForDocuments,
  MOCK_DOCUMENTS_STATUS,
  seedOperadorSession,
} from './documents-ui-mocks.js';

test.describe('HU #9792 — Panel estado documental UI', () => {
  test('QA_TC01_DOCUMENTOS_PANEL - DocumentStatusPanel lista documentos con badges de estado', async ({
    page,
  }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);
    await mockProcedureDetailForDocuments(page);
    await mockDocumentsStatus(page);

    await page.goto(`/procedures/${DOC_PROCEDURE_ID}`);

    await expect(page.getByRole('heading', { name: /Estado documental/i })).toBeVisible();
    await expect(page.getByText('Contrato compraventa')).toBeVisible();
    await expect(page.getByText('Listo')).toBeVisible();
    await expect(page.getByText('Licencia de tránsito')).toBeVisible();
    await expect(page.getByText('Pendiente')).toBeVisible();
  });

  test('QA_TC02_DOCUMENTOS_PANEL - ConsolidatedPackageCard muestra versión y descarga', async ({
    page,
  }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);
    await mockProcedureDetailForDocuments(page);
    await mockDocumentsStatus(page);
    await mockConsolidatedPdfDownload(page);

    await page.goto(`/procedures/${DOC_PROCEDURE_ID}`);

    await expect(page.getByText(/Paquete consolidado/i)).toBeVisible();
    const consolidatedSection = page.locator('section[aria-labelledby="consolidated-package-title"]');
    await expect(consolidatedSection).toBeVisible();
    await expect(consolidatedSection).toContainText('Versión:');
    await expect(consolidatedSection).toContainText('Documentos:');
    await expect(
      page.getByText('TRAMITE_TRASP-01_ACM-0001_traspasos_20260611.pdf'),
    ).toBeVisible();

    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name: /Descargar PDF/i }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toContain('.pdf');
  });

  test('QA_TC03_DOCUMENTOS_PANEL - Estado vacío sin documentos configurados', async ({ page }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);
    await mockProcedureDetailForDocuments(page);
    await mockDocumentsStatus(page, {
      procedureId: DOC_PROCEDURE_ID,
      documents: [],
      consolidatedPackages: [],
    });

    await page.goto(`/procedures/${DOC_PROCEDURE_ID}`);

    await expect(
      page.getByText(/Sin documentos configurados para este trámite/i),
    ).toBeVisible();
    await expect(page.getByText(/Consolidación pendiente/i)).toBeVisible();
  });

  test('QA_TC04_DOCUMENTOS_PANEL - Error de carga muestra alerta y reintentar', async ({ page }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);
    await mockProcedureDetailForDocuments(page);

    await page.route(`**/api/v1/procedures/${DOC_PROCEDURE_ID}/documents`, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'INTERNAL_ERROR', message: 'Fallo simulado E2E' }),
      });
    });

    await page.goto(`/procedures/${DOC_PROCEDURE_ID}`);

    await expect(page.getByRole('alert')).toContainText(/No se pudo cargar el estado documental/i);
    await expect(page.getByRole('button', { name: /Reintentar/i })).toBeVisible();
  });

  test('QA_TC05_DOCUMENTOS_PANEL - Visor PDF abre modal con iframe', async ({ page }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);
    await mockProcedureDetailForDocuments(page);
    await mockDocumentsStatus(page);
    await mockConsolidatedPdfDownload(page);

    await page.goto(`/procedures/${DOC_PROCEDURE_ID}`);

    await page.getByRole('button', { name: /Ver PDF/i }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await expect(page.getByRole('heading', { name: /Visor —/i })).toBeVisible();
    await expect(page.locator('iframe[title*="PDF consolidado"]')).toBeVisible();
  });
});
