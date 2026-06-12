/**
 * Credenciales seed DEV — DevSeedService + DevQaDemoSeedService (Flit.Api).
 * VPS DEV: requiere Flit__SeedDemoData=true (o ASPNETCORE_ENVIRONMENT=Development).
 * URL DEV: https://dev.jormanc.flitsas.online
 */
export const QA_SEED = {
  email: 'admin@acme.com',
  password: 'Flit2026@Dev!',
  tenantSlug: 'acme',
} as const;

/** Operador tenant (admin sin superadmin SaaS). */
export const QA_OPERADOR = {
  email: 'operador@acme.com',
  password: QA_SEED.password,
  tenantSlug: QA_SEED.tenantSlug,
} as const;

export const API_BASE =
  process.env.PLAYWRIGHT_API_URL ?? 'http://localhost:4002/api/v1';
