# ADR-0007 — API Gateway con YARP en .NET (reemplazo de services/go-gateway/)

- **Estado:** **Aceptado**
- **Fecha:** 2026-05-27
- **Aceptado por:** Jorman Copete (Líder Técnico FLIT)
- **Fecha de aceptación:** 2026-05-27
- **Autor:** Claude Code (sesión interactiva con Líder Técnico)
- **Decisores:** Líder Técnico FLIT (Jorman Copete)
- **Consultados:** Architecture Agent, Backend .NET Agent, Infra Agent, Security Agent
- **Informados:** Equipo .NET, Equipo Frontend
- **Tags:** arquitectura, backend, gateway, .NET, yarp, jwt, rate-limiting
- **Relaciona:** [ADR-0004 Consolidación stack](ADR-0004-consolidacion-stack-dotnet-python.md)

> **📦 Nota de port (2026-05-27):** ADR portado desde repo hermano FLIT donde supersedeaba ADR-0002 §5 (fila go-gateway). En este repo el ADR-0002 trata `closedxml-excel-export` (distinto). Esta decisión aplica igual: usar YARP en .NET para gateway. Referencias a "ADR-0002 microservicios" en el cuerpo refieren al repo origen.

---

## Contexto

[ADR-0002 §5 (2026-05-20)](ADR-0002-arquitectura-microservicios-2026.md) definió **`services/go-gateway/` (Go 1.23 + chi v5 + JWT RS256 + OpenTelemetry)** como API Gateway del stack. El scaffold existe (`services/go-gateway/cmd/`, `services/go-gateway/internal/`) con: routing, auth JWT, health, observability, proxy, ratelimit.

[ADR-0004 hermano](ADR-0004-consolidacion-stack-dotnet-python.md) elimina `services/go-gateway/` por consolidación a 2 servicios backend. Este ADR define **el reemplazo concreto** del gateway dentro de .NET.

### Requisitos del gateway

| # | Requisito | Origen |
|---|---|---|
| G1 | Routing por path/host hacia core-api y python-ml | ADR-0002 §5 |
| G2 | Validación JWT RS256 al borde (rechaza al borde, no propaga a backends) | ADR-0002 §8 |
| G3 | Rate limiting por IP/usuario/login | ADR-0002 §5 |
| G4 | OpenTelemetry obligatorio | ADR-0002 §9 |
| G5 | CORS configurable por entorno (dev, staging, prod) | requisito frontend |
| G6 | Health check público (`/health`) y readiness (`/ready`) | ADR-0002 §9 |
| G7 | Cumplir ADR-0004 (consolidación, sin nuevos servicios fuera de .NET y Python) | ADR-0004 |
| G8 | Imagen Docker mínima (AOT) | objetivo VPS |
| G9 | Soportar SignalR (WebSockets) cuando se agregue a `Flit.Modules.Notifications` | ADR-0004 |
| G10 | Soportar HTTP/2 y HTTP/3 si Caddy negocia con cliente | objetivo perf |

---

## Decisión propuesta

**Adoptar YARP (Yet Another Reverse Proxy) de Microsoft** como API Gateway, hospedado en un **proyecto .NET separado** dentro del solution de `core-api`:

```
services/core-api/src/Flit.Gateway/
├── Flit.Gateway.csproj                    # ASP.NET Core 10 + YARP + AOT
├── Program.cs                              # host minimal
├── appsettings.json                        # config YARP (Routes, Clusters)
├── appsettings.Production.json
├── appsettings.Development.json
├── Middleware/
│   ├── JwtValidationMiddleware.cs         # valida RS256 al borde
│   ├── RateLimitMiddleware.cs             # usa AspNetCoreRateLimit o System.Threading.RateLimiting
│   └── CorrelationIdMiddleware.cs         # propaga X-Correlation-Id al backend
├── Configuration/
│   ├── GatewayOptions.cs
│   └── JwtOptions.cs                       # IssuerSigningKey desde llave pública RS256
├── Health/
│   └── HealthEndpoints.cs                  # /health (proxy + clusters), /ready
└── Dockerfile                              # multi-stage AOT
```

