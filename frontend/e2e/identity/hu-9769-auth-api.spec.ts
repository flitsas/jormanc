import { expect, test } from '@playwright/test';
import { API_BASE, QA_SEED } from '../fixtures/qa-seed.js';

test.describe('HU #9769 — Identidad Auth API', () => {
  test('QA_TC01_IDENTIDAD_AUTH - Login exitoso emite JWT y registra sesion', async ({
    request,
  }) => {
    const res = await request.post(`${API_BASE}/auth/login`, {
      data: {
        email: QA_SEED.email,
        password: QA_SEED.password,
        tenantSlug: QA_SEED.tenantSlug,
      },
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(body.accessToken).toBeTruthy();
    expect(body.expiresIn).toBe(900);
    expect(body.user?.email).toBe(QA_SEED.email);
    expect(Array.isArray(body.user?.roles)).toBe(true);
  });

  test('QA_TC02_IDENTIDAD_AUTH - Credenciales incorrectas no crean sesion', async ({
    request,
  }) => {
    const res = await request.post(`${API_BASE}/auth/login`, {
      data: {
        email: QA_SEED.email,
        password: 'WrongPassword!',
        tenantSlug: QA_SEED.tenantSlug,
      },
    });
    expect(res.status()).toBe(401);
    const body = await res.json();
    expect(body.code).toMatch(/INVALID_CREDENTIALS/i);
  });

  test('QA_TC03_IDENTIDAD_AUTH - Perfil del usuario autenticado', async ({ request }) => {
    const login = await request.post(`${API_BASE}/auth/login`, {
      data: {
        email: QA_SEED.email,
        password: QA_SEED.password,
        tenantSlug: QA_SEED.tenantSlug,
      },
    });
    expect(login.status()).toBe(200);
    const { accessToken: token } = await login.json();

    const me = await request.get(`${API_BASE}/auth/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(me.status()).toBe(200);
    const profile = await me.json();
    expect(profile.email).toBe(QA_SEED.email);
    expect(profile.tenantId).toBeTruthy();
    expect(Array.isArray(profile.permissions)).toBe(true);
  });

  test('QA_TC04_IDENTIDAD_AUTH - Caso Borde Multitenant', async ({ request }) => {
    const res = await request.post(`${API_BASE}/auth/login`, {
      data: {
        email: QA_SEED.email,
        password: QA_SEED.password,
        tenantSlug: 'tenant-inexistente',
      },
    });
    expect(res.status()).toBe(401);
    const body = await res.json();
    expect(body.code).toMatch(/INVALID_CREDENTIALS/i);
  });

  test('QA_TC05_IDENTIDAD_AUTH - Validacion Contrato API', async ({ request }) => {
    const login = await request.post(`${API_BASE}/auth/login`, {
      data: {
        email: QA_SEED.email,
        password: QA_SEED.password,
        tenantSlug: QA_SEED.tenantSlug,
      },
    });
    expect(login.status()).toBe(200);
    const { accessToken: token } = await login.json();

    const me = await request.get(`${API_BASE}/auth/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(me.status()).toBe(200);
    const profile = await me.json();

    expect(profile.id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
    expect(profile.name).toBeTruthy();
    expect(profile.email).toBe(QA_SEED.email);
    expect(profile.tenantId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
    expect(profile.tenantName).toBeTruthy();
    expect(Array.isArray(profile.roles)).toBe(true);
    expect(profile.roles.length).toBeGreaterThan(0);
    expect(Array.isArray(profile.permissions)).toBe(true);
    for (const perm of profile.permissions as string[]) {
      expect(perm).toMatch(/^[a-z0-9_.:-]+$/i);
    }
  });
});
