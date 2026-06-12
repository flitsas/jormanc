import { HubConnectionBuilder } from '@microsoft/signalr';
import { expect, test } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import {
  loginAndGetToken,
  loginAsSuperAdmin,
  QA_TENANT_ADMIN,
} from '../fixtures/qa-auth.js';

const HUB_BASE = API_BASE.replace('/api/v1', '');
const auth = (token: string) => ({ Authorization: `Bearer ${token}` });

function decodeJwtPayload(token: string): { jti?: string; sub?: string } {
  const segment = token.split('.')[1];
  const json = Buffer.from(segment, 'base64url').toString('utf8');
  return JSON.parse(json) as { jti?: string; sub?: string };
}

async function getUserId(
  request: import('@playwright/test').APIRequestContext,
  token: string,
) {
  const me = await request.get(`${API_BASE}/auth/me`, { headers: auth(token) });
  expect(me.status()).toBe(200);
  return (await me.json()).id as string;
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

/** Login operador, PATCH roles por superadmin → revoca sesión activa del operador. */
async function revokeOperadorSessionViaRolePatch(
  request: import('@playwright/test').APIRequestContext,
) {
  const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
  const operadorId = await getUserId(request, operadorToken);

  const adminToken = await loginAsSuperAdmin(request);
  const adminRoleId = await getAdminRoleId(request, adminToken);

  const patch = await request.patch(`${API_BASE}/users/${operadorId}/roles`, {
    headers: auth(adminToken),
    data: { roles: [adminRoleId] },
  });
  expect(patch.status()).toBe(200);

  return { operadorToken, operadorId, adminToken };
}

test.describe('HU #9771 — Invalidación de sesión en tiempo real', () => {
  test('QA_TC01_IDENTIDAD_FUNCIONAL - PATCH roles invalida sesiones activas vía blacklist', async ({
    request,
  }) => {
    const { operadorToken } = await revokeOperadorSessionViaRolePatch(request);
    const payload = decodeJwtPayload(operadorToken);
    expect(payload.jti).toBeTruthy();

    const meBefore = await request.get(`${API_BASE}/auth/me`, {
      headers: auth(operadorToken),
    });
    expect(meBefore.status()).toBe(403);
    const body = await meBefore.json();
    expect(body.error).toBe('session_revoked');

    const freshToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const meFresh = await request.get(`${API_BASE}/auth/me`, {
      headers: auth(freshToken),
    });
    expect(meFresh.status()).toBe(200);
  });

  test('QA_TC02_IDENTIDAD_FUNCIONAL - SignalR notifica SessionRevoked al usuario afectado', async ({
    request,
  }) => {
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const operadorId = await getUserId(request, operadorToken);

    const eventPromise = new Promise<{ reason: string }>((resolve, reject) => {
      const timer = setTimeout(() => reject(new Error('SessionRevoked timeout')), 20_000);

      const connection = new HubConnectionBuilder()
        .withUrl(`${HUB_BASE}/hubs/session`, {
          accessTokenFactory: () => operadorToken,
        })
        .build();

      connection.on('SessionRevoked', (payload: { reason: string }) => {
        clearTimeout(timer);
        resolve(payload);
      });

      connection
        .start()
        .then(async () => {
          const adminToken = await loginAsSuperAdmin(request);
          const adminRoleId = await getAdminRoleId(request, adminToken);
          const patch = await request.patch(`${API_BASE}/users/${operadorId}/roles`, {
            headers: auth(adminToken),
            data: { roles: [adminRoleId] },
          });
          expect(patch.status()).toBe(200);
        })
        .catch((err: Error) => {
          clearTimeout(timer);
          reject(err);
        });

      test.info().attach('operadorId', { body: operadorId, contentType: 'text/plain' });
    });

    const event = await eventPromise;
    expect(event.reason).toBe('roles_changed');
  });

  test('QA_TC03_IDENTIDAD_FUNCIONAL - BlacklistRehydrationService carga JTIs al arrancar', async ({
    request,
  }) => {
    const health = await request.get(`${API_BASE}/health`);
    expect(health.status()).toBe(200);
    const healthBody = await health.json();
    expect(healthBody.status).toBe('ok');

    const { operadorToken } = await revokeOperadorSessionViaRolePatch(request);
    const { jti } = decodeJwtPayload(operadorToken);
    expect(jti).toMatch(/^[0-9a-f]{32}$/i);

    const blocked = await request.get(`${API_BASE}/auth/me`, {
      headers: auth(operadorToken),
    });
    expect(blocked.status()).toBe(403);
    expect((await blocked.json()).error).toBe('session_revoked');

    const newToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const newJti = decodeJwtPayload(newToken).jti;
    expect(newJti).not.toBe(jti);

    const allowed = await request.get(`${API_BASE}/auth/me`, {
      headers: auth(newToken),
    });
    expect(allowed.status()).toBe(200);
  });

  test('QA_TC04_IDENTIDAD_FUNCIONAL - Caso Borde Multitenant', async ({ request }) => {
    const unauth = await request.patch(
      `${API_BASE}/users/00000000-0000-0000-0000-000000000099/roles`,
      { data: { roles: [] } },
    );
    expect(unauth.status()).toBe(401);

    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const assign = await request.patch(
      `${API_BASE}/users/00000000-0000-0000-0000-000000000099/roles`,
      { headers: auth(operadorToken), data: { roles: [] } },
    );
    expect(assign.status()).toBe(404);
    expect((await assign.json()).code).toBe('USER_NOT_FOUND');

    const { operadorToken: revokedToken } = await revokeOperadorSessionViaRolePatch(request);
    const me = await request.get(`${API_BASE}/auth/me`, { headers: auth(revokedToken) });
    expect(me.status()).toBe(403);
    expect((await me.json()).error).toBe('session_revoked');
  });

  test('QA_TC05_IDENTIDAD_FUNCIONAL - Validacion Contrato API', async ({ request }) => {
    const { operadorToken } = await revokeOperadorSessionViaRolePatch(request);

    const me = await request.get(`${API_BASE}/auth/me`, { headers: auth(operadorToken) });
    expect(me.status()).toBe(403);
    expect(await me.json()).toEqual({ error: 'session_revoked' });

    const loginRes = await request.post(`${API_BASE}/auth/login`, {
      data: {
        email: QA_TENANT_ADMIN.email,
        password: QA_TENANT_ADMIN.password,
        tenantSlug: QA_TENANT_ADMIN.tenantSlug,
      },
    });
    expect(loginRes.status()).toBe(200);
    const loginBody = await loginRes.json();
    expect(loginBody).toMatchObject({
      accessToken: expect.any(String),
      expiresIn: 900,
      user: expect.objectContaining({
        email: QA_TENANT_ADMIN.email,
        roles: expect.any(Array),
      }),
    });

    const payload = decodeJwtPayload(loginBody.accessToken as string);
    expect(payload.jti).toMatch(/^[0-9a-f]{32}$/i);
    expect(payload.sub).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
  });
});