### Por qué proyecto separado y no middleware en `Flit.Api`

| Aspecto | Proyecto separado (`Flit.Gateway`) | Middleware en `Flit.Api` |
|---|---|---|
| Despliegue independiente posible | ✅ (binario aparte) | ❌ |
| Escalado independiente | ✅ | ❌ |
| Acopla gateway al dominio | ❌ | ✅ (anti-pattern) |
| Imagen Docker más pequeña | ✅ (sin EF Core, sin dominio) | ❌ |
| Reinicios del dominio afectan gateway | ❌ (deseable) | ✅ (mal) |
| Coexistencia ambientes (1 binario en dev, 2 en prod) | ✅ (mismo solution) | ❌ |

Es un proyecto independiente, pero comparte el solution `Flit.slnx` y las herramientas de build/test.

### Configuración YARP (appsettings)

```jsonc
{
  "ReverseProxy": {
    "Routes": {
      "core-api-route": {
        "ClusterId": "core-api-cluster",
        "Match": { "Path": "/api/{**catch-all}" },
        "Transforms": [
          { "PathPattern": "/api/{**catch-all}" },
          { "RequestHeader": "X-Forwarded-Host", "Set": "api.flit.co" }
        ],
        "AuthorizationPolicy": "JwtRequired"
      },
      "ml-route": {
        "ClusterId": "python-ml-cluster",
        "Match": { "Path": "/ml/{**catch-all}" },
        "Transforms": [
          { "PathPattern": "/{**catch-all}" }
        ],
        "AuthorizationPolicy": "JwtRequired"
      },
      "signalr-route": {
        "ClusterId": "core-api-cluster",
        "Match": { "Path": "/hubs/{**catch-all}" },
        "AuthorizationPolicy": "JwtRequired"
      }
    },
    "Clusters": {
      "core-api-cluster": {
        "LoadBalancingPolicy": "RoundRobin",
        "Destinations": {
          "core-api-1": { "Address": "http://core-api:8080/" }
        },
        "HttpRequest": { "ActivityTimeout": "00:00:30" }
      },
      "python-ml-cluster": {
        "Destinations": {
          "ml-1": { "Address": "http://python-ml:8000/" }
        },
        "HttpRequest": { "ActivityTimeout": "00:01:00" }
      }
    }
  },
  "Jwt": {
    "Issuer": "https://api.flit.co",
    "Audience": "flit-api",
    "PublicKeyPem": ""                    // inyectado por env JWT__PUBLICKEYPEM
  },
  "RateLimit": {
    "PerIpPermitsPerMinute": 600,
    "PerUserPermitsPerMinute": 1200,
    "LoginEndpointPermitsPerMinute": 10
  }
}
```

### Diagrama de tráfico

```
        Caddy 2 (TLS)  api.flit.co
              │
              ▼
       ┌──────────────┐
       │ Flit.Gateway │ (YARP, AOT, ~50 MB image)
       │  :8080       │
       │              │
       │ Middleware:  │
       │ ├─ JWT RS256 │
       │ ├─ RateLimit │
       │ ├─ Correlation
       │ └─ OTel      │
       │              │
       │  Routes:     │
       │ ├─ /api/* → core-api-cluster
       │ ├─ /ml/*  → python-ml-cluster
       │ └─ /hubs/*→ core-api-cluster (SignalR)
       └──────┬───────┘
              │ HTTP/2
       ┌──────┴─────────────────┐
       ▼                        ▼
  ┌──────────┐            ┌──────────┐
  │ core-api │            │python-ml │
  │  :8080   │            │  :8000   │
  └──────────┘            └──────────┘
```

### AOT compatibility

YARP es **AOT-compatible desde v2.2** (2025). Validar con `dotnet publish /p:PublishAot=true` y test `Flit.ArchTests/GatewayAotTests.cs`.

### Rate limiting strategy

Usar **`System.Threading.RateLimiting`** built-in de .NET 10 (sin paquete externo). Estrategia:

- **Por IP**: TokenBucket 600 permits/minuto. Headers `X-RateLimit-*` informativos.
- **Por usuario autenticado**: TokenBucket 1200 permits/minuto (sumado al de IP).
- **Por endpoint sensible** (login, refresh-token, request-upload-url): 10 permits/minuto.

