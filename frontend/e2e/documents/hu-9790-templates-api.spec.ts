import { expect, test } from '@playwright/test';
import { loginAsSuperAdmin } from '../fixtures/qa-auth.js';
import {
  createDocumentType,
  DOC_TYPES_BASE,
  sampleTemplateHtml,
  uploadDocumentTemplate,
} from './documents-helpers.js';

test.describe('HU #9790 — Plantillas HTML y PDF API', () => {
  test('QA_TC01_DOCUMENTOS_TEMPLATES - Upload detecta marcadores y crea versión activa', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const docType = await createDocumentType(request, token, { loadType: 'generacion' });
    const html = sampleTemplateHtml(['actor[vendedor].full_name', 'vehicle.plate']);

    const res = await uploadDocumentTemplate(request, token, docType.id, html, 'Primera versión');
    expect(res.status()).toBe(201);
    const body = await res.json();

    expect(body.templateId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
    expect(body.documentTypeId).toBe(docType.id);
    expect(body.version).toBe(1);
    expect(body.status).toBe('active');
    expect(body.contentRef).toContain(`templates/`);
    expect(body.contentRef).toContain(`${docType.id}`);
    expect(body.markersDetected).toEqual(
      expect.arrayContaining(['actor[vendedor].full_name', 'vehicle.plate']),
    );
  });

  test('QA_TC02_DOCUMENTOS_TEMPLATES - Segunda versión depreca la anterior', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const docType = await createDocumentType(request, token, { loadType: 'generacion' });

    const v1Res = await uploadDocumentTemplate(
      request,
      token,
      docType.id,
      sampleTemplateHtml(['vehicle.plate']),
      'v1',
    );
    expect(v1Res.status()).toBe(201);
    const v1 = await v1Res.json();

    const v2Res = await uploadDocumentTemplate(
      request,
      token,
      docType.id,
      sampleTemplateHtml(['vehicle.plate', 'actor[comprador].nit']),
      'v2',
    );
    expect(v2Res.status()).toBe(201);
    const v2 = await v2Res.json();
    expect(v2.version).toBe(2);
    expect(v2.status).toBe('active');

    const listRes = await request.get(`${DOC_TYPES_BASE}/${docType.id}/templates`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(listRes.status()).toBe(200);
    const templates = await listRes.json();

    const byVersion = Object.fromEntries(
      templates.map((t: { version: number; status: string }) => [t.version, t.status]),
    );
    expect(byVersion[1]).toBe('deprecated');
    expect(byVersion[2]).toBe('active');
    expect(v1.templateId).not.toBe(v2.templateId);
  });

  test('QA_TC03_DOCUMENTOS_TEMPLATES - Preview PDF resuelve marcadores con contexto', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const docType = await createDocumentType(request, token, { loadType: 'generacion' });
    const uploadRes = await uploadDocumentTemplate(
      request,
      token,
      docType.id,
      '<html><body><p>Placa: {{vehicle.plate}}</p><p>NIT: {{actor[comprador].nit}}</p></body></html>',
    );
    expect(uploadRes.status()).toBe(201);
    const template = await uploadRes.json();

    const previewRes = await request.post(
      `${DOC_TYPES_BASE}/${docType.id}/templates/${template.templateId}/preview-pdf`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          vehicle: { plate: 'AAA123' },
          actors: {},
        },
      },
    );
    expect(previewRes.status()).toBe(200);
    expect(previewRes.headers()['content-type']).toContain('application/pdf');
    const buffer = await previewRes.body();
    expect(buffer.byteLength).toBeGreaterThan(100);
    expect(buffer.subarray(0, 4).toString()).toBe('%PDF');
  });

  test('QA_TC04_DOCUMENTOS_TEMPLATES - GET templates lista versiones del tipo', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const docType = await createDocumentType(request, token, { loadType: 'generacion' });
    await uploadDocumentTemplate(request, token, docType.id, sampleTemplateHtml(['vehicle.plate']));

    const res = await request.get(`${DOC_TYPES_BASE}/${docType.id}/templates`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(res.status()).toBe(200);
    const templates = await res.json();
    expect(Array.isArray(templates)).toBe(true);
    expect(templates.length).toBeGreaterThanOrEqual(1);
    expect(templates[0]).toMatchObject({
      documentTypeId: docType.id,
      version: expect.any(Number),
      status: expect.stringMatching(/active|deprecated/),
      markersDetected: expect.any(Array),
    });
  });

  test('QA_TC05_DOCUMENTOS_TEMPLATES - Tipo de documento inexistente retorna 404', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const fakeId = '00000000-0000-0000-0000-000000000099';

    const res = await uploadDocumentTemplate(
      request,
      token,
      fakeId,
      sampleTemplateHtml(),
    );
    expect(res.status()).toBe(404);
    expect((await res.json()).code).toBe('DOCUMENT_TYPE_NOT_FOUND');
  });
});
