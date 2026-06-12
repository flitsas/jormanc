import type { Page } from '@playwright/test';
import {
  buildProcedureDetail,
  mockAuthMe,
  mockStep,
  seedOperadorSession,
  STEPPER_PROCEDURE_ID,
  STEPPER_SNAPSHOT_ID,
} from '../procedures/procedures-ui-mocks.js';

export const DOC_PROCEDURE_ID = STEPPER_PROCEDURE_ID;
/** UUID v4 válido para el selector de DocumentAdminPage ([1-5] en tercer grupo). */
export const DOC_TYPE_ID = '550e8400-e29b-41d4-a716-446655440010';
export const TEMPLATE_V1_ID = '550e8400-e29b-41d4-a716-446655440021';
export const TEMPLATE_V2_ID = '550e8400-e29b-41d4-a716-446655440022';
export const TEMPLATE_V3_ID = '550e8400-e29b-41d4-a716-446655440023';

export const MOCK_DOCUMENTS_STATUS = {
  procedureId: DOC_PROCEDURE_ID,
  documents: [
    {
      id: '550e8400-e29b-41d4-a716-446655440031',
      documentType: {
        id: DOC_TYPE_ID,
        name: 'Contrato compraventa',
        loadType: 'generacion',
      },
      origin: 'generated',
      status: 'ready',
      templateVersion: 2,
      fileRef: 'procedures/acme/generated/contrato.pdf',
      generatedAt: '2026-06-11T12:00:00Z',
      uploadedBy: null,
      isRequired: true,
      orderIndex: 1,
    },
    {
      id: '550e8400-e29b-41d4-a716-446655440032',
      documentType: {
        id: '550e8400-e29b-41d4-a716-446655440011',
        name: 'Licencia de tránsito',
        loadType: 'carga',
      },
      origin: 'uploaded',
      status: 'pending',
      templateVersion: null,
      fileRef: null,
      generatedAt: null,
      uploadedBy: null,
      isRequired: true,
      orderIndex: 2,
    },
  ],
  consolidatedPackages: [
    {
      version: 2,
      mergedFileRef: 'procedures/acme/merged/v2.pdf',
      createdAt: '2026-06-11T14:30:00Z',
      downloadFilename: 'TRAMITE_TRASP-01_ACM-0001_traspasos_20260611.pdf',
      docCount: 2,
    },
    {
      version: 1,
      mergedFileRef: 'procedures/acme/merged/v1.pdf',
      createdAt: '2026-06-11T10:00:00Z',
      downloadFilename: 'TRAMITE_TRASP-01_ACM-0001_traspasos_20260611_v1.pdf',
      docCount: 1,
    },
  ],
};

export const MOCK_TEMPLATES = [
  {
    templateId: TEMPLATE_V3_ID,
    documentTypeId: DOC_TYPE_ID,
    version: 3,
    status: 'active',
    contentRef: 'templates/acme/v3.html',
    notes: 'Versión vigente',
    markersDetected: ['actor[vendedor].full_name', 'vehicle.plate'],
    createdAt: '2026-06-11T16:00:00Z',
  },
  {
    templateId: TEMPLATE_V2_ID,
    documentTypeId: DOC_TYPE_ID,
    version: 2,
    status: 'deprecated',
    contentRef: 'templates/acme/v2.html',
    notes: 'Segunda versión',
    markersDetected: ['vehicle.plate'],
    createdAt: '2026-06-10T12:00:00Z',
  },
  {
    templateId: TEMPLATE_V1_ID,
    documentTypeId: DOC_TYPE_ID,
    version: 1,
    status: 'deprecated',
    contentRef: 'templates/acme/v1.html',
    notes: null,
    markersDetected: ['vehicle.plate'],
    createdAt: '2026-06-09T08:00:00Z',
  },
];

export { seedOperadorSession, mockAuthMe };

export async function mockProcedureDetailForDocuments(page: Page) {
  await page.route(`**/api/v1/procedures/${DOC_PROCEDURE_ID}`, async (route) => {
    if (route.request().method() !== 'GET') return route.fallback();
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(
        buildProcedureDetail(DOC_PROCEDURE_ID, STEPPER_SNAPSHOT_ID, 'placa', [
          mockStep('00000000-0000-0000-0000-000000000041', 1, 'Vehículo', 'vehicle'),
        ]),
      ),
    });
  });

  await page.route(`**/api/v1/procedures/${DOC_PROCEDURE_ID}/actors`, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  await page.route(`**/api/v1/procedures/${DOC_PROCEDURE_ID}/attachments`, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  await page.route(`**/api/v1/procedures/${DOC_PROCEDURE_ID}/secondary-sellers`, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });
}

export async function mockDocumentsStatus(
  page: Page,
  payload: typeof MOCK_DOCUMENTS_STATUS = MOCK_DOCUMENTS_STATUS,
) {
  await page.route(`**/api/v1/procedures/${DOC_PROCEDURE_ID}/documents`, async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(payload),
      });
      return;
    }
    await route.fallback();
  });
}

export async function mockConsolidatedPdfDownload(page: Page) {
  await page.route(`**/api/v1/procedures/${DOC_PROCEDURE_ID}/consolidated`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/pdf',
      headers: {
        'content-disposition': 'attachment; filename="TRAMITE_mock.pdf"',
      },
      body: Buffer.from('%PDF-1.4 E2E mock consolidated'),
    });
  });
}

export async function mockDocumentTemplates(page: Page) {
  await page.route(`**/api/v1/document-types/${DOC_TYPE_ID}/templates`, async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_TEMPLATES),
      });
      return;
    }
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          templateId: '00000000-0000-0000-0000-000000000099',
          documentTypeId: DOC_TYPE_ID,
          version: 4,
          status: 'active',
          contentRef: 'templates/acme/v4.html',
          notes: 'Subida E2E',
          markersDetected: ['actor[vendedor].full_name', 'vehicle.plate'],
          createdAt: '2026-06-11T18:00:00Z',
        }),
      });
      return;
    }
    await route.fallback();
  });
}

export const MOCK_SUPERADMIN = {
  id: '550e8400-e29b-41d4-a716-446655440000',
  email: 'admin@acme.com',
  name: 'Admin FLIT Dev',
  roles: ['superadmin', 'admin'],
  permissions: ['tramites.read', 'tramites.admin.maestro'],
  tenant_id: '550e8400-e29b-41d4-a716-446655440001',
  tenant_name: 'Acme',
};

export async function seedSuperAdminSession(page: Page) {
  await page.addInitScript((user) => {
    localStorage.setItem('flit_access_token', 'e2e-jwt-token');
    localStorage.setItem('flit_user', JSON.stringify(user));
  }, MOCK_SUPERADMIN);
}

export async function mockSuperAdminAuthMe(page: Page) {
  await page.route('**/api/v1/auth/me', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: MOCK_SUPERADMIN.id,
        email: MOCK_SUPERADMIN.email,
        name: MOCK_SUPERADMIN.name,
        roles: MOCK_SUPERADMIN.roles,
        permissions: MOCK_SUPERADMIN.permissions,
        tenantId: MOCK_SUPERADMIN.tenant_id,
        tenantName: MOCK_SUPERADMIN.tenant_name,
      }),
    });
  });
}
