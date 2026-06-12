import { expect, test } from '@playwright/test';
import { loginAndGetToken, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import { auth } from '../procedures/procedures-helpers.js';
import { DASHBOARD_BASE, wideDateRange } from './dashboard-helpers.js';

test.describe('HU #9795 — Dashboard export API', () => {
  test('QA_TC01_DASHBOARD_EXPORT - Export Excel retorna xlsx válido', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { from, to } = wideDateRange();

    const res = await request.get(`${DASHBOARD_BASE}/export/excel`, {
      headers: auth(token),
      params: { from, to, format: 'xlsx' },
    });
    expect(res.status()).toBe(200);
    expect(res.headers()['content-type']).toContain(
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    );
    const buffer = await res.body();
    expect(buffer.byteLength).toBeGreaterThan(100);
    expect(buffer.subarray(0, 2).toString()).toBe('PK');
  });

  test('QA_TC02_DASHBOARD_EXPORT - Export PDF retorna application/pdf', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { from, to } = wideDateRange();

    const res = await request.get(`${DASHBOARD_BASE}/export/pdf`, {
      headers: auth(token),
      params: { from, to, include_charts: 'true', families: 'traspasos' },
    });
    expect(res.status()).toBe(200);
    expect(res.headers()['content-type']).toContain('application/pdf');
    const pdf = await res.body();
    expect(pdf.subarray(0, 4).toString()).toBe('%PDF');
  });

  test('QA_TC03_DASHBOARD_EXPORT - GET top-users retorna ranking', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { from, to } = wideDateRange();

    const res = await request.get(`${DASHBOARD_BASE}/top-users`, {
      headers: auth(token),
      params: { from, to },
    });
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body.data)).toBe(true);
    for (const row of body.data) {
      expect(row.userId).toMatch(
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
      );
      expect(typeof row.fullName).toBe('string');
      expect(typeof row.count).toBe('number');
      expect(typeof row.pctOfTotal).toBe('number');
    }
  });

  test('QA_TC04_DASHBOARD_EXPORT - family inválida retorna 400', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { from, to } = wideDateRange();

    const res = await request.get(`${DASHBOARD_BASE}/procedures`, {
      headers: auth(token),
      params: { from, to, family: 'invalida' },
    });
    expect(res.status()).toBe(400);
    expect((await res.json()).code).toBe('VALIDATION_ERROR');
  });

  test('QA_TC05_DASHBOARD_EXPORT - format csv alternativo en Excel export', async ({ request }) => {
    const token = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { from, to } = wideDateRange();

    const res = await request.get(`${DASHBOARD_BASE}/export/excel`, {
      headers: auth(token),
      params: { from, to, format: 'csv' },
    });
    expect(res.status()).toBe(200);
    const text = (await res.body()).toString('utf-8');
    expect(text.length).toBeGreaterThan(0);
    expect(text.split('\n').length).toBeGreaterThanOrEqual(1);
  });
});
