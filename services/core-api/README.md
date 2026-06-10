# services/core-api — .NET 10 / C# 14

> **Estado:** scaffold mínimo (Fase 4 del MIGRATION_PLAN).
> **ADR base:** [ADR-0002 §8.1 + Decision 5 + §5 stack tabla core-api](../../docs/decisions/ADR-0002-arquitectura-microservicios-2026.md).
> **Agente:** [`core-dotnet`](../../.ai/agents/core-dotnet.md).

## Lo que está en este scaffold (PR Fase 4)

- `global.json` fijando .NET 10.0.300 con rollForward `latestPatch`.
- `Directory.Build.props` con AOT, Nullable, TreatWarningsAsErrors, InvariantGlobalization.
- `Directory.Packages.props` con Central Package Management.
- `Tramites.slnx` (nuevo formato XML de solución de .NET 10).
- `src/Tramites.SharedKernel/`: `Result<TValue, TError>` y `IClock` / `SystemClock`.
- `src/Tramites.Modules.Tramites/Domain/`: aggregate root `Tramite` con factory `Crear(numeroRadicado, IClock)` que retorna `Result`. UUIDv7 para `Id`.
- `src/Tramites.Api/`: Minimal API con endpoint `GET /api/v1/health` (`AppJsonSerializerContext` para AOT). Serilog. OpenTelemetry con OTLP exporter (configurable por `OTEL_EXPORTER_OTLP_ENDPOINT`).
- `tests/Tramites.Api.Tests/`: xUnit v3 + FluentAssertions. Pruebas de factory de `Tramite` (4 tests).
- `tests/Tramites.ArchTests/`: NetArchTest 1.4 con 3 reglas iniciales (Domain ↛ Adapters, Domain ↛ AspNetCore, SharedKernel ↛ otros módulos).
- `Dockerfile` multi-stage AOT (sdk 10.0 → runtime-deps:10.0-alpine).

## Lo que NO está aún (TODO Fase 4 ampliada / Fase 9)

- Módulos: `Tramites.Modules.Documentos`, `Tramites.Modules.Integraciones` (Dian/Runt/Rues), `Tramites.Modules.Identity`, `Tramites.Modules.Notificaciones`. Crear cuando se implementen.
- Wolverine 3.x (mediator + outbox). Pendiente porque añade ~6 paquetes y patrón handler que se incluye con el primer caso de uso real.
- EF Core 10 + Dapper. Sin DB en este scaffold; se añaden con el primer módulo persistente.
- FluentValidation 12, OpenIddict 6, Refit, Mapster. Se añaden cuando aparezca el primer caso de uso que los requiera.
- Las 6 reglas inviolables del ADR-0002 §8.1 — solo 3 implementadas en `ArchitectureRulesTests`. Las restantes (Features no se importan entre sí, módulos comunican por eventos, Ports en módulo dueño, Adapters no exponen tipos infra) se activan cuando exista más de 1 módulo y vertical slices.
- Workflow `core-api.yml` en `.github/workflows/`. **Lo creo en Fase 8** (junto con los demás workflows nuevos).
- Testcontainers para tests de integración. Se añaden con el primer test que requiera DB.

## Comandos típicos

```bash
cd services/core-api

# Restaurar
dotnet restore

# Build
dotnet build

# Tests
dotnet test

# AOT publish (Linux x64)
dotnet publish src/Tramites.Api -c Release -r linux-x64

# Run en local
dotnet run --project src/Tramites.Api
# → http://localhost:5xxx/api/v1/health

# Docker
docker build -f Dockerfile -t flit/core-api:dev .
docker run -p 8080:8080 flit/core-api:dev
```

## Notas de AOT

- `PublishAot=true` por defecto en `Directory.Build.props`.
- Librerías y proyectos de tipo `Library` tienen `<PublishAot>false</PublishAot>` (solo el ejecutable genera AOT). Tienen `IsAotCompatible=true` para que el analizador detecte llamadas reflexivas problemáticas.
- JSON usa source generators (`AppJsonSerializerContext`). NO usar `JsonSerializer.Serialize(obj)` sin contexto en hot paths.
- Cuando se integre un SDK que rompa AOT (ej. SDK SOAP gubernamental colombiano), seguir el procedimiento de escape descrito en `.ai/agents/core-dotnet.md` §"Política Native AOT".
