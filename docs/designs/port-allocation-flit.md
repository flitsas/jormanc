# Propuesta: asignación de puertos FLIT (DEV / QA / PDN)

> **Estado:** Implementado (DEV 4xxx)  
> **Fecha:** 2026-05-28  
> **Alcance:** Un proyecto (monorepo `flit-boilerplate`), esquema base por ambiente.

## Objetivo

Unificar puertos por **servicio** y **ambiente**, usando el patrón:

| Ambiente | Rango host | Ejemplo Front |
|----------|------------|---------------|
| **DEV**  | `4xxx`     | 4001          |
| **QA**   | `5xxx`     | 5001          |
| **PDN**  | `6xxx`     | 6001          |

Así se evita colisión con puertos “clásicos” (5432, 8080, 5173) y con reservas de Windows (p. ej. 8081 en Hyper-V).

---

## Tabla oficial (ordenada por puerto)

![Tabla de puertos FLIT por ambiente](./flit-port-allocation-table.png)

| # | Puerto (DEV) | Servicio | DEV | QA | PDN | Componente FLIT |
|---|--------------|----------|-----|-----|-----|-------------------|
| 01 | 4001 | Front (React) | **4001** | **5001** | **6001** | `frontend` (Vite) |
| 02 | 4002 | Core .NET (API pública) | **4002** | **5002** | **6002** | `Flit.Gateway` (YARP) — `/api`, `/hubs`, `/ml` |
| 03 | 4003 | Core .NET interno (Flit.Api) | **4003** | **5003** | **6003** | `Flit.Api` — solo YARP, no expuesto al Front |
| 04 | 4004 | Worker (jobs) | **4004** | **5004** | **6004** | Reservado: Hangfire (.NET) / Celery (Python) |
| 05 | 4005 | PostgreSQL | **4005** | **5005** | **6005** | `infra` → postgres |
| 06 | 4006 | Redis | **4006** | **5006** | **6006** | `infra` → redis |
| 07 | 4007 | RabbitMQ (AMQP) | **4007** | **5007** | **6007** | `infra` → rabbitmq |
| 08 | 4008 | RabbitMQ (Management UI) | **4008** | **5008** | **6008** | `infra` → rabbitmq UI |
| 09 | 4009 | MinIO (API S3) | **4009** | **5009** | **6009** | `infra` → minio |
| 10 | 4010 | MinIO (Console) | **4010** | **5010** | **6010** | `infra` → minio console |
| 11 | 4011 | MailHog (SMTP) | **4011** | **5011** | **6011** | `infra` → mailhog |
| 12 | 4012 | Core Python (python-ml) | **4012** | **5012** | **6012** | `services/python-ml` (opcional en dev) |
| 13 | 4013 | OTLP gRPC (otel-collector) | **4013** | **5013** | **6013** | `infra` → observability |
| 14 | 4014 | Prometheus | **4014** | **5014** | **6014** | `infra` → observability |
| 15 | 4015 | Grafana | **4015** | **5015** | **6015** | `infra` → observability |
| 16 | 4016 | Loki | **4016** | **5016** | **6016** | `infra` → observability |
| 17 | 4017 | Tempo | **4017** | **5017** | **6017** | `infra` → observability |
| 18 | 4018 | MailHog (Web UI) | **4018** | **5018** | **6018** | `infra` → mailhog UI |
| 19 | 4019 | Reservado | **4019** | **5019** | **6019** | Libre |

> **Importante:** En la tabla del equipo, “Core .NET (API)” = **lo que ve el navegador y el Front** → **Gateway (4002)**.  
> `Flit.Api` no es otro puerto público: es el backend detrás de YARP (**4003** en DEV).

---

## Diagrama de tráfico (DEV)

```mermaid
flowchart LR
  Browser["Browser :4001"]
  GW["Flit.Gateway :4002"]
  API["Flit.Api :4003"]
  PY["python-ml :4012"]
  PG[("Postgres :4005")]
  RD[("Redis :4006")]
  MQ[("RabbitMQ :4007")]
  S3[("MinIO :4009")]

  Browser -->|"/api /hubs /ml"| GW
  GW -->|"/api /hubs"| API
  GW -->|"/ml"| PY
  API --> PG
  API --> RD
  API --> MQ
  API --> S3
```

---

## URLs de referencia (DEV)

| Consumidor | URL |
|------------|-----|
| Usuario (UI) | http://localhost:4001 |
| Front → API (proxy Vite) | http://localhost:4002 (`/api`, `/hubs`, `/ml`) |
| Health gateway | http://localhost:4002/health |
| Health core (directo, debug) | http://localhost:4003/health |
| Health python (opcional) | http://localhost:4012/health |
| Postgres (desde host) | `localhost:4005` |
| Redis | `localhost:4006` |
| RabbitMQ AMQP | `amqp://flit:***@localhost:4007` |
| RabbitMQ UI | http://localhost:4008 |
| MinIO S3 | http://localhost:4009 |
| MinIO Console | http://localhost:4010 |