Implementado como middleware orden:
```
CORS → CorrelationId → RateLimit(IP) → JWT → RateLimit(User) → YARP forward
```

### Token de validación JWT

Llave pública RS256 distribuida desde `Flit.Api` (donde vive OpenIddict) via:
- Local/dev: archivo `infra/secrets/jwt-public.pem` montado readonly
- Prod: variable de entorno `JWT__PUBLICKEYPEM` inyectada desde sops

El gateway **no contacta al core-api** para validar (eso sería un cuello de botella y un acople innecesario). Validación stateless con la llave pública.

### Health y readiness

| Endpoint | Lógica | Uso |
|---|---|---|
| `GET /health` | Responde 200 si el proceso vive | Liveness probe Docker |
| `GET /ready` | Hace HEAD a cada cluster destination; 200 si todos responden, 503 si alguno falla | Readiness probe + Caddy upstream check |

### Observabilidad

- `OpenTelemetry.AutoInstrumentation` para ASP.NET Core + HttpClient (YARP usa HttpClient interno)
- Spans con `service.name=flit-gateway`, propagados al backend via `traceparent`
- Métricas YARP nativas a Prometheus (`/metrics`)
- Logs estructurados JSON a stdout (Loki)

---

## Alternativas consideradas

### Opción 1 — YARP en proyecto .NET separado dentro de core-api solution (RECOMENDADA)

Descrita arriba.

**Pros:**
- Mismo lenguaje y toolchain que el dominio (1 stack)
- AOT-compatible (~50 MB imagen)
- YARP es Microsoft-mantenido, usado en producción interna (Bing, Office, Dynamics) y por terceros (LinkedIn, Stack Overflow)
- Hot reload de config (`appsettings.json` re-cargado en runtime sin restart)
- Soporte HTTP/2 + HTTP/3 nativo
- SignalR pasa nativamente (WebSocket upgrade automático)
- `System.Threading.RateLimiting` built-in (sin paquete externo)
- Despliegue independiente del core-api (binarios separados)

**Cons:**
- Cargar dependencias .NET completas para un gateway "ligero" (mitigado: AOT reduce a ~50 MB)
- Si core-api se reescribiera en otro lenguaje futuro, el gateway tendría que migrar (problema improbable y abordable)

**Esfuerzo:** S (1-2 días: proyecto + middleware + config + Dockerfile + tests)
**Riesgos principales:** ninguno destacado

---

### Opción 2 — YARP como middleware dentro del mismo Flit.Api

**Descripción:** sin proyecto aparte. `Flit.Api` monta YARP como `app.MapReverseProxy()` antes de los endpoints del dominio.

**Pros:**
- Menos infraestructura (1 binario, 1 imagen, 1 puerto)
- Cero latencia red intra-VPS (no hop entre gateway y dominio)
- Más simple operacionalmente

**Cons:**
- **Acopla gateway al dominio**: reinicio del dominio reinicia el gateway (mala higiene)
- Imagen Docker más grande (gateway carga EF Core, Wolverine, dominio completo)
- Escalado conjunto: no se puede escalar solo gateway si CPU se va por ML proxy
- Configuración mezclada: Routes/Clusters de YARP en el mismo `appsettings.json` que connection strings de Postgres
- Anti-pattern documentado: gateway debe poder estar arriba aunque el dominio esté en reinicio

**Esfuerzo:** XS (0.5 día)
**Riesgos principales:** acople, blast radius en cada deploy

---

### Opción 3 — Mantener services/go-gateway/

**Descripción:** conservar el scaffold Go, completar JWT RS256 + rate limit faltantes, operarlo en producción.

**Pros:**
- Aprendizaje Go ya invertido
- Footprint Go ~30 MB
- Stack desacoplado del .NET

**Cons:**
- **Contradice ADR-0004** (consolidación a 2 servicios)
- Equipo de 6-10 personas tendría que mantener 3 stacks backend (Go + .NET + Python)
- Operación de un servicio adicional (CI/CD, observability, secrets, deploys, on-call)
- Sin beneficio técnico único — YARP cubre lo mismo
- Justificación original era aprendizaje (admitido en ADR-0002 §1) — no técnica

