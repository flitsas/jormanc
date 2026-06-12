import { expect, test } from '@playwright/test';
import {
  buildProcedureDetail,
  mockAuthMe,
  mockProcedureDetailRoutes,
  mockStep,
  seedOperadorSession,
} from './procedures-ui-mocks.js';

const PROCEDURE_ID = '00000000-0000-0000-0000-000000000002';
const SNAPSHOT_ID = '00000000-0000-0000-0000-000000000051';

test.describe('HU #9788 — Grilla, banners y adjuntos', () => {
  test('QA_TC01_TRAMITES_GRID - ProceduresGrid filtra y pagina', async ({ page }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);

    const captured: string[] = [];

    await page.route('**/api/v1/procedures?**', async (route) => {
      const url = new URL(route.request().url());
      captured.push(url.searchParams.get('status') ?? '');
      captured.push(url.searchParams.get('fecha_from') ?? '');
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: [
            {
              id: PROCEDURE_ID,
              compositeId: 'TRASP-02_EVE-8841',
              status: 'submitted',
              procedureTypeId: '00000000-0000-0000-0000-000000000010',
              companyId: '00000000-0000-0000-0000-000000000020',
              createdAt: '2026-02-01T10:00:00Z',
            },
          ],
          total: 45,
          page: 1,
          pageSize: 20,
        }),
      });
    });

    await page.goto('/procedures');
    await expect(page.getByText('TRASP-02_EVE-8841')).toBeVisible();
    await expect(page.getByText(/Página 1 de 3 · 45 trámites/i)).toBeVisible();

    await page.getByLabel(/filtrar por estado/i).selectOption('submitted');
    await page.getByLabel(/filtrar desde fecha/i).fill('2026-01-01');

    await expect.poll(() => captured.includes('submitted')).toBe(true);
    await expect.poll(() => captured.includes('2026-01-01')).toBe(true);
  });

  test('QA_TC02_TRAMITES_GRID - VehicleWarningBanner hallazgos no bloqueantes', async ({
    page,
  }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);

    await mockProcedureDetailRoutes(
      page,
      PROCEDURE_ID,
      buildProcedureDetail(PROCEDURE_ID, SNAPSHOT_ID, 'placa', [
        mockStep('00000000-0000-0000-0000-000000000041', 1, 'Vehículo', 'vehicle'),
      ], { compositeId: 'TRASP-01_ACM-0002' }),
    );

    await page.route(`**/api/v1/procedures/${PROCEDURE_ID}/vehicle`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          procedureId: PROCEDURE_ID,
          status: 'draft',
          vehicle: { plate: 'AAA123', brand: 'TOYOTA', model: 'HILUX' },
          warnings: ['Multa SIMIT: $500.000', 'Restricción: PRENDA'],
        }),
      });
    });

    await page.goto(`/procedures/${PROCEDURE_ID}`);

    await page.getByLabel('Placa').fill('AAA123');
    await page.getByRole('button', { name: /consultar vehículo/i }).click();

    await expect(page.getByText('Hallazgos informativos')).toBeVisible();
    await expect(page.getByText('Multa SIMIT: $500.000')).toBeVisible();
    await expect(page.getByText('Restricción: PRENDA')).toBeVisible();
    await expect(page.getByRole('button', { name: /siguiente/i })).toBeEnabled();
  });

  test('QA_TC03_TRAMITES_GRID - AttachmentsPanel lista por etiqueta', async ({ page }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);

    let attachments = [
      {
        id: '00000000-0000-0000-0000-000000000061',
        fileName: 'licencia.pdf',
        labelSlug: 'licencia_transito',
        sizeBytes: 2048,
        uploadedAt: '2026-02-01T12:00:00Z',
      },
    ];

    await mockProcedureDetailRoutes(
      page,
      PROCEDURE_ID,
      buildProcedureDetail(
        PROCEDURE_ID,
        '00000000-0000-0000-0000-000000000052',
        'placa',
        [],
        { compositeId: 'TRASP-01_ACM-0003', status: 'submitted' },
      ),
    );

    await page.route(`**/api/v1/procedures/${PROCEDURE_ID}/attachments`, async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(attachments),
        });
        return;
      }
      if (route.request().method() === 'POST') {
        attachments = [
          ...attachments,
          {
            id: '00000000-0000-0000-0000-000000000062',
            fileName: 'licencia.pdf',
            labelSlug: 'licencia_transito',
            sizeBytes: 4096,
            uploadedAt: '2026-02-02T10:00:00Z',
          },
        ];
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            attachment_id: '00000000-0000-0000-0000-000000000062',
            file_name: 'licencia.pdf',
            label_slug: 'licencia_transito',
          }),
        });
        return;
      }
      await route.continue();
    });

    await page.goto(`/procedures/${PROCEDURE_ID}`);

    await expect(page.getByText('licencia.pdf')).toBeVisible();
    await expect(page.getByText(/licencia_transito/i)).toBeVisible();
  });

  test('QA_TC04_TRAMITES_GRID - Caso Borde listado vacío', async ({ page }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);

    await page.route('**/api/v1/procedures?**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [], total: 0, page: 1, pageSize: 20 }),
      });
    });

    await page.goto('/procedures');
    await expect(page.getByText(/no hay trámites en este rango/i)).toBeVisible();
  });

  test('QA_TC05_TRAMITES_GRID - Validacion Contrato UI paginación', async ({ page }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);

    await page.route('**/api/v1/procedures?**', async (route) => {
      const url = new URL(route.request().url());
      const pageNum = Number(url.searchParams.get('page') ?? '1');
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          data: Array.from({ length: 20 }, (_, i) => ({
            id: `00000000-0000-0000-0000-0000000000${String(i).padStart(2, '0')}`,
            compositeId: `TRASP-02_EVE-${8800 + i}`,
            status: 'submitted',
            procedureTypeId: '00000000-0000-0000-0000-000000000010',
            companyId: '00000000-0000-0000-0000-000000000020',
            createdAt: '2026-02-01T10:00:00Z',
          })),
          total: 45,
          page: pageNum,
          pageSize: 20,
        }),
      });
    });

    await page.goto('/procedures');
    await page.getByRole('button', { name: /página siguiente/i }).click();
    await expect(page.getByText(/Página 2 de 3/i)).toBeVisible();
  });
});
