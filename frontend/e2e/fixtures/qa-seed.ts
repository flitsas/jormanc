/** Credenciales seed DEV — DevSeedService (Flit.Api). */
export const QA_SEED = {
  email: 'admin@acme.com',
  password: 'Flit2026@Dev!',
  tenantSlug: 'acme',
} as const;

export const API_BASE =
  process.env.PLAYWRIGHT_API_URL ?? 'http://localhost:4002/api/v1';
