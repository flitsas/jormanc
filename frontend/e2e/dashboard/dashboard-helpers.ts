import { expect } from '@playwright/test';
import { API_BASE } from '../fixtures/qa-seed.js';
import { auth, uniqueCompanyIds } from '../procedures/procedures-helpers.js';

export const DASHBOARD_BASE = `${API_BASE}/dashboard`;
export const COMPANIES_BASE = `${API_BASE}/admin/companies`;

export function wideDateRange() {
  return {
    from: '2026-01-01T00:00:00.000Z',
    to: '2026-12-31T23:59:59.999Z',
  };
}

export async function createIsolatedTenant(
  request: import('@playwright/test').APIRequestContext,
  superToken: string,
) {
  const { nit, name, tenantSlug } = uniqueCompanyIds();
  const res = await request.post(`${COMPANIES_BASE}/`, {
    headers: auth(superToken),
    data: { nit, name, tenantSlug },
  });
  expect(res.status()).toBe(201);
  const company = await res.json();
  return { company, tenantId: company.tenantId as string, tenantSlug };
}

export function expectSummaryShape(body: Record<string, unknown>) {
  expect(body).toHaveProperty('period');
  expect(body).toHaveProperty('summary');
  const summary = body.summary as Record<string, unknown>;
  expect(typeof summary.total).toBe('number');
  expect(Array.isArray(summary.byFamily)).toBe(true);
  expect(summary.byStatus).toMatchObject({
    draft: expect.any(Number),
    submitted: expect.any(Number),
    approved: expect.any(Number),
    rejected: expect.any(Number),
  });

  for (const row of summary.byFamily as Array<Record<string, unknown>>) {
    expect(['matricula_inicial', 'traspasos', 'otros']).toContain(row.family);
    expect(typeof row.count).toBe('number');
    expect(typeof row.pct).toBe('number');
    expect(row.byStatus).toBeTruthy();
  }
}
