import { expect } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import { loginAndGetToken, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import {
  auth,
  createProcedureType,
  getAcmeCompanyId,
  uniqueName,
} from '../procedures/procedures-helpers.js';

export const DOC_TYPES_BASE = `${API_BASE}/document-types`;
export const PT_BASE = `${API_BASE}/procedure-types`;
export const PROCEDURES_BASE = `${API_BASE}/procedures`;

export function sampleTemplateHtml(markers: string[] = ['actor[vendedor].full_name', 'vehicle.plate']) {
  const body = markers.map((m) => `<p>{{${m}}}</p>`).join('\n');
  return `<html><body>${body}</body></html>`;
}

export async function createDocumentType(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  options: { name?: string; loadType?: 'carga' | 'generacion' } = {},
) {
  const res = await request.post(`${DOC_TYPES_BASE}/`, {
    headers: auth(token),
    data: {
      name: options.name ?? uniqueName('E2E Doc'),
      loadType: options.loadType ?? 'carga',
      maxSizeMb: 10,
      isReusable: true,
    },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{
    id: string;
    tenantId: string;
    name: string;
    loadType: string;
  }>;
}

export async function associateDocumentConfig(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  procedureTypeId: string,
  documentTypeId: string,
  options: {
    isRequired?: boolean;
    orderIndex?: number;
    allowPartialConsolidation?: boolean;
  } = {},
) {
  const res = await request.post(`${PT_BASE}/${procedureTypeId}/document-config`, {
    headers: auth(token),
    data: {
      documentTypeId,
      isRequired: options.isRequired ?? true,
      orderIndex: options.orderIndex ?? 1,
      allowPartialConsolidation: options.allowPartialConsolidation ?? false,
    },
  });
  return res;
}

export async function uploadDocumentTemplate(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  documentTypeId: string,
  html: string,
  notes?: string,
) {
  const multipart: Record<string, string | { name: string; mimeType: string; buffer: Buffer }> = {
    html_content: {
      name: 'plantilla.html',
      mimeType: 'text/html',
      buffer: Buffer.from(html, 'utf-8'),
    },
  };
  if (notes) multipart.notes = notes;

  const res = await request.post(`${DOC_TYPES_BASE}/${documentTypeId}/templates`, {
    headers: auth(token),
    multipart,
  });
  return res;
}

export async function setupGeneracionPipeline(
  request: import('@playwright/test').APIRequestContext,
  superToken: string,
) {
  const procedureType = await createProcedureType(request, superToken);
  const docType = await createDocumentType(request, superToken, {
    loadType: 'generacion',
    name: uniqueName('E2E Contrato'),
  });
  const templateRes = await uploadDocumentTemplate(
    request,
    superToken,
    docType.id,
    sampleTemplateHtml(),
    'E2E plantilla inicial',
  );
  expect(templateRes.status()).toBe(201);
  const template = await templateRes.json();

  const assocRes = await associateDocumentConfig(request, superToken, procedureType.id, docType.id);
  expect(assocRes.status()).toBe(201);
  const association = await assocRes.json();

  return { procedureType, docType, template, association };
}

export async function createProcedureWithDocumentConfig(
  request: import('@playwright/test').APIRequestContext,
  superToken: string,
  operadorToken?: string,
) {
  const pipeline = await setupGeneracionPipeline(request, superToken);
  const opToken =
    operadorToken ?? (await loginAndGetToken(request, QA_TENANT_ADMIN));
  const companyId = await getAcmeCompanyId(request, superToken);

  const procRes = await request.post(`${PROCEDURES_BASE}/`, {
    headers: auth(opToken),
    data: { procedureTypeId: pipeline.procedureType.id, companyId },
  });
  expect(procRes.status()).toBe(201);
  const procedure = await procRes.json();

  return { ...pipeline, procedure, operadorToken: opToken, companyId };
}
