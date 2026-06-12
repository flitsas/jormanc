import { expect, test } from '@playwright/test';
import { fetchLatestDevEmailToken } from '../fixtures/qa-dev-mail.js';
import { API_BASE, QA_SEED } from '../fixtures/qa-seed.js';
import { loginAsSuperAdmin, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';

const auth = (token: string) => ({ Authorization: `Bearer ${token}` });
const STRONG_PASSWORD = 'Flit2026@Dev!';
const NEW_PASSWORD = 'Flit2026@Reset!';

function uniqueEmail(prefix: string) {
  return `${prefix}-${Date.now().toString().slice(-8)}@acme.com`;
}

async function getAdminRoleId(
  request: import('@playwright/test').APIRequestContext,
  adminToken: string,
) {
  const rolesRes = await request.get(`${API_BASE}/roles`, { headers: auth(adminToken) });
  expect(rolesRes.status()).toBe(200);
  const roles = (await rolesRes.json()) as { id: string; slug: string }[];
  const adminRole = roles.find((r) => r.slug === 'admin');
  expect(adminRole).toBeTruthy();
  return adminRole!.id;
}

test.describe('HU #9772 — Onboarding invitación y reset de contraseña', () => {
  test('QA_TC01_IDENTIDAD_ONBOARDING - Flujo de invitación completo', async ({ request }) => {
    const adminToken = await loginAsSuperAdmin(request);
    const inviteEmail = uniqueEmail('invitado-e2e');
    const adminRoleId = await getAdminRoleId(request, adminToken);

    const createRes = await request.post(`${API_BASE}/invitations`, {
      headers: auth(adminToken),
      data: { email: inviteEmail, roleIds: [adminRoleId] },
    });
    expect(createRes.status()).toBe(201);
    const created = await createRes.json();
    expect(created.status).toBe('pending');
    expect(created.email).toBe(inviteEmail);

    const inviteToken = await fetchLatestDevEmailToken(request, inviteEmail);

    const validateRes = await request.get(`${API_BASE}/invitations/${inviteToken}/validate`);
    expect(validateRes.status()).toBe(200);
    const validated = await validateRes.json();
    expect(validated.email).toBe(inviteEmail);
    expect(validated.roleSlugs).toContain('admin');

    const acceptRes = await request.post(`${API_BASE}/invitations/${inviteToken}/accept`, {
      data: { fullName: 'Usuario Invitado E2E', password: STRONG_PASSWORD },
    });
    expect(acceptRes.status()).toBe(201);
    const accepted = await acceptRes.json();
    expect(accepted.accessToken).toBeTruthy();
    expect(accepted.expiresIn).toBe(900);
    expect(accepted.user.email).toBe(inviteEmail);
    expect(accepted.user.roles).toContain('admin');

    const me = await request.get(`${API_BASE}/auth/me`, {
      headers: auth(accepted.accessToken as string),
    });
    expect(me.status()).toBe(200);
    expect((await me.json()).email).toBe(inviteEmail);
  });

  test('QA_TC02_IDENTIDAD_ONBOARDING - Token de invitación expirado', async ({ request }) => {
    const adminToken = await loginAsSuperAdmin(request);
    const inviteEmail = uniqueEmail('expirado-e2e');
    const adminRoleId = await getAdminRoleId(request, adminToken);

    const createRes = await request.post(`${API_BASE}/invitations`, {
      headers: auth(adminToken),
      data: { email: inviteEmail, roleIds: [adminRoleId] },
    });
    expect(createRes.status()).toBe(201);

    const inviteToken = await fetchLatestDevEmailToken(request, inviteEmail);
    const expireRes = await request.post(`${API_BASE}/dev/invitations/${inviteToken}/expire`);
    expect(expireRes.status()).toBe(204);

    const validateRes = await request.get(`${API_BASE}/invitations/${inviteToken}/validate`);
    expect(validateRes.status()).toBe(400);
    expect((await validateRes.json()).code).toBe('INVITATION_EXPIRED');

    const acceptRes = await request.post(`${API_BASE}/invitations/${inviteToken}/accept`, {
      data: { fullName: 'No Debe Crearse', password: STRONG_PASSWORD },
    });
    expect(acceptRes.status()).toBe(400);
    expect((await acceptRes.json()).code).toBe('INVITATION_EXPIRED');
  });

  test('QA_TC03_IDENTIDAD_ONBOARDING - Flujo de reset de contraseña', async ({ request }) => {
    const forgotRes = await request.post(`${API_BASE}/auth/forgot-password`, {
      data: { email: QA_TENANT_ADMIN.email, tenantSlug: QA_SEED.tenantSlug },
    });
    expect(forgotRes.status()).toBe(204);

    const resetToken = await fetchLatestDevEmailToken(request, QA_TENANT_ADMIN.email);

    const resetRes = await request.post(`${API_BASE}/auth/reset-password`, {
      data: { token: resetToken, newPassword: NEW_PASSWORD },
    });
    expect(resetRes.status()).toBe(204);

    const loginNew = await request.post(`${API_BASE}/auth/login`, {
      data: {
        email: QA_TENANT_ADMIN.email,
        password: NEW_PASSWORD,
        tenantSlug: QA_SEED.tenantSlug,
      },
    });
    expect(loginNew.status()).toBe(200);

    const reuse = await request.post(`${API_BASE}/auth/reset-password`, {
      data: { token: resetToken, newPassword: 'OtraClave2026!' },
    });
    expect(reuse.status()).toBe(400);
    expect((await reuse.json()).code).toBe('RESET_TOKEN_ALREADY_USED');

    await request.post(`${API_BASE}/auth/forgot-password`, {
      data: { email: QA_TENANT_ADMIN.email, tenantSlug: QA_SEED.tenantSlug },
    });
    const restoreToken = await fetchLatestDevEmailToken(request, QA_TENANT_ADMIN.email);
    await request.post(`${API_BASE}/auth/reset-password`, {
      data: { token: restoreToken, newPassword: QA_TENANT_ADMIN.password },
    });
  });

  test('QA_TC04_IDENTIDAD_ONBOARDING - Caso Borde Multitenant', async ({ request }) => {
    const unauth = await request.post(`${API_BASE}/invitations`, {
      data: { email: uniqueEmail('sin-auth'), roleIds: [] },
    });
    expect(unauth.status()).toBe(401);

    const validate = await request.get(`${API_BASE}/invitations/token-inexistente-e2e/validate`);
    expect(validate.status()).toBe(404);
    expect((await validate.json()).code).toBe('INVITATION_NOT_FOUND');

    const ghostForgot = await request.post(`${API_BASE}/auth/forgot-password`, {
      data: { email: 'noexiste@acme.com', tenantSlug: 'tenant-inexistente' },
    });
    expect(ghostForgot.status()).toBe(204);
  });

  test('QA_TC05_IDENTIDAD_ONBOARDING - Validacion Contrato API', async ({ request }) => {
    const adminToken = await loginAsSuperAdmin(request);
    const inviteEmail = uniqueEmail('contrato-e2e');
    const adminRoleId = await getAdminRoleId(request, adminToken);

    const createRes = await request.post(`${API_BASE}/invitations`, {
      headers: auth(adminToken),
      data: { email: inviteEmail, roleIds: [adminRoleId] },
    });
    expect(createRes.status()).toBe(201);
    const created = await createRes.json();
    expect(created).toMatchObject({
      id: expect.stringMatching(
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
      ),
      email: inviteEmail,
      status: 'pending',
      expiresAt: expect.any(String),
    });

    const inviteToken = await fetchLatestDevEmailToken(request, inviteEmail);
    const validateRes = await request.get(`${API_BASE}/invitations/${inviteToken}/validate`);
    expect(validateRes.status()).toBe(200);
    const validated = await validateRes.json();
    expect(validated).toMatchObject({
      email: inviteEmail,
      tenantId: expect.stringMatching(
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
      ),
      tenantName: expect.any(String),
      roleSlugs: expect.arrayContaining(['admin']),
    });

    const invalidForgot = await request.post(`${API_BASE}/auth/forgot-password`, {
      data: { email: '', tenantSlug: '' },
    });
    expect(invalidForgot.status()).toBe(400);
    expect((await invalidForgot.json()).code).toBe('VALIDATION_ERROR');
  });
});
