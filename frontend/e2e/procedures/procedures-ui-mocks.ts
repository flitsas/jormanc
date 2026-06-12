import type { Page } from '@playwright/test';

export const MOCK_OPERADOR = {
  id: '550e8400-e29b-41d4-a716-446655440000',
  email: 'operador@acme.com',
  name: 'Operador Tenant',
  roles: ['admin'],
  permissions: ['tramites.read', 'tramites.create'],
  tenant_id: '550e8400-e29b-41d4-a716-446655440001',
  tenant_name: 'Acme',
};

export async function seedOperadorSession(page: Page) {
  await page.addInitScript((user) => {
    localStorage.setItem('flit_access_token', 'e2e-jwt-token');
    localStorage.setItem('flit_user', JSON.stringify(user));
  }, MOCK_OPERADOR);
}

export async function mockAuthMe(page: Page) {
  await page.route('**/api/v1/auth/me', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: MOCK_OPERADOR.id,
        email: MOCK_OPERADOR.email,
        name: MOCK_OPERADOR.name,
        roles: MOCK_OPERADOR.roles,
        permissions: MOCK_OPERADOR.permissions,
        tenantId: MOCK_OPERADOR.tenant_id,
        tenantName: MOCK_OPERADOR.tenant_name,
      }),
    });
  });
}

/** Step ids must be UUID — ProcedureDetailSchema / SnapshotStepSchema. */
export function mockStep(
  id: string,
  orderIndex: number,
  name: string,
  stepType: string,
  isRequired = true,
) {
  return {
    id,
    orderIndex,
    name,
    stepType,
    isRequired,
    sections: [] as unknown[],
  };
}

export const STEPPER_PROCEDURE_ID = '00000000-0000-0000-0000-000000000001';
export const STEPPER_SNAPSHOT_ID = '00000000-0000-0000-0000-000000000050';

export const FIVE_STEPS = [
  mockStep('00000000-0000-0000-0000-000000000011', 1, 'Vehículo', 'vehicle'),
  mockStep('00000000-0000-0000-0000-000000000012', 2, 'Datos generales', 'form'),
  mockStep('00000000-0000-0000-0000-000000000013', 3, 'Compradores', 'copropietarios', false),
  mockStep('00000000-0000-0000-0000-000000000014', 4, 'Documentos', 'form', false),
  mockStep('00000000-0000-0000-0000-000000000015', 5, 'Revisión', 'form', false),
];

export function buildProcedureDetail(
  procedureId: string,
  snapshotId: string,
  vehicleQueryKey: string,
  steps: ReturnType<typeof mockStep>[],
  overrides: Record<string, unknown> = {},
) {
  return {
    id: procedureId,
    compositeId: 'TRASP-01_ACM-0001',
    status: 'draft',
    procedureTypeSnapshotId: snapshotId,
    vehicleQueryKey,
    currentStepOrder: 1,
    stepData: {},
    snapshotConfig: { steps },
    ...overrides,
  };
}

export async function mockProcedureDetailRoutes(
  page: Page,
  procedureId: string,
  detail: ReturnType<typeof buildProcedureDetail>,
) {
  await page.route(`**/api/v1/procedures/${procedureId}`, async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(detail),
      });
      return;
    }
    await route.continue();
  });

  await page.route(`**/api/v1/procedures/${procedureId}/attachments`, async (route) => {
    await route.fulfill({ status: 200, body: JSON.stringify([]) });
  });
  await page.route(`**/api/v1/procedures/${procedureId}/secondary-sellers`, async (route) => {
    await route.fulfill({ status: 200, body: JSON.stringify([]) });
  });
  await page.route(`**/api/v1/procedures/${procedureId}/documents`, async (route) => {
    await route.fulfill({ status: 200, body: JSON.stringify([]) });
  });
}
