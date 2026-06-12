import { expect, test } from '@playwright/test';
import { loginAndGetToken, loginAsSuperAdmin, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import { auth, getAcmeCompanyId, PT_BASE } from '../procedures/procedures-helpers.js';
import {
  associateDocumentConfig,
  createDocumentType,
  createProcedureWithDocumentConfig,
  PROCEDURES_BASE,
  setupGeneracionPipeline,
} from './documents-helpers.js';

test.describe('HU #9791 — Consolidación documental API', () => {
  test('QA_TC01_DOCUMENTOS_CONSOLIDACION - GET estado documental del trámite', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { procedure } = await createProcedureWithDocumentConfig(
      request,
      superToken,
      operadorToken,
    );

    const res = await request.get(`${PROCEDURES_BASE}/${procedure.id}/documents`, {
      headers: auth(operadorToken),
    });
    expect(res.status()).toBe(200);
    const body = await res.json();

    expect(body.procedureId).toBe(procedure.id);
    expect(Array.isArray(body.documents)).toBe(true);
    expect(Array.isArray(body.consolidatedPackages)).toBe(true);
  });

  test('QA_TC02_DOCUMENTOS_CONSOLIDACION - Consolidación bloqueada sin documentos listos', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ptRes = await request.post(`${PT_BASE}/`, {
      headers: auth(superToken),
      data: {
        name: `E2E Solo Carga ${Date.now().toString().slice(-7)}`,
        family: 'otros',
        scope: 'global',
        vehicleQueryKey: 'placa',
      },
    });
    expect(ptRes.status()).toBe(201);
    const procedureType = await ptRes.json();

    const cargaType = await createDocumentType(request, superToken, {
      name: 'Licencia pendiente',
      loadType: 'carga',
    });
    const assocRes = await associateDocumentConfig(
      request,
      superToken,
      procedureType.id,
      cargaType.id,
      { isRequired: true, orderIndex: 1 },
    );
    expect(assocRes.status()).toBe(201);

    const companyId = await getAcmeCompanyId(request, superToken);

    const procRes = await request.post(`${PROCEDURES_BASE}/`, {
      headers: auth(operadorToken),
      data: { procedureTypeId: procedureType.id, companyId },
    });
    expect(procRes.status()).toBe(201);
    const procedure = await procRes.json();

    const consolidateRes = await request.post(
      `${PROCEDURES_BASE}/${procedure.id}/documents/consolidate`,
      { headers: auth(superToken) },
    );
    expect(consolidateRes.status()).toBe(400);
    expect((await consolidateRes.json()).code).toBe('CONSOLIDATION_NOT_READY');
  });

  test('QA_TC03_DOCUMENTOS_CONSOLIDACION - Forzar consolidación genera paquete v1', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { procedure } = await createProcedureWithDocumentConfig(
      request,
      superToken,
      operadorToken,
    );

    const consolidateRes = await request.post(
      `${PROCEDURES_BASE}/${procedure.id}/documents/consolidate`,
      { headers: auth(superToken) },
    );
    expect(consolidateRes.status()).toBe(200);
    const pkg = await consolidateRes.json();

    expect(pkg.version).toBeGreaterThanOrEqual(1);
    expect(pkg.docCount).toBeGreaterThanOrEqual(1);
    expect(pkg.downloadFilename).toMatch(/^TRAMITE_/);
    expect(pkg.mergedFileRef).toBeTruthy();
  });

  test('QA_TC04_DOCUMENTOS_CONSOLIDACION - GET consolidated descarga PDF', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const { procedure } = await createProcedureWithDocumentConfig(request, superToken);

    await request.post(`${PROCEDURES_BASE}/${procedure.id}/documents/consolidate`, {
      headers: auth(superToken),
    });

    const downloadRes = await request.get(
      `${PROCEDURES_BASE}/${procedure.id}/consolidated`,
      { headers: auth(superToken) },
    );
    expect(downloadRes.status()).toBe(200);
    expect(downloadRes.headers()['content-type']).toContain('application/pdf');
    const pdf = await downloadRes.body();
    expect(pdf.subarray(0, 4).toString()).toBe('%PDF');
  });

  test('QA_TC05_DOCUMENTOS_CONSOLIDACION - Re-consolidación crea versión 2 preservando historial', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const { procedure } = await createProcedureWithDocumentConfig(request, superToken);

    const first = await request.post(
      `${PROCEDURES_BASE}/${procedure.id}/documents/consolidate`,
      { headers: auth(superToken) },
    );
    expect(first.status()).toBe(200);
    const firstVersion = (await first.json()).version as number;

    const second = await request.post(
      `${PROCEDURES_BASE}/${procedure.id}/documents/consolidate`,
      { headers: auth(superToken) },
    );
    expect(second.status()).toBe(200);
    const secondVersion = (await second.json()).version as number;
    expect(secondVersion).toBeGreaterThan(firstVersion);

    const statusRes = await request.get(`${PROCEDURES_BASE}/${procedure.id}/documents`, {
      headers: auth(superToken),
    });
    expect(statusRes.status()).toBe(200);
    const status = await statusRes.json();
    const versions = status.consolidatedPackages.map((p: { version: number }) => p.version).sort(
      (a: number, b: number) => a - b,
    );
    expect(versions.length).toBeGreaterThanOrEqual(2);
    expect(versions).toContain(firstVersion);
    expect(versions).toContain(secondVersion);
  });
});
