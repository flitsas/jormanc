import { expect, test } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import {
  loginAndGetToken,
  loginAsSuperAdmin,
  QA_TENANT_ADMIN,
} from '../fixtures/qa-auth.js';

const COMPANIES_BASE = `${API_BASE}/admin/companies`;

function uniqueCompanyIds(prefix = 'e2e-cfg') {
  const suffix = Date.now().toString().slice(-7);
  return {
    nit: `902${suffix}`,
    name: `E2E Config ${suffix}`,
    tenantSlug: `${prefix}-${suffix}`,
  };
}

async function createCompany(
  request: import('@playwright/test').APIRequestContext,
  token: string,
  prefix?: string,
) {
  const { nit, name, tenantSlug } = uniqueCompanyIds(prefix);
  const res = await request.post(`${COMPANIES_BASE}/`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { nit, name, tenantSlug },
  });
  expect(res.status()).toBe(201);
  return res.json() as Promise<{ id: string; tenantId: string }>;
}

async function getUserIdFromMe(
  request: import('@playwright/test').APIRequestContext,
  token: string,
) {
  const me = await request.get(`${API_BASE}/auth/me`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(me.status()).toBe(200);
  const profile = await me.json();
  return profile.id as string;
}

test.describe('HU #9776 — Matriz firmas, excepciones y OT habilitadas', () => {
  test('QA_TC01_COMPANIAS_COMPANIAS - Actualizacion de matriz de firmas por actor', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const company = await createCompany(request, token, 'e2e-sig');

    const putRes = await request.put(
      `${COMPANIES_BASE}/${company.id}/config/signature-matrix`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          entries: [
            { actorRole: 'vendedor', signatureType: 'identidad_digital', isActive: true },
            { actorRole: 'comprador', signatureType: 'firma_pantalla', isActive: true },
          ],
        },
      },
    );
    expect(putRes.status()).toBe(200);
    const matrix = await putRes.json();
    expect(matrix).toHaveLength(2);
    expect(matrix[0]).toMatchObject({
      companyId: company.id,
      actorRole: 'vendedor',
      signatureType: 'identidad_digital',
      isActive: true,
    });

    const getRes = await request.get(
      `${COMPANIES_BASE}/${company.id}/config/signature-matrix`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(getRes.status()).toBe(200);
    const fetched = await getRes.json();
    expect(fetched).toHaveLength(2);
    expect(
      fetched.some(
        (e: { actorRole: string; signatureType: string }) =>
          e.actorRole === 'comprador' && e.signatureType === 'firma_pantalla',
      ),
    ).toBe(true);
  });

  test('QA_TC02_COMPANIAS_COMPANIAS - Agregar y eliminar usuario de lista blanca (only_own_vehicles)', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const company = await createCompany(request, token, 'e2e-exc');
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const operadorUserId = await getUserIdFromMe(request, operadorToken);

    const postRes = await request.post(
      `${COMPANIES_BASE}/${company.id}/user-exceptions`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: { userId: operadorUserId },
      },
    );
    expect(postRes.status()).toBe(201);
    const created = await postRes.json();
    expect(created.companyId).toBe(company.id);
    expect(created.userId).toBe(operadorUserId);
    expect(created.addedAt).toBeTruthy();

    const dupRes = await request.post(
      `${COMPANIES_BASE}/${company.id}/user-exceptions`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: { userId: operadorUserId },
      },
    );
    expect(dupRes.status()).toBe(409);
    expect((await dupRes.json()).code).toBe('COMPANY_USER_EXCEPTION_ALREADY_EXISTS');

    const listRes = await request.get(
      `${COMPANIES_BASE}/${company.id}/user-exceptions`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(listRes.status()).toBe(200);
    const list = await listRes.json();
    expect(list.some((e: { userId: string }) => e.userId === operadorUserId)).toBe(true);

    const delRes = await request.delete(
      `${COMPANIES_BASE}/${company.id}/user-exceptions/${operadorUserId}`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(delRes.status()).toBe(204);

    const afterDel = await request.get(
      `${COMPANIES_BASE}/${company.id}/user-exceptions`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(afterDel.status()).toBe(200);
    const remaining = await afterDel.json();
    expect(remaining.some((e: { userId: string }) => e.userId === operadorUserId)).toBe(false);
  });

  test('QA_TC03_COMPANIAS_COMPANIAS - OT habilitadas por tenant persisten y se consultan', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);
    const company = await createCompany(request, token, 'e2e-ot');

    const putRes = await request.put(
      `${COMPANIES_BASE}/${company.id}/config/ot-enabled`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          entries: [
            { otSlug: 'simit-bogota', procedureFamily: 'matricula_inicial', isEnabled: true },
            { otSlug: 'runt-medellin', procedureFamily: 'traspasos', isEnabled: false },
          ],
        },
      },
    );
    expect(putRes.status()).toBe(200);
    const otList = await putRes.json();
    expect(otList).toHaveLength(2);
    expect(otList[0].otSlug).toBe('simit-bogota');
    expect(otList[0].procedureFamily).toBe('matricula_inicial');

    const getRes = await request.get(
      `${COMPANIES_BASE}/${company.id}/config/ot-enabled`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect(getRes.status()).toBe(200);
    const fetched = await getRes.json();
    expect(fetched).toHaveLength(2);
    const traspaso = fetched.find(
      (e: { procedureFamily: string }) => e.procedureFamily === 'traspasos',
    );
    expect(traspaso.isEnabled).toBe(false);
  });

  test('QA_TC04_COMPANIAS_COMPANIAS - Caso Borde Multitenant', async ({ request }) => {
    const tenantToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { nit, name, tenantSlug } = uniqueCompanyIds('e2e-mt');
    const create = await request.post(`${COMPANIES_BASE}/`, {
      headers: { Authorization: `Bearer ${tenantToken}` },
      data: { nit, name, tenantSlug },
    });
    expect(create.status()).toBe(403);

    const fakeId = '00000000-0000-0000-0000-000000000099';
    const sigPut = await request.put(
      `${COMPANIES_BASE}/${fakeId}/config/signature-matrix`,
      {
        headers: { Authorization: `Bearer ${tenantToken}` },
        data: {
          entries: [{ actorRole: 'vendedor', signatureType: 'identidad_digital' }],
        },
      },
    );
    expect(sigPut.status()).toBe(403);
    expect((await sigPut.json()).code).toBe('FORBIDDEN');
  });

  test('QA_TC05_COMPANIAS_COMPANIAS - Validacion Contrato API', async ({ request }) => {
    const token = await loginAsSuperAdmin(request);
    const company = await createCompany(request, token, 'e2e-ctr');

    const invalidSig = await request.put(
      `${COMPANIES_BASE}/${company.id}/config/signature-matrix`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          entries: [{ actorRole: 'invalid_role', signatureType: 'identidad_digital' }],
        },
      },
    );
    expect(invalidSig.status()).toBe(400);
    expect((await invalidSig.json()).code).toBe('COMPANY_INVALID_ACTOR_ROLE');

    const validSig = await request.put(
      `${COMPANIES_BASE}/${company.id}/config/signature-matrix`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          entries: [
            { actorRole: 'representante_legal', signatureType: 'preasignada', isActive: true },
          ],
        },
      },
    );
    expect(validSig.status()).toBe(200);
    const entry = (await validSig.json())[0];
    expect(entry).toMatchObject({
      id: expect.stringMatching(
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
      ),
      companyId: company.id,
      actorRole: 'representante_legal',
      signatureType: 'preasignada',
      isActive: true,
    });

    const invalidOt = await request.put(
      `${COMPANIES_BASE}/${company.id}/config/ot-enabled`,
      {
        headers: { Authorization: `Bearer ${token}` },
        data: {
          entries: [{ otSlug: 'test-ot', procedureFamily: 'invalid_family' }],
        },
      },
    );
    expect(invalidOt.status()).toBe(400);
    expect((await invalidOt.json()).code).toBe('COMPANY_INVALID_PROCEDURE_FAMILY');
  });
});
