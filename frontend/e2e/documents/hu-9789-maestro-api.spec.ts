import { expect, test } from '@playwright/test';
import { loginAndGetToken, loginAsSuperAdmin, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import { PT_BASE } from '../procedures/procedures-helpers.js';
import {
  associateDocumentConfig,
  createDocumentType,
  DOC_TYPES_BASE,
} from './documents-helpers.js';

test.describe('HU #9789 — Maestro documental API', () => {
  test('QA_TC01_DOCUMENTOS_MAESTRO - Crear tipo de documento y asociar a tipo de trámite', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const ptRes = await request.post(`${PT_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `E2E PT Doc ${Date.now().toString().slice(-7)}`,
        family: 'traspasos',
        scope: 'global',
        vehicleQueryKey: 'placa',
      },
    });
    expect(ptRes.status()).toBe(201);
    const procedureType = await ptRes.json();

    const docType = await createDocumentType(request, token, {
      name: 'Licencia de Tránsito',
      loadType: 'carga',
    });

    const assocRes = await associateDocumentConfig(
      request,
      token,
      procedureType.id,
      docType.id,
      { isRequired: true, orderIndex: 1 },
    );
    expect(assocRes.status()).toBe(201);
    const association = await assocRes.json();

    expect(association.procedureTypeId).toBe(procedureType.id);
    expect(association.documentTypeId).toBe(docType.id);
    expect(association.isRequired).toBe(true);
    expect(association.orderIndex).toBe(1);
    expect(association.loadType).toBe('carga');
    expect(association.documentTypeName).toBe('Licencia de Tránsito');
  });

  test('QA_TC02_DOCUMENTOS_MAESTRO - Asociación duplicada retorna 409 DOCUMENT_ALREADY_ASSOCIATED', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const ptRes = await request.post(`${PT_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `E2E Dup ${Date.now().toString().slice(-7)}`,
        family: 'otros',
        scope: 'global',
        vehicleQueryKey: 'placa',
      },
    });
    expect(ptRes.status()).toBe(201);
    const procedureType = await ptRes.json();
    const docType = await createDocumentType(request, token);

    const first = await associateDocumentConfig(
      request,
      token,
      procedureType.id,
      docType.id,
    );
    expect(first.status()).toBe(201);

    const second = await associateDocumentConfig(
      request,
      token,
      procedureType.id,
      docType.id,
    );
    expect(second.status()).toBe(409);
    expect((await second.json()).code).toBe('DOCUMENT_ALREADY_ASSOCIATED');
  });

  test('QA_TC03_DOCUMENTOS_MAESTRO - GET document-config ordenado por order_index', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const ptRes = await request.post(`${PT_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        name: `E2E Orden ${Date.now().toString().slice(-7)}`,
        family: 'traspasos',
        scope: 'global',
        vehicleQueryKey: 'placa',
      },
    });
    expect(ptRes.status()).toBe(201);
    const procedureType = await ptRes.json();

    const docs = await Promise.all([
      createDocumentType(request, token, { name: 'Doc A', loadType: 'carga' }),
      createDocumentType(request, token, { name: 'Doc B', loadType: 'generacion' }),
      createDocumentType(request, token, { name: 'Doc C', loadType: 'carga' }),
    ]);

    for (const [index, doc] of docs.entries()) {
      const res = await associateDocumentConfig(request, token, procedureType.id, doc.id, {
        orderIndex: index + 1,
        isRequired: index !== 2,
        allowPartialConsolidation: index === 2,
      });
      expect(res.status()).toBe(201);
    }

    const listRes = await request.get(
      `${PT_BASE}/${procedureType.id}/document-config`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(listRes.status()).toBe(200);
    const configs = await listRes.json();

    expect(configs).toHaveLength(3);
    expect(configs.map((c: { orderIndex: number }) => c.orderIndex)).toEqual([1, 2, 3]);
    expect(configs[0].documentTypeName).toBe('Doc A');
    expect(configs[1].loadType).toBe('generacion');
    expect(configs[2].allowPartialConsolidation).toBe(true);
    expect(configs[2].isRequired).toBe(false);
  });

  test('QA_TC04_DOCUMENTOS_MAESTRO - Operador sin superadmin recibe 403 en document-types', async ({
    request,
  }) => {
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);

    const res = await request.post(`${DOC_TYPES_BASE}/`, {
      headers: { Authorization: `Bearer ${operadorToken}` },
      data: { name: 'No permitido', loadType: 'carga' },
    });
    expect(res.status()).toBe(403);
    expect((await res.json()).code).toBe('FORBIDDEN');
  });

  test('QA_TC05_DOCUMENTOS_MAESTRO - Validación name requerido en POST document-types', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);

    const res = await request.post(`${DOC_TYPES_BASE}/`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { name: '   ', loadType: 'carga' },
    });
    expect(res.status()).toBe(400);
    expect((await res.json()).code).toBe('VALIDATION_ERROR');
  });
});
