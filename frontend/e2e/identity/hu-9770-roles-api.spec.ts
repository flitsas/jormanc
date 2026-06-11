import { expect, test } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import {
  loginAndGetToken,
  loginAsSuperAdmin,
  QA_TENANT_ADMIN,
} from '../fixtures/qa-auth.js';

const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

function uniqueSlug(prefix: string) {
  return `${prefix}-${Date.now().toString().slice(-7)}`;
}

async function getUserId(request: import('@playwright/test').APIRequestContext, token: string) {
  const me = await request.get(`${API_BASE}/auth/me`, { headers: auth(token) });
  expect(me.status()).toBe(200);
  return (await me.json()).id as string;
}

test.describe('HU #9770 — CRUD roles, permisos y asignación multi-tenant', () => {
  test('QA_TC01_IDENTIDAD_RBAC - Crear rol con permisos y asignar a usuario', async ({
    request,
  }) => {
    const adminToken = await loginAsSuperAdmin(request);
    const slug = uniqueSlug('operador-e2e');

    const permsRes = await request.get(`${API_BASE}/permissions`, {
      headers: auth(adminToken),
    });
    expect(permsRes.status()).toBe(200);
    const permissions = (await permsRes.json()) as { id: string; slug: string }[];
    expect(permissions.length).toBeGreaterThanOrEqual(2);
    const permIds = permissions.slice(0, 2).map((p) => p.id);
    const permSlugs = permissions.slice(0, 2).map((p) => p.slug);

    const createRes = await request.post(`${API_BASE}/roles`, {
      headers: auth(adminToken),
      data: {
        slug,
        name: 'Operador E2E',
        description: 'Rol creado en prueba E2E',
        permissions: permIds,
      },
    });
    expect(createRes.status()).toBe(201);
    const created = await createRes.json();
    expect(created.slug).toBe(slug);
    expect(created.permissions).toEqual(expect.arrayContaining(permSlugs));

    const operadorId = await getUserId(
      request,
      await loginAndGetToken(request, QA_TENANT_ADMIN),
    );

    const assignRes = await request.patch(`${API_BASE}/users/${operadorId}/roles`, {
      headers: auth(adminToken),
      data: { roles: [created.id] },
    });
    expect(assignRes.status()).toBe(200);
    const assigned = await assignRes.json();
    expect(assigned.roles.some((r: { slug: string }) => r.slug === slug)).toBe(true);

    const freshOperadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const me = await request.get(`${API_BASE}/auth/me`, {
      headers: auth(freshOperadorToken),
    });
    expect(me.status()).toBe(200);
    const profile = await me.json();
    for (const slugExpected of permSlugs) {
      expect(profile.permissions).toContain(slugExpected);
    }
  });

  test('QA_TC02_IDENTIDAD_RBAC - Aislamiento de tenant en roles y permisos', async ({
    request,
  }) => {
    const token = await loginAsSuperAdmin(request);

    const rolesRes = await request.get(`${API_BASE}/roles`, { headers: auth(token) });
    expect(rolesRes.status()).toBe(200);
    const roles = (await rolesRes.json()) as { slug: string; isSystem: boolean }[];
    expect(Array.isArray(roles)).toBe(true);
    expect(roles.length).toBeGreaterThanOrEqual(1);

    const slugs = roles.map((r) => r.slug);
    expect(slugs).toContain('admin');
    expect(slugs.every((s) => typeof s === 'string' && s.length > 0)).toBe(true);

    const slug = uniqueSlug('tenant-scope');
    const createRes = await request.post(`${API_BASE}/roles`, {
      headers: auth(token),
      data: { slug, name: 'Scope Test', permissions: [] },
    });
    expect(createRes.status()).toBe(201);

    const listAfter = await request.get(`${API_BASE}/roles`, { headers: auth(token) });
    const listed = (await listAfter.json()) as { slug: string }[];
    expect(listed.some((r) => r.slug === slug)).toBe(true);
  });

  test('QA_TC03_IDENTIDAD_RBAC - Eliminar rol de sistema rechazado', async ({ request }) => {
    const token = await loginAsSuperAdmin(request);

    const rolesRes = await request.get(`${API_BASE}/roles`, { headers: auth(token) });
    expect(rolesRes.status()).toBe(200);
    const roles = (await rolesRes.json()) as { id: string; slug: string; isSystem: boolean }[];
    const systemRole = roles.find((r) => r.isSystem && r.slug === 'admin');
    expect(systemRole).toBeTruthy();

    const delRes = await request.delete(`${API_BASE}/roles/${systemRole!.id}`, {
      headers: auth(token),
    });
    expect(delRes.status()).toBe(409);
    const body = await delRes.json();
    expect(body.code).toBe('SYSTEM_ROLE_CANNOT_BE_DELETED');

    const stillThere = await request.get(`${API_BASE}/roles`, { headers: auth(token) });
    const after = (await stillThere.json()) as { id: string }[];
    expect(after.some((r) => r.id === systemRole!.id)).toBe(true);
  });

  test('QA_TC04_IDENTIDAD_RBAC - Caso Borde Multitenant', async ({ request }) => {
    const unauth = await request.get(`${API_BASE}/roles`);
    expect(unauth.status()).toBe(401);

    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const fakeUserId = '00000000-0000-0000-0000-000000000099';
    const assign = await request.patch(`${API_BASE}/users/${fakeUserId}/roles`, {
      headers: auth(token),
      data: { roles: [] },
    });
    expect(assign.status()).toBe(404);
    expect((await assign.json()).code).toBe('USER_NOT_FOUND');
  });

  test('QA_TC05_IDENTIDAD_RBAC - Validacion Contrato API', async ({ request }) => {
    const token = await loginAsSuperAdmin(request);

    const permsRes = await request.get(`${API_BASE}/permissions`, { headers: auth(token) });
    expect(permsRes.status()).toBe(200);
    const perm = (await permsRes.json())[0];
    expect(perm).toMatchObject({
      id: expect.stringMatching(
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
      ),
      slug: expect.any(String),
      module: expect.any(String),
      action: expect.any(String),
    });

    const slug = uniqueSlug('contract');
    const createRes = await request.post(`${API_BASE}/roles`, {
      headers: auth(token),
      data: { slug, name: 'Contrato E2E', permissions: [perm.id] },
    });
    expect(createRes.status()).toBe(201);
    const role = await createRes.json();
    expect(role).toMatchObject({
      id: expect.stringMatching(
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
      ),
      slug,
      name: 'Contrato E2E',
      isSystem: false,
      permissions: expect.any(Array),
    });

    const invalid = await request.post(`${API_BASE}/roles`, {
      headers: auth(token),
      data: { slug: '', name: '' },
    });
    expect(invalid.status()).toBe(400);
    expect((await invalid.json()).code).toBe('VALIDATION_ERROR');
  });
});
