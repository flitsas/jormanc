import { expect, test } from '@playwright/test';
import { loginAndGetToken, loginAsSuperAdmin, QA_TENANT_ADMIN } from '../fixtures/qa-auth.js';
import {
  auth,
  createDraftProcedure,
  PROCEDURES_BASE,
  PT_BASE,
  seedActorDefinition,
} from './procedures-helpers.js';

test.describe('HU #9785 — Actores natural, jurídica y copropietarios', () => {
  test('QA_TC01_TRAMITES_ACTORS - Actor natural persiste query_results RUNT', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { procedure, procedureType } = await createDraftProcedure(
      request,
      superToken,
      operadorToken,
    );

    const vendedor = await seedActorDefinition(request, superToken, procedureType.id, {
      role: 'vendedor',
      allowedNature: 'natural',
      minCount: 1,
      maxCount: 1,
      isRequired: false,
      orderIndex: 0,
    });

    const res = await request.post(`${PROCEDURES_BASE}/${procedure.id}/actors`, {
      headers: auth(operadorToken),
      data: {
        actorDefinitionId: vendedor.id,
        nature: 'natural',
        documentType: 'CC',
        documentNumber: '12345678',
      },
    });
    expect(res.status()).toBe(201);
    const body = await res.json();

    expect(body.actorId).toBeTruthy();
    expect(body.nature).toBe('natural');
    expect(Array.isArray(body.warnings)).toBe(true);
  });

  test('QA_TC02_TRAMITES_ACTORS - Actor jurídica crea representante legal hijo', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { procedure, procedureType } = await createDraftProcedure(
      request,
      superToken,
      operadorToken,
    );

    const comprador = await seedActorDefinition(request, superToken, procedureType.id, {
      role: 'comprador',
      allowedNature: 'ambas',
      minCount: 1,
      maxCount: 1,
      isRequired: false,
      orderIndex: 0,
    });

    const res = await request.post(`${PROCEDURES_BASE}/${procedure.id}/actors`, {
      headers: auth(operadorToken),
      data: {
        actorDefinitionId: comprador.id,
        nature: 'juridica',
        nit: '900123456',
      },
    });
    expect(res.status()).toBe(201);
    const body = await res.json();

    expect(body.actorId).toBeTruthy();
    expect(body.nature).toBe('juridica');
    expect(body.legalRepresentativeActorId).toBeTruthy();
  });

  test('QA_TC03_TRAMITES_ACTORS - Cuota 4to copropietario valida suma 100%', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { procedure, procedureType } = await createDraftProcedure(
      request,
      superToken,
      operadorToken,
    );

    const comprador = await seedActorDefinition(request, superToken, procedureType.id, {
      role: 'comprador',
      allowedNature: 'natural',
      minCount: 1,
      maxCount: 4,
      isRequired: false,
      orderIndex: 0,
    });

    const docs = ['11111111', '22222222', '33333333'];
    for (const doc of docs) {
      const add = await request.post(`${PROCEDURES_BASE}/${procedure.id}/actors`, {
        headers: auth(operadorToken),
        data: {
          actorDefinitionId: comprador.id,
          nature: 'natural',
          documentType: 'CC',
          documentNumber: doc,
          cuotaPct: 25,
        },
      });
      expect(add.status()).toBe(201);
    }

    const blocked = await request.post(`${PROCEDURES_BASE}/${procedure.id}/actors`, {
      headers: auth(operadorToken),
      data: {
        actorDefinitionId: comprador.id,
        nature: 'natural',
        documentType: 'CC',
        documentNumber: '44444444',
        cuotaPct: 26,
      },
    });
    expect(blocked.status()).toBe(422);
    const err = await blocked.json();
    expect(err.error).toBe('CUOTA_SUM_EXCEEDS_100');
    expect(err.current_sum).toBe(75);
    expect(err.proposed).toBe(26);

    const ok = await request.post(`${PROCEDURES_BASE}/${procedure.id}/actors`, {
      headers: auth(operadorToken),
      data: {
        actorDefinitionId: comprador.id,
        nature: 'natural',
        documentType: 'CC',
        documentNumber: '55555555',
        cuotaPct: 25,
      },
    });
    expect(ok.status()).toBe(201);
  });

  test('QA_TC04_TRAMITES_ACTORS - Caso Borde trámite inexistente', async ({ request }) => {
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const fakeId = '00000000-0000-0000-0000-000000000099';

    const res = await request.post(`${PROCEDURES_BASE}/${fakeId}/actors`, {
      headers: auth(operadorToken),
      data: {
        actorDefinitionId: '00000000-0000-0000-0000-000000000001',
        nature: 'natural',
        documentNumber: '12345678',
      },
    });

    expect(res.status()).toBe(404);
    const body = await res.json();
    expect(body.code).toBe('PROCEDURE_NOT_FOUND');
  });

  test('QA_TC05_TRAMITES_ACTORS - Validacion Contrato API AddActorResponse', async ({
    request,
  }) => {
    const superToken = await loginAsSuperAdmin(request);
    const operadorToken = await loginAndGetToken(request, QA_TENANT_ADMIN);
    const { procedure, procedureType } = await createDraftProcedure(
      request,
      superToken,
      operadorToken,
    );

    const vendedor = await seedActorDefinition(request, superToken, procedureType.id, {
      role: 'vendedor',
      allowedNature: 'natural',
      minCount: 1,
      maxCount: 1,
      isRequired: false,
      orderIndex: 0,
    });

    const res = await request.post(`${PROCEDURES_BASE}/${procedure.id}/actors`, {
      headers: auth(operadorToken),
      data: {
        actorDefinitionId: vendedor.id,
        nature: 'natural',
        documentType: 'CC',
        documentNumber: '87654321',
      },
    });
    expect(res.status()).toBe(201);
    const body = await res.json();

    expect(body).toMatchObject({
      actorId: expect.any(String),
      nature: 'natural',
    });
    expect(Array.isArray(body.warnings)).toBe(true);
  });
});