---

## Mapeo desde puertos actuales → propuesta (DEV)

| Actual | Propuesto | Notas |
|--------|-----------|--------|
| 5173 (Vite) | **4001** | Front |
| 8080 (Gateway) | **4002** | API pública |
| 18081 / 8081 (Flit.Api) | **4003** | Interno |
| 8000 (python-ml) | **4012** | Python (reservado) |
| 5432 | **4005** | Docker `4005:5432` |
| 6379 | **4006** | Docker `4006:6379` |
| 5672 | **4007** | Docker `4007:5672` |
| 15672 | **4008** | Docker `4008:15672` |
| 9000 | **4009** | Docker `4009:9000` |
| 9001 | **4010** | Docker `4010:9001` |
| 1025 (MailHog) | **4011** | Docker `4011:1025` |

---

## Variables de entorno (convención)

Archivo sugerido: `infra/ports/.env.dev` (gitignored o commiteado como `.example`).

```bash
FLIT_ENV=dev
FLIT_PORT_FRONT=4001
FLIT_PORT_GATEWAY=4002
FLIT_PORT_CORE_API_INTERNAL=4003
FLIT_PORT_PYTHON=4012
FLIT_PORT_WORKER=4004
FLIT_PORT_POSTGRES=4005
FLIT_PORT_REDIS=4006
FLIT_PORT_RABBITMQ=4007
FLIT_PORT_RABBITMQ_UI=4008
FLIT_PORT_MINIO=4009
FLIT_PORT_MINIO_CONSOLE=4010
```

QA/PDN: mismo prefijo con `5xxx` / `6xxx` (o `FLIT_ENV=qa` y tabla derivada).

Connection strings en **host** (desarrollo local con `pnpm dev`):

```text
ConnectionStrings__Core=Host=localhost;Port=4005;Database=flit_dev;Username=flit;Password=...
Redis__Connection=localhost:4006
RabbitMq__Uri=amqp://flit:flit_local@localhost:4007
MinIO__Endpoint=http://localhost:4009
PythonMl__BaseUrl=http://localhost:4012
```

Dentro de **Docker network** (compose full stack), los servicios siguen usando nombres DNS (`postgres:5432`, `core-api:8081` interno); solo el **mapeo host** usa `4xxx`.

---

## Archivos a tocar (implementación)

| Área | Archivos |
|------|----------|
| Scripts dev | `package.json` (`dev:*`, `docker:up:infra`) |
| Front | `frontend/vite.config.ts`, `frontend/playwright.config.ts`, `frontend/.env.example` |
| Gateway | `Flit.Gateway/appsettings.Development.json`, `appsettings.QA.json`, `appsettings.Production.json` |
| Core API | `Flit.Api/appsettings.Development.json`, `Properties/launchSettings.json` |
| Infra | `infra/docker-compose.yml`, `docker-compose.prod.yml`, `infra/observability/prometheus.yml` |
| Docs | `README.md`, `scripts/doctor.mjs` |
| CI | Sin cambio de puertos (tests usan TestServer / in-memory) |

---

## Estrategia Docker

**Opción A (recomendada):** puertos internos del contenedor estándar (5432, 6379…) y **publish** al host con `4xxx`:

```yaml
postgres:
  ports: ["${FLIT_PORT_POSTGRES:-4005}:5432"]
```

**Opción B:** cambiar también el puerto interno del contenedor a 4005 — más frágil con imágenes oficiales; no recomendado.

---

## Fases de adopción

1. **Fase 1 — DEV local:** `package.json`, Vite, `appsettings.Development.json`, `docker:up:infra` con mapeo `4xxx`.
2. **Fase 2 — Documentación:** README, `pnpm doctor`, agent-templates infra.
3. **Fase 3 — QA/PDN:** `appsettings.QA.json`, compose prod, Caddy → `6002` (gateway).
4. **Fase 4 — Worker 4004** cuando exista Hangfire/Celery.

---

## Decisiones abiertas (para PO / LT)

1. ¿El Front en DEV debe ser **4001** fijo o se permite 5173 con `strictPort: false` y documentar 4001 como oficial?
2. ¿QA/PDN exponen Postgres/Redis al host o solo red interna Docker (sin publish)?
3. ¿Caddy en PDN termina TLS en 443 y reenvía a **6002** (gateway) sin publicar 6xxx al internet?

---

## Comandos objetivo (post-implementación)

```bash
pnpm docker:up:infra   # postgres:4005, redis:4006, ...
pnpm dev               # front :4001, gateway :4002, core :4003
pnpm doctor            # valida puertos 4xxx
```
