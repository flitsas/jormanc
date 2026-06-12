import { expect, test } from '@playwright/test';
import { loginAndGetToken, loginAsSuperAdmin, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import {
  auth,
  createDraftProcedure,
  getAcmeCompanyId,
  PROCEDURES_BASE,
  PT_BASE,
  seedActorDefinition,
} from './procedures-helpers.js';

async function createSubmitReadyType(
  request: import('@playwright/test').APIRequestContext,
  superToken: string,
  options: { requiredPlaca?: boolean } = {},
) {
  const typeRes = await request.post(`${PT_BASE}/`, {
    headers: auth(superToken),
    data: {
      name: `E2E Submit ${Date.now().toString().slice(-7)}`,
      family: 'traspasos',
      scope: 'global',
      vehicleQueryKey: 'placa',
    },
  });
  expect(typeRes.status()).toBe(201);
  const created = await typeRes.json();

  const vendedor = await seedActorDefinition(request, superToken, created.id, {
    role: 'vendedor',
    allowedNature: 'natural',
    minCount: 1,
    maxCount: 1,
    isRequired: true,
    orderIndex: 0,
  });

  if (options.requiredPlaca) {
    const stepRes = await request.post(`${PT_BASE}/${created.id}/steps`, {
      headers: auth(superToken),
      data: { name: 'Datos del vehículo', stepType: 'form', orderIndex: 1 },
    });
    expect(stepRes.status()).toBe(201);
    const step = await stepRes.json();

    const sectionRes = await request.post(
      `${PT_BASE}/${created.id}/steps/${step.id}/sections`,
      {
        headers: auth(superToken),
        data: { slug: 'veh', name: 'Vehículo', orderIndex: 1 },
      },
    );
    expect(sectionRes.status()).toBe(201);
    const section = await sectionRes.json();

    const fieldRes = await request.post(
      `${PT_BASE}/${created.id}/steps/${step.id}/sections/${section.id}/fields`,
      {
        headers: auth(superToken),
        data: {
          slug: 'placa',
          fieldType: 'text',
          isRequired: true,
          orderIndex: 1,
        },
      },
    );
    expect(fieldRes.status()).toBe(201);
  }

  return { typeId: created.id as string, vendedorDefId: vendedor.id };
}

test.describe('HU #9786 — Submit, pipeline y adjuntos', () => {
  test('QA_TC01_TRAMITES_SUBMIT - Submit válido pasa a submitted', async ({ request }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { typeId, vendedorDefId } = await createSubmitReadyType(request, superToken);
    const companyId = await getAcmeCompanyId(request, superToken);

    const procRes = await request.post(`${PROCEDURES_BASE}/`, {
      headers: auth(operadorToken),
      data: { procedureTypeId: typeId, companyId },
    });
    expect(procRes.status()).toBe(201);
    const proc = await procRes.json();

    const actorRes = await request.post(`${PROCEDURES_BASE}/${proc.id}/actors`, {
      headers: auth(operadorToken),
      data: {
        actorDefinitionId: vendedorDefId,
        nature: 'natural',
        documentType: 'CC',
        documentNumber: '12345678',
      },
    });
    expect(actorRes.status()).toBe(201);

    const submitRes = await request.post(`${PROCEDURES_BASE}/${proc.id}/submit`, {
      headers: auth(operadorToken),
    });
    expect(submitRes.status()).toBe(200);
    const submitted = await submitRes.json();

    expect(submitted.status).toBe('submitted');
    expect(submitted.submittedAt).toBeTruthy();

    const getRes = await request.get(`${PROCEDURES_BASE}/${proc.id}`, {
      headers: auth(operadorToken),
    });
    expect(getRes.status()).toBe(200);
    expect((await getRes.json()).status).toBe('submitted');
  });

  test('QA_TC02_TRAMITES_SUBMIT - Submit rechazado por campo requerido faltante', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { typeId } = await createSubmitReadyType(request, superToken, {
      requiredPlaca: true,
    });
    const companyId = await getAcmeCompanyId(request, superToken);

    const procRes = await request.post(`${PROCEDURES_BASE}/`, {
      headers: auth(operadorToken),
      data: { procedureTypeId: typeId, companyId },
    });
    expect(procRes.status()).toBe(201);
    const proc = await procRes.json();

    const submitRes = await request.post(`${PROCEDURES_BASE}/${proc.id}/submit`, {
      headers: auth(operadorToken),
    });
    expect(submitRes.status()).toBe(422);
    const body = await submitRes.json();

    expect(Array.isArray(body.errors)).toBe(true);
    expect(body.errors.some((e: { field_slug: string }) => e.field_slug === 'placa')).toBe(
      true,
    );

    const stillDraft = await request.get(`${PROCEDURES_BASE}/${proc.id}`, {
      headers: auth(operadorToken),
    });
    expect((await stillDraft.json()).status).toBe('draft');
  });

  test('QA_TC03_TRAMITES_SUBMIT - Upload adjunto con etiqueta dinámica', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { procedure } = await createDraftProcedure(request, superToken, operadorToken);

    const uploadRes = await request.post(
      `${PROCEDURES_BASE}/${procedure.id}/attachments`,
      {
        headers: auth(operadorToken),
        multipart: {
          file: {
            name: 'licencia.pdf',
            mimeType: 'application/pdf',
            buffer: Buffer.from('%PDF-1.4 E2E test'),
          },
          label_slug: 'licencia_transito',
        },
      },
    );
    expect(uploadRes.status()).toBe(201);
    const uploaded = await uploadRes.json();

    expect(uploaded).toMatchObject({
      attachment_id: expect.any(String),
      file_name: 'licencia.pdf',
      label_slug: 'licencia_transito',
    });

    const listRes = await request.get(
      `${PROCEDURES_BASE}/${procedure.id}/attachments`,
      { headers: auth(operadorToken) },
    );
    expect(listRes.status()).toBe(200);
    const list = await listRes.json();
    expect(list.some((a: { labelSlug: string }) => a.labelSlug === 'licencia_transito')).toBe(
      true,
    );
  });

  test('QA_TC04_TRAMITES_SUBMIT - Caso Borde submit trámite inexistente', async ({
    request,
  }) => {
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const fakeId = '00000000-0000-0000-0000-000000000098';

    const res = await request.post(`${PROCEDURES_BASE}/${fakeId}/submit`, {
      headers: auth(operadorToken),
    });
    expect(res.status()).toBe(404);
    expect((await res.json()).code).toBe('PROCEDURE_NOT_FOUND');
  });

  test('QA_TC05_TRAMITES_SUBMIT - Validacion Contrato API attachments list', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { procedure } = await createDraftProcedure(request, superToken, operadorToken);

    await request.post(`${PROCEDURES_BASE}/${procedure.id}/attachments`, {
      headers: auth(operadorToken),
      multipart: {
        file: {
          name: 'doc.pdf',
          mimeType: 'application/pdf',
          buffer: Buffer.from('%PDF'),
        },
        label_slug: 'otro_doc',
      },
    });

    const listRes = await request.get(
      `${PROCEDURES_BASE}/${procedure.id}/attachments`,
      { headers: auth(operadorToken) },
    );
    expect(listRes.status()).toBe(200);
    const list = await listRes.json();

    expect(Array.isArray(list)).toBe(true);
    expect(list.length).toBeGreaterThanOrEqual(1);
    expect(list[0]).toMatchObject({
      id: expect.any(String),
      fileName: expect.any(String),
      labelSlug: expect.any(String),
      sizeBytes: expect.any(Number),
      uploadedAt: expect.any(String),
    });
  });
});
