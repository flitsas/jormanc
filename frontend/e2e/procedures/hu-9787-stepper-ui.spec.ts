import { expect, test } from '@playwright/test';
import {
  buildProcedureDetail,
  FIVE_STEPS,
  mockAuthMe,
  mockProcedureDetailRoutes,
  mockStep,
  seedOperadorSession,
  STEPPER_PROCEDURE_ID,
  STEPPER_SNAPSHOT_ID,
} from './procedures-ui-mocks.js';

test.describe('HU #9787 — Stepper dinámico y captura de vehículo', () => {
  test('QA_TC01_TRAMITES_STEPPER - DynamicStepper renderiza 5 pasos desde snapshot', async ({
    page,
  }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);
    await mockProcedureDetailRoutes(
      page,
      STEPPER_PROCEDURE_ID,
      buildProcedureDetail(STEPPER_PROCEDURE_ID, STEPPER_SNAPSHOT_ID, 'placa', FIVE_STEPS),
    );

    await page.goto(`/procedures/${STEPPER_PROCEDURE_ID}`);

    const stepper = page.getByTestId('dynamic-stepper-content');
    await expect(stepper).toBeVisible();
    await expect(stepper).toHaveAttribute('data-snapshot-id', STEPPER_SNAPSHOT_ID);

    for (const step of FIVE_STEPS) {
      await expect(
        page.getByRole('button', { name: new RegExp(`${step.orderIndex}\\.\\s*${step.name}`, 'i') }),
      ).toBeVisible();
    }
  });

  test('QA_TC02_TRAMITES_STEPPER - VehicleCaptureStep label placa vs VIN', async ({ page }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);

    for (const [key, label] of [
      ['placa', /Placa/i],
      ['vin', /VIN \/ Número de chasis/i],
    ] as const) {
      await page.unrouteAll();
      await mockAuthMe(page);
      await mockProcedureDetailRoutes(
        page,
        STEPPER_PROCEDURE_ID,
        buildProcedureDetail(STEPPER_PROCEDURE_ID, STEPPER_SNAPSHOT_ID, key, [
          mockStep('00000000-0000-0000-0000-000000000021', 1, 'Vehículo', 'vehicle'),
        ]),
      );

      await page.goto(`/procedures/${STEPPER_PROCEDURE_ID}`);
      await expect(page.getByRole('textbox', { name: label })).toBeVisible();
    }
  });

  test('QA_TC03_TRAMITES_STEPPER - CopropietariosManager error inline cuota > 100%', async ({
    page,
  }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);

    await mockProcedureDetailRoutes(
      page,
      STEPPER_PROCEDURE_ID,
      buildProcedureDetail(STEPPER_PROCEDURE_ID, STEPPER_SNAPSHOT_ID, 'placa', [
        mockStep('00000000-0000-0000-0000-000000000031', 1, 'Copropietarios', 'copropietarios', false),
      ]),
    );

    await page.route(`**/api/v1/procedures/${STEPPER_PROCEDURE_ID}/actors`, async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 422,
          contentType: 'application/json',
          body: JSON.stringify({
            error: 'CUOTA_SUM_EXCEEDS_100',
            current_sum: 80,
            proposed: 30,
          }),
        });
        return;
      }
      await route.continue();
    });

    await page.goto(`/procedures/${STEPPER_PROCEDURE_ID}`);

    await page.getByLabel(/número de documento/i).fill('99999999');
    const cuotaInput = page
      .getByRole('region', { name: /copropietarios/i })
      .locator('input.p-inputnumber-input');
    await cuotaInput.click();
    await cuotaInput.fill('30');
    await cuotaInput.blur();
    await expect(page.getByRole('button', { name: /agregar copropietario/i })).toBeEnabled();
    await page.getByRole('button', { name: /agregar copropietario/i }).click();

    await expect(page.getByText(/supera el 100%/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /continuar/i })).toBeDisabled();
  });

  test('QA_TC04_TRAMITES_STEPPER - Caso Borde snapshot vacío', async ({ page }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);
    await mockProcedureDetailRoutes(
      page,
      STEPPER_PROCEDURE_ID,
      buildProcedureDetail(STEPPER_PROCEDURE_ID, STEPPER_SNAPSHOT_ID, 'placa', []),
    );

    await page.goto(`/procedures/${STEPPER_PROCEDURE_ID}`);
    await expect(page.getByText(/no hay pasos configurados/i)).toBeVisible();
  });

  test('QA_TC05_TRAMITES_STEPPER - Validacion Contrato UI snapshot id en DOM', async ({
    page,
  }) => {
    await seedOperadorSession(page);
    await mockAuthMe(page);
    await mockProcedureDetailRoutes(
      page,
      STEPPER_PROCEDURE_ID,
      buildProcedureDetail(STEPPER_PROCEDURE_ID, STEPPER_SNAPSHOT_ID, 'placa', FIVE_STEPS),
    );

    await page.goto(`/procedures/${STEPPER_PROCEDURE_ID}`);
    await expect(page.getByText(STEPPER_SNAPSHOT_ID)).toBeVisible();
    await expect(page.getByTestId('dynamic-stepper-content')).toHaveAttribute(
      'data-snapshot-id',
      STEPPER_SNAPSHOT_ID,
    );
  });
});
