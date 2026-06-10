# Scripts SQL — FLIT

## Modelo híbrido (EF Core + SQL embebido)

Las migraciones EF Core viven en `services/core-api/src/Flit.Infrastructure/Migrations/`.

| Capa | Mecanismo | Aplicación |
|------|-----------|------------|
| Shell (`identity`, `rbac`, `notifications`) | Migración EF `InitialFlitShell` | `pnpm migrate` o `MigrateAsync()` al arrancar |
| Trámites (#9437, #9438, …) | Migraciones EF con SQL embebido en `Migrations/Sql/` | Idem |
| Seeds del shell | `FlitV2Seeds` vía `POST /api/v1/dev/seed-admin` (DEV) | Tras migraciones |

**Canal principal:** `pnpm migrate` (equivale a `dotnet ef database update` en core-api, startup `Flit.Api`).
La cadena Npgsql se toma de `ConnectionStrings:Core` en `appsettings.Development.json` (o `ConnectionStrings__Core` en env).

Los scripts bajo `tramites/` son **espejo manual** del SQL embebido (útil para `psql` aislado o rollback puntual). Mantener sincronizados con `Migrations/Sql/`.

## Base de datos nueva o reiniciada

```powershell
pnpm docker:reset:infra
pnpm docker:up:infra
pnpm migrate
pnpm dev
```

Seeds DEV (admin local):

```http
POST /api/v1/dev/seed-admin?email=admin@flit.io
```

## Espejo manual — Trámites 2.0

Scripts bajo `tramites/` reflejan migraciones híbridas ya incluidas en EF:

| Script espejo | HU | Migración EF |
|---------------|-----|--------------|
| `tramites/9437-rgl01-procedures-config-rules.sql` | #9437 | `AddProceduresConfigRulesRgl01` |
| `tramites/9437-rgl01-procedures-config-rules-down.sql` | #9437 | Down de la anterior |
| `tramites/9438-rgl02-integrations-endpoint-call-log.sql` | #9438 | `AddIntegrationsEndpointCallLogRgl02` |
| `tramites/9409-procedures-config-parametrization50.sql` | #9408 #9409 #9410 | `AddProceduresConfigParametrization50` |
| `tramites/9409-procedures-config-parametrization50-down.sql` | #9408 #9409 #9410 | Down de la anterior |

```bash
# Solo si necesitas aplicar fuera de EF (debug / rollback manual):
psql "$DATABASE_URL" -f scripts/sql/tramites/9437-rgl01-procedures-config-rules.sql
psql "$DATABASE_URL" -f scripts/sql/tramites/9438-rgl02-integrations-endpoint-call-log.sql
psql "$DATABASE_URL" -f scripts/sql/tramites/9409-procedures-config-parametrization50.sql
```

## Schema shell legacy (`flit-shell-schema.sql`)

`flit-shell-schema.sql` se conserva como referencia y para `reset-local-database.ps1/.sh`.
En entornos con EF activo, **`pnpm migrate`** es la vía canónica (genera el mismo modelo vía `InitialFlitShell`).