**Esfuerzo:** M (completar scaffold a producción)
**Riesgos:** contradice consolidación, mantiene complejidad operativa

---

## Tradeoff aceptado

Elegimos **Opción 1 (proyecto YARP separado en core-api solution)** sobre las demás porque:

1. **Cumple ADR-0004** (consolidación: nada fuera de .NET y Python)
2. **No acopla gateway al dominio** — despliegue independiente, reinicios independientes
3. **YARP es maduro y backed by Microsoft** — bajo riesgo de abandono
4. **AOT reduce footprint** — ~50 MB es competitivo con un gateway Go (~30 MB) sin la complejidad de otro lenguaje
5. **Patrón estándar** en ecosistema .NET — el equipo y futuros developers lo reconocen

**Costo aceptado:** ligero overhead de un proceso adicional (vs Opción 2 in-process) y latencia de un hop intra-VPS (<1ms). Aceptable.

---

## Comparativa

| Criterio | Op.1 YARP separado | Op.2 YARP en Flit.Api | Op.3 Go gateway |
|---|---|---|---|
| Cumple ADR-0004 consolidación | ✅ | ✅ | ❌ |
| Stacks backend a mantener | 2 (.NET + Py) | 2 (.NET + Py) | 3 (.NET + Go + Py) |
| Imagen Docker gateway | ~50 MB (AOT) | N/A (mismo Flit.Api ~200 MB) | ~30 MB |
| Despliegue independiente | ✅ | ❌ | ✅ |
| Acople gateway-dominio | Bajo | Alto | Bajo |
| AOT-compatible | ✅ | ✅ | ✅ |
| HTTP/2 + HTTP/3 + WS | ✅ nativo | ✅ nativo | ✅ con extra config |
| Hot reload de routes | ✅ | ✅ | ❌ |
| Rate limit nativo | ✅ `System.Threading.RateLimiting` | ✅ | ✅ middleware Go |
| Cero latencia (in-process) | ❌ (~1ms hop) | ✅ | ❌ |
| Maduro en producción | ✅ MS interna + LinkedIn + StackOverflow | ✅ | ✅ Netflix + Uber |
| Curva aprendizaje para equipo .NET | XS | XS | M |

---

## Consecuencias positivas

- Stack consolidado a 2 lenguajes backend (.NET + Python)
- Pipeline CI/CD: 1 workflow `core-api.yml` puede buildear `Flit.Api` + `Flit.Gateway` juntos
- Imagen `Flit.Gateway` AOT pequeña (~50 MB)
- Hot reload de configuración de routes sin restart del gateway
- SignalR/WebSocket nativos sin configuración especial
- Métricas Prometheus y traces OTel funcionan out-of-the-box
- Equipo aprende un patrón .NET reutilizable en futuros proyectos

## Consecuencias negativas / riesgos

| # | Riesgo | Mitigación |
|---|---|---|
| Y1 | Si YARP cambia bruscamente de API en futura versión, hay que adaptar | YARP sigue semver. Pinning en `Directory.Packages.props`. Microsoft históricamente respeta compat |
| Y2 | Validación JWT requiere llave pública sincronizada con OpenIddict | Llave pública se publica como artefacto en build de `Flit.Api`, gateway la lee al startup. Si rotación, redeploy en cascada |
| Y3 | Sin proceso aparte de prevenir DoS si rate limit es bypaseable | Rate limit en gateway + Caddy puede tener layer adicional con `rate_limit` directive |
| Y4 | Gateway down → todo el API down | Health check + Docker restart policy `unless-stopped` + monitoring + alerting. Eventualmente: 2 réplicas detrás de Caddy |
| Y5 | Logs duplicados gateway + backend si ambos logguean requests | Convención: gateway loggea metadata (ip, ruta, status, latency); backend solo loggea negocio |

---

## Plan de implementación (PR posterior a aprobación)

