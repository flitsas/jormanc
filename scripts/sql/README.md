# Scripts SQL — FLIT 2.0 (esqueleto base)

Tras el reset, el backend arranca **sin migraciones ni esquema**. El modelo de
datos lo construyen las migraciones EF Core a medida que se implementan las
features.

## Modelo de migraciones (EF Core)

Las migraciones viven en `services/core-api/src/Flit.Infrastructure/Migrations/`
(directorio que se regenera al crear la primera migración).

| Capa | Mecanismo | Aplicación |
|------|-----------|------------|
| Features | Migraciones EF Core (con SQL embebido en `Migrations/Sql/` si aplica) | `pnpm migrate` o `MigrateAsync()` al arrancar |

**Canal principal:** `pnpm migrate` (equivale a `dotnet ef database update` en
core-api, startup `Flit.Api`). La cadena Npgsql se toma de `ConnectionStrings:Core`
en `appsettings.Development.json` (o `ConnectionStrings__Core` en env).

### Crear la primera migración

```bash
cd services/core-api
dotnet ef migrations add InitialCreate \
  --project src/Flit.Infrastructure \
  --startup-project src/Flit.Api
```

## Base de datos nueva o reiniciada

```powershell
pnpm docker:reset:infra
pnpm docker:up:infra
pnpm migrate
pnpm dev
```

`reset-local-database.ps1` / `.sh` solo levantan Postgres vacío; el esquema se
aplica con `pnpm migrate`.
