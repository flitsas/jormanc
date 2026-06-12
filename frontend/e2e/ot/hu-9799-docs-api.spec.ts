import { expect, test } from '@playwright/test';
import { loginAndGetToken, loginAsSuperAdmin, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import {
  associateDocumentConfig,
  createDocumentType,
} from '../documents/documents-helpers.js';
import {
  auth,
  createProcedureType,
  getAcmeCompanyId,
  PROCEDURES_BASE,
} from '../procedures/procedures-helpers.js';
import { createOtOrganism, OT_BASE } from './ot-helpers.js';

test.describe('HU #9799 — OT prelación y etiquetas API', () => {
  test('QA_TC01_OT_DOCS - Actualización de prelación se completa en menos de 500ms', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);
    const procedureType = await createProcedureType(request, superToken);
    const docA = await createDocumentType(request, superToken);
    const docB = await createDocumentType(request, superToken);
    await associateDocumentConfig(request, superToken, procedureType.id, docA.id, {
      orderIndex: 1,
    });
    await associateDocumentConfig(request, superToken, procedureType.id, docB.id, {
      orderIndex: 2,
    });

    const started = Date.now();
    const res = await request.put(
      `${OT_BASE}/${ot.id}/document-order/${procedureType.id}`,
      {
        headers: auth(token),
        data: { orderedDocumentTypeIds: [docB.id, docA.id] },
      },
    );
    const elapsed = Date.now() - started;

    expect(res.status()).toBe(200);
    expect(elapsed).toBeLessThan(500);
    const body = await res.json();
    expect(body.updated).toBe(true);
    expect(body.orderedDocuments[0].documentType.id).toBe(docB.id);
  });

  test('QA_TC02_OT_DOCS - Delete de etiqueta con adjuntos requiere confirm=true', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);
    const slug = `lbl-${Date.now().toString().slice(-5)}`;

    const create = await request.post(`${OT_BASE}/${ot.id}/labels`, {
      headers: auth(token),
      data: { slug, displayName: 'Etiqueta E2E' },
    });
    expect(create.status()).toBe(201);
    const label = await create.json();

    const companyId = await getAcmeCompanyId(request, superToken);
    const procedureType = await createProcedureType(request, superToken);
    const procRes = await request.post(`${PROCEDURES_BASE}/`, {
      headers: auth(token),
      data: { procedureTypeId: procedureType.id, companyId, otId: ot.id },
    });
    expect(procRes.status()).toBe(201);
    const procedure = await procRes.json();

    const uploadRes = await request.post(
      `${PROCEDURES_BASE}/${procedure.id}/attachments`,
      {
        headers: auth(token),
        multipart: {
          file: {
            name: 'doc.pdf',
            mimeType: 'application/pdf',
            buffer: Buffer.from('%PDF-1.4 E2E'),
          },
          label_slug: slug,
        },
      },
    );
    expect(uploadRes.status()).toBe(201);

    const withoutConfirm = await request.delete(`${OT_BASE}/${ot.id}/labels/${label.id}`, {
      headers: auth(token),
      data: { confirm: false },
    });
    expect(withoutConfirm.status()).toBe(409);
    expect((await withoutConfirm.json()).error).toBe('LABEL_IN_USE');

    const withConfirm = await request.delete(`${OT_BASE}/${ot.id}/labels/${label.id}`, {
      headers: auth(token),
      data: { confirm: true },
    });
    expect(withConfirm.status()).toBe(200);
    expect((await withConfirm.json()).deleted).toBe(true);
  });

  test('QA_TC03_OT_DOCS - Crear etiqueta con slug único por OT', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);
    const slug = `tag-${Date.now().toString().slice(-6)}`;

    const res = await request.post(`${OT_BASE}/${ot.id}/labels`, {
      headers: auth(token),
      data: { slug, displayName: 'Copia autenticada' },
    });
    expect(res.status()).toBe(201);
    const body = await res.json();
    expect(body.slug).toBe(slug);
    expect(body.displayName).toBe('Copia autenticada');
    expect(body.otId).toBe(ot.id);
  });

  test('QA_TC04_OT_DOCS - Caso Borde Multitenant', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const foreignOtId = '00000000-0000-0000-0000-000000000098';

    const res = await request.get(`${OT_BASE}/${foreignOtId}/labels`, {
      headers: auth(token),
    });
    expect(res.status()).toBe(404);
  });

  test('QA_TC05_OT_DOCS - Validacion Contrato API', async ({ request }) => {
    const superToken = await loginAsSuperAdmin(request);
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const ot = await createOtOrganism(request, token);
    const procedureType = await createProcedureType(request, superToken);
    const doc = await createDocumentType(request, superToken);
    await associateDocumentConfig(request, superToken, procedureType.id, doc.id);

    await request.put(`${OT_BASE}/${ot.id}/document-order/${procedureType.id}`, {
      headers: auth(token),
      data: { orderedDocumentTypeIds: [doc.id] },
    });

    const res = await request.get(`${OT_BASE}/${ot.id}/document-order`, {
      headers: auth(token),
    });
    expect(res.status()).toBe(200);
    const entries = await res.json();
    expect(Array.isArray(entries)).toBe(true);
    const entry = entries.find(
      (e: { procedureTypeId: string }) => e.procedureTypeId === procedureType.id,
    );
    expect(entry).toBeTruthy();
    expect(entry.orderedDocuments[0].documentType.id).toBe(doc.id);
    expect(typeof entry.orderedDocuments[0].orderIndex).toBe('number');
  });
});