1. Crear `services/core-api/src/Flit.Gateway/Flit.Gateway.csproj` (ASP.NET Core 10)
2. Agregar `Yarp.ReverseProxy` a `Directory.Packages.props`
3. Implementar `Program.cs` mínimo con YARP + middleware (JWT, RateLimit, CorrelationId, OTel)
4. Crear `appsettings.{Development,Production}.json` con Routes/Clusters
5. Implementar `Health/HealthEndpoints.cs` con liveness y readiness
6. Dockerfile multi-stage AOT (target ~50 MB)
7. Agregar al `Flit.slnx`
8. Tests: `Flit.Gateway.Tests/` con WebApplicationFactory + mock JWT
9. Workflow CI: extender `core-api.yml` para buildear `Flit.Gateway` también
10. Actualizar `infra/docker-compose.yml`: agregar bloque `core-api-gateway` (puerto 8081 interno), eliminar bloque `go-gateway`
11. Actualizar `Caddyfile`: `api.flit.co → core-api-gateway:8080` (en vez de `go-gateway:8080`)
12. Documentar uso en `services/core-api/src/Flit.Gateway/README.md`

**Estimado:** S (1-2 días con un developer .NET)

---

## ADRs relacionados

- [ADR-0002 §5](ADR-0002-arquitectura-microservicios-2026.md) — Stack tecnológico final (este ADR supersede fila "go-gateway")
- [ADR-0008 §"Diseño MVP"](ADR-0008-excepcion-aws-s3-dynamodb-mvp.md) — referencia "go-gateway valida JWT" (actualizar referencia tras aceptación)
- [ADR-0004](ADR-0004-consolidacion-stack-dotnet-python.md) — hermano (consolidación de stack)
- [ADR-0006](ADR-0006-gestion-archivos-vps-minio.md) — hermano (Caddy rutea `api.flit.co` a este gateway)

## Compliance

- **OWASP A01 (Broken Access Control):** JWT validation al borde + AuthorizationPolicy obligatoria por route
- **OWASP A07 (Identification and Authentication Failures):** Rate limit estricto en `/api/auth/login`, `/api/auth/refresh-token`
- **Habeas Data:** correlation ID propagado en todos los spans/logs para auditoría completa de la cadena

---

## Notas operativas para otros agentes

- **Backend Engineer .NET (`core-dotnet`):** propietario de `Flit.Gateway`. Implementa middleware + tests + Dockerfile.
- **Frontend Engineer:** sin cambios. El frontend sigue apuntando a `https://api.flit.co/...`; el cambio es transparente (de go-gateway a Flit.Gateway).
- **Security Agent:**
  - Validar que JWT public key NO esté en git
  - Validar rate limits configurados antes de prod
  - Auditar CORS policy por entorno
- **Infra Agent:** actualizar `docker-compose.yml` y `Caddyfile`. Configurar healthcheck. Tras aceptación: `git rm -r services/go-gateway/`.
- **QA Agent:** Test Cases nuevos para rate limit (esperar 429) y JWT inválido (esperar 401), validar correlation ID propagado.

---

## Solicitud de aprobación humana

- [ ] Líder Técnico FLIT confirma adoptar YARP sobre alternativa Go
- [ ] Líder Técnico FLIT confirma `services/core-api/src/Flit.Gateway/` como ubicación (vs proyecto aparte del solution core-api)
- [ ] Líder Técnico FLIT confirma estrategia rate limit (600/IP, 1200/user, 10/login por minuto)
- [ ] Líder Técnico FLIT promueve ADR-0007 a **Aceptado** junto con ADRs 0014, 0015, 0016 en PR de promoción

## Trazabilidad

- **Entrada:** ADR-0002 §5 "go-gateway", scaffold existente en `services/go-gateway/`, ADR-0004 (consolidación)
- **Salida (cuando se acepte):** PR `gateway-yarp` con `Flit.Gateway` scaffold + actualización `Flit.slnx`, `Directory.Packages.props`, `infra/docker-compose.yml`, `Caddyfile`; `git rm -r services/go-gateway/`

---

*ADR generado por Claude Code (Opus 4.7) — 2026-05-27.*
*Estado:* **Aceptado** por el Líder Técnico humano el 2026-05-27 (regla FLIT 13 cumplida).
