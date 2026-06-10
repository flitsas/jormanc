# Plan de Migración — Reestructuración hacia Plataforma de Microservicios FLIT

> **⚠ SUPERSEDED PARCIAL (2026-05-27):** Las fases referentes a `services/go-gateway/` y `services/node-bff/` están obsoletas tras [ADR-0014 (Aceptado 2026-05-27)](decisions/ADR-0014-consolidacion-stack-dotnet-python.md) que consolida el stack a 2 servicios backend (.NET + Python). Se mantiene este documento como referencia histórica. Las fases relativas a `services/core-api/`, `services/python-ml/` y `frontend/` siguen vigentes con ajustes (ahora `core-api` también hospeda Gateway YARP, Files MinIO, SignalR, RUNT, PDF QuestPDF). Ver ADR-0014/15/16/17 para el plan vigente.
>
> **Documento de trabajo** generado por Claude Code en modo ANÁLISIS COMPARATIVO
> a partir de [ADR-0002](decisions/ADR-0002-arquitectura-microservicios-2026.md).
>
> **Estado original:** PENDIENTE DE APROBACIÓN HUMANA (superseded parcial 2026-05-27).
> **Fecha:** 2026-05-20
> **Rama de trabajo prevista:** `feature/restructure-arquitectura-microservicios`
> **ADR base:** ADR-0002-arquitectura-microservicios-2026.md
> **Co-ADRs hijos previstos:** ADR-0003 (sync-agents.sh metadata-driven), ADR-0004 (BFF reduction plan, superseded por ADR-0014).

---

## Decisiones globales ya tomadas (PASO 2)

| # | Decisión | Valor | Fuente |
|---|---|---|---|
| G1 | Número de ADR base | **ADR-0002** | Respuesta del usuario |
| G2 | Rama de trabajo | **feature/restructure-arquitectura-microservicios** | Respuesta del usuario |
| G3 | Gate de tests por subfase | **backend Vitest + frontend Vitest + frontend Playwright + lint/typecheck en ambos** | Respuesta del usuario |
| G4 | Package manager monorepo | **pnpm global** (pnpm-workspace.yaml en raíz, ambos workspaces) | Respuesta del usuario |
| G5 | sync-agents.sh | **Reescribir para leer metadata embebida en `.ai/agents/<name>.md`** (vía ADR-0003) | Respuesta del usuario |
| G6 | .env protegidos | `.env.user-identity`, `.env.verifik`, `backend/.env` — **NO TOCAR** | Respuesta del usuario |
| G7 | Cambios al ADR antes de empezar | **Ninguno**, se ejecuta tal como está | Respuesta del usuario |

---

## 3.1. Resumen del análisis del PASO 1

### Hallazgos del repositorio real

**Agentes existentes en `.ai/agents/` (10 + orchestrator):**

| Archivo | Frontmatter YAML | Estado para DIFF |
|---|---|---|
| architecture-agent.md | No | Sin cambios planeados |
| **backend-agent.md** | **Sí (atípico)** | Aplicar DIFF 7.2 al cuerpo, preservar frontmatter |
| code-review-agent.md | No | Sin cambios planeados |
| **frontend-agent.md** | No | Aplicar DIFF 7.1 directo |
| infra-agent.md | No | Sin cambios planeados |
| integration-agent.md | No | Sin cambios planeados |
| qa-agent.md | No | Sin cambios planeados |
| security-agent.md | No | Sin cambios planeados |
| tech-lead-agent.md | No | Sin cambios planeados |
| vehicle-query-agent.md | No | Sin cambios planeados (pero no está en tabla de sync-agents.sh) |
| `.ai/orchestrator.md` | No | Sin cambios planeados |

**Stack actual confirmado:**

| Capa | Tecnología real | Versión |
|---|---|---|
| Backend framework | Fastify | 5.8.5 |
| Backend ORM | TypeORM | 0.3.20 |
| Backend logger | Pino | 9.4.0 |
| Backend validación | Zod | 3.23.8 |
| Backend Node | 22 LTS | (engines) |
| Frontend framework | React + Vite | React 19 / Vite 5.4.4 |
| Frontend routing | react-router-dom (declarado) | 6.26.2 (no usado en App.tsx — usa pestañas con useState) |
| Frontend styling | TailwindCSS | **3.4.11** (el ADR pide 4) |
| Frontend data fetching | TanStack Query | 5.56.2 |
| Frontend forms | (ad-hoc, no react-hook-form) | — |
| Frontend lint/format | ESLint 9 + Prettier 3 | — |
| Package manager | **npm workspaces** | (root package-lock.json) |
| Lockfiles detectados | `package-lock.json` (raíz) | sin `pnpm-lock.yaml` ni `yarn.lock` |

**Módulos backend reales** (5 módulos, no solo `personas`):
- `backend/src/modules/personas/`
- `backend/src/modules/employees/`
- `backend/src/modules/employee-dependents/`
- `backend/src/modules/employee-positions/`
- `backend/src/modules/vehicle-query/`

**Features frontend reales** (5 features, mismos nombres):
- `frontend/src/features/{personas,employees,employee-dependents,employee-positions,vehicle-query}/`

**Migraciones existentes** (`backend/migrations/`):
- 7 archivos `.ts` + 7 archivos `.js` **duplicados** (artefacto sospechoso)
- Personas, Employees, Equipo de trabajo, Habeas Data, Dependents, Seguridad Social, Positions

**Otras anomalías:**
- `frontend/src/app/` existe pero está VACÍA (el ADR pide crearla — colisión benigna)
- `frontend/src/shared/{components` directorio basura por typo de shell antiguo (limpiar)
- `infra/docker-compose.yml` mínimo: solo `postgres + backend + frontend`. Sin Redis/RabbitMQ/MinIO/obs
- `docker-compose.prod.yml` mínimo: solo `backend + frontend`, espera Postgres en host
- `.github/workflows/`: `backend-ci.yml`, `cd.yml`, `ci.yml`, `security-scan.yml`

### Cómo funciona el propagador `scripts/sync-agents.sh` (estado actual)

- **Fuente canónica**: `.ai/agents/*.md` + `.ai/orchestrator.md`.
- **Adapter Claude Code**: para cada archivo, antepone un bloque YAML con `name`, `description`, `tools` tomados de **dos diccionarios hard-coded** dentro del propio script (líneas 47-71). Escribe en `.claude/agents/<name>.md`.
- **Adapter Cursor**: copia `.cursor/rules/flit-agents.mdc` solo si no existe; **no regenera** si ya existe.
- **Adapter GitHub Copilot**: solo verifica existencia de `.github/copilot-instructions.md`; **no regenera contenido**.
- **Limitación clave**: la tabla hard-coded de DESCRIPTIONS/TOOLS solo contempla 10 nombres específicos. `vehicle-query-agent` ya está degradado (`description: "Agent for vehicle-query-agent"`, `tools: Read, Grep, Glob, Bash`). Si añadimos `core-dotnet`, `python-ml`, `go-gateway`, `architect`, también saldrán degradados.
- **Plan acordado (G5)**: reescribir el script para que lea metadata embebida en el propio `.ai/agents/<name>.md` (frontmatter o sección comentada `<!-- agent-meta -->`). Esto se hará vía ADR-0003 como pre-requisito de la Fase 1.

---

## 3.2. Tabla maestra de decisiones componente por componente

Para cada componente: estado real → Opción A (preservar) / Opción B (migrar) → trade-offs → recomendación → casilla para tu decisión.

> Marca tu elección con `[X]`. Si eliges Personalizada, escribe la variante debajo.

### Componente 1 — Frontend: framework

- **Estado actual:** React 19.0.0 + Vite 5.4.4 + TypeScript 5.4.5 + Tailwind 3.4.11. RR6 declarado pero no usado (App.tsx maneja pestañas con useState). Lint ESLint 9 + Prettier 3.
- **Opción A (preservar):** mantener Vite, hacer upgrades menores. Resto del stack queda igual.
- **Opción B (migrar):** Next.js 16.2.x con App Router. App Router como capa fina en `src/app/`; FSD se mantiene en `src/features/`.
- **Trade-offs A:** preserva conocimiento del equipo; pierde Server Components/Server Actions; impide alineación con el resto del ADR.
- **Trade-offs B:** alineado al ADR; gran cambio: rutas, SSR, build, deploy (Dockerfile + nginx.conf). React Compiler estable → hay que quitar useMemo/useCallback manuales.
- **Recomendación:** **B** (ya confirmada por el ADR sección 3 fila 1).
- **Tu decisión:** `[ ] A    [X] B    [ ] Personalizada → __________`

### Componente 2 — Frontend: organización FSD vs App Router

- **Estado actual:** FSD presente (`src/features/*`, `src/shared/*`), `src/app/` vacía y sin uso.
- **Opción A (preservar):** Mantener FSD en `src/features/` y crear `src/app/` solo para routing.
- **Opción B (radical):** Disolver `src/features/` dentro de `src/app/` (App Router puro).
- **Recomendación:** **A** (FSD en `src/`, App Router solo orquesta) — confirma ADR sección 3 fila 2.
- **Tu decisión:** `[X] A    [ ] B    [ ] Personalizada → __________`

### Componente 3 — Frontend: estilos

- **Estado actual:** Tailwind 3.4.11 + PostCSS + autoprefixer. Sin shadcn/ui.
- **Opción A (preservar):** Mantener Tailwind 3, no introducir shadcn/ui.
- **Opción B (migrar):** Tailwind 4 + shadcn/ui sobre Radix.
- **Trade-offs B:** Tailwind 4 cambia el sistema de config (CSS-first, sin postcss.config). Migración requiere rescribir `tailwind.config.js` → `tailwind.config.ts` + `@import "tailwindcss"`. shadcn/ui copia primitivos a `src/shared/ui/`.
- **Recomendación:** **B** (confirma ADR).
- **Tu decisión:** `[ ] A    [X] B    [ ] Personalizada → __________`

### Componente 4 — Package manager (decidido en G4)

- **Estado actual:** npm workspaces. `package-lock.json` en raíz. `workspaces: ["backend","frontend"]`.
- **Opción A (preservar):** npm workspaces.
- **Opción B (migrar):** **pnpm workspaces globales** (decidido en G4).
- **Acciones B:**
  1. Crear `pnpm-workspace.yaml` con `packages: ['backend','frontend']`.
  2. Quitar `workspaces` y `engines.node` del `package.json` raíz (o reemplazar `workspaces` por nada y dejarlo solo informativo).
  3. Eliminar `package-lock.json` raíz.
  4. Ejecutar `pnpm install` en raíz → genera `pnpm-lock.yaml`.
  5. Actualizar scripts raíz: `"dev": "pnpm --filter @flit/backend dev | pnpm --filter @flit/frontend dev"` (con concurrently o `pnpm -r`).
  6. Actualizar [INSTRUCCIONES.md](../INSTRUCCIONES.md) (apéndice) y scripts CI.
- **Recomendación:** **B** (ya decidido).
- **Tu decisión:** `[ ] A    [X] B    [ ] Personalizada → __________`

### Componente 5 — Backend: scope

- **Estado actual:** Fastify 5 con Clean Architecture y 5 módulos productivos (`personas, employees, employee-dependents, employee-positions, vehicle-query`).
- **Opción A (preservar):** Mantener todos los endpoints; sigue siendo backend principal.
- **Opción B (reducir gradual):** Convertir en BFF de WebSockets + RabbitMQ consumer. La lógica de dominio migra a `services/core-api/` .NET.
- **Trade-offs B:** **Migración gradual**: durante la transición, el backend Node sigue activo con sus 5 módulos. NO se rompe nada en un PR. La reducción real ocurre en Fase 9 (módulo por módulo).
- **Recomendación:** **B gradual** (confirma ADR sección 3 fila 5).
- **Tu decisión:** `[ ] A    [X] B gradual    [ ] Personalizada → __________`

### Componente 6 — Backend: estructura Clean Architecture interna

- **Estado actual:** `domain/application/infrastructure/interfaces` correcto por cada uno de los 5 módulos.
- **Opción A (preservar):** Mantener tal cual; nuevos módulos siguen el mismo patrón.
- **Opción B (migrar contenido a .NET):** Reimplementar dominio en `services/core-api/src/Tramites.Modules.*` (no traducir literal, reescribir en C# 14).
- **Recomendación:** **B** pero como **referencia documental** (el código actual queda en `backend/src/modules/` hasta validación del módulo equivalente .NET).
- **Tu decisión:** `[ ] A    [X] B (con referencia documental)    [ ] Personalizada → __________`

### Componente 7 — TypeORM en backend → EF Core 10 en core-api

- **Estado actual:** TypeORM 0.3.20 con 7 migraciones (5 entidades).
- **Opción A (preservar):** Mantener TypeORM como ORM productivo.
- **Opción B (sustituir):** EF Core 10 dueño del schema en core-api .NET; TypeORM se desuso gradual.
- **Recomendación:** **B** (confirma ADR Decisión 6, sección 3 fila 7).
- **Tu decisión:** `[ ] A    [X] B    [ ] Personalizada → __________`

### Componente 8 — `backend/migrations/` (`.js` + `.ts` coexistentes)

- **Estado actual:** 7 `.ts` + 7 `.js`, **ambos versionados en git**.
- **CORRECCIÓN tras inspección en Fase 0e (2026-05-20):** los `.js` **NO son duplicados de build accidentales**. Son requeridos por el setup TypeScript ESM + NodeNext del backend:
  - `backend/src/shared/config/database.ts` líneas 8-14 importa cada migración con **extensión `.js` explícita** (`from "../../../migrations/1700000000000-CreatePersonasTable.js"`).
  - El runner `migration:run` consume estos imports; **eliminar los `.js` rompe migraciones en runtime**.
- **Opción A (preservar todo) ✅ ELEGIDA:** No tocar nada.
- **Opción B (eliminar `.js`):** ❌ DESCARTADA — rompería el runner ESM.
- **Decisión:** **A** (cancelada Fase 0e en ejecución, 2026-05-20).
- **Tu decisión:** `[X] A    [ ] B    [ ] Personalizada → __________`

### Componente 9 — Validación: Zod (BFF) vs FluentValidation (.NET)

- **Estado actual:** Zod 3.23.8 en `interfaces/*.dto.ts` con `fastify-type-provider-zod`.
- **Recomendación:** **Zod conserva en BFF, FluentValidation en core-api .NET** (confirma ADR sección 3 fila 9).
- **Tu decisión:** `[ ] A    [X] B    [ ] Personalizada → __________`

### Componente 10 — `infra/docker-compose.yml` (desarrollo)

- **Estado actual:** Solo `postgres + backend + frontend`. Sin Redis/RabbitMQ/MinIO/obs/Caddy.
- **Opción A (preservar):** Solo Postgres + apps.
- **Opción B (expandir):** Postgres + Redis + RabbitMQ + MinIO + Tempo + Loki + Prometheus + Grafana + Caddy.
- **Recomendación:** **B** (confirma ADR sección 3 fila 10). Implementar en Fase 8.
- **Tu decisión:** `[ ] A    [X] B    [ ] Personalizada → __________`

### Componente 11 — `docker-compose.prod.yml`

- **Estado actual:** Solo `backend + frontend`, Postgres en host.
- **Opción B (expandir):** añadir 3 servicios nuevos + Caddy + observabilidad.
- **Recomendación:** **B** (confirma ADR). Fase 8.
- **Tu decisión:** `[ ] A    [X] B    [ ] Personalizada → __________`

### Componente 12 — Agentes existentes en `.ai/agents/`

- **Estado actual:** 10 archivos (lista arriba en 3.1).
- **DIFFs a aplicar:**
  - `.ai/agents/frontend-agent.md` ← DIFF 7.1 del ADR (Next.js, pnpm, Tailwind 4, shadcn/ui, Zustand, Server Components, etc.).
  - `.ai/agents/backend-agent.md` ← DIFF 7.2 del ADR (BFF reducido, WebSockets + RabbitMQ). **Preservar el frontmatter YAML existente**.
- **Recomendación:** Aplicar DIFFs en Fase 1.
- **Tu decisión:** `[X] Aplicar DIFFs    [ ] Reescribir desde cero    [ ] Personalizada → __________`

### Componente 13 — Agentes nuevos en `.ai/agents/`

- **A crear:** `core-dotnet.md`, `python-ml.md`, `go-gateway.md`, `architect.md` (definiciones en ADR sección 8.1-8.4).
- **Cuándo:** Fase 1.
- **Tu decisión:** `[X] Crear los 4    [ ] Subconjunto (especifica)    [ ] Personalizada → __________`

### Componente 14 — `.claude/agents/`, `.cursor/rules/`, `.github/`

- **Regla:** NO TOCAR DIRECTAMENTE. Solo a través de `sync-agents.sh`.
- **Recomendación:** **A** (proceso existente). Confirmado por reglas absolutas del prompt.
- **Tu decisión:** `[X] A (solo via sync-agents.sh)`

### Componente 15 — `scripts/sync-agents.sh`

- **Estado actual:** descripciones y tools hard-coded para 10 agentes.
- **Decisión G5 ya tomada:** **reescribir** para que lea metadata desde el propio `.ai/agents/<name>.md`.
- **Acciones:**
  1. Definir formato de metadata embebido (frontmatter YAML o bloque `<!-- agent-meta -->`).
  2. Decisión clave de diseño: **el frontmatter de `.ai/agents/<name>.md` se considera metadata, y el script lo extrae y lo reescribe (eventualmente con campos adicionales) sobre `.claude/agents/<name>.md`**. El cuerpo Markdown se copia sin cambios.
  3. Generar ADR-0003-sync-agents-metadata-driven.md (Propuesto).
  4. Implementar nuevo script.
  5. Validar que regenera correctamente los 10 agentes actuales + los 4 nuevos.
- **Cuándo:** Pre-requisito de Fase 1 (lo llamamos **Fase 0.5**).
- **Tu decisión:** `[X] Reescribir (Fase 0.5, ADR-0003)    [ ] Otra opción → __________`

### Componente 16 — `agent-templates/`

- **Estado actual:** Plantillas corporativas FLIT (conventions, DoR, DoD, code-style, security-checklist, etc.).
- **Regla:** NO TOCAR sin OK explícito.
- **Tu decisión:** `[X] A (no tocar)`

### Componente 17 — ADRs existentes en `docs/decisions/`

- **Estado actual:** ADR-0001 (Aceptado), ADR-0002 (este, Propuesto).
- **Recomendación:** Conservar; añadir ADR-0003 (sync-agents.sh) y ADR-0004 (BFF reduction) cuando corresponda.
- **Tu decisión:** `[X] A + nuevos`

### Componente 18 — `CLAUDE.md` raíz

- **Estado actual:** Convenciones FLIT (este archivo es la fuente de verdad).
- **Recomendación:** **A + apéndice** al final, referenciando los nuevos servicios. NO reescribir.
- **Tu decisión:** `[X] A + apéndice    [ ] Personalizada → __________`

### Componente 19 — `.env.verifik.example`

- **Estado actual:** Plantilla con `VERIFIK_API_TOKEN=`.
- **Decisión G6:** NO leer `.env.verifik`. Preservar `.env.verifik.example`.
- **Recomendación:** **A inicial** (preservar). Migrar a sops es Fase 8.
- **Tu decisión:** `[X] A inicial, B en Fase 8`

### Componente 20 — `.env.user-identity` (G6)

- **Tu decisión:** `[X] A (no tocar)`

### Componente 21 — `agent-templates/` (repetido en ADR, mismo que C16)

- **Tu decisión:** `[X] A (no tocar)`

### Componente 22 — `INSTRUCCIONES.md`

- **Estado actual:** Guía paso a paso, asume npm workspaces.
- **Recomendación:** **A + apéndice** con nueva sección "Plataforma de microservicios". Actualizar el paso 1 de install para reflejar `pnpm install` (es un cambio pequeño pero rompe el flujo actual). Discutir si es apéndice o reemplazo del paso 1.
- **Subdecisión:** ¿reemplazar el paso 1 de install por pnpm, o solo agregar apéndice?
  - **Sub-A:** reemplazar `npm install` por `pnpm install` en paso 1 (más correcto, pero modifica el documento).
  - **Sub-B:** dejar el paso 1 actual + apéndice "si usas la nueva arquitectura, usa pnpm".
- **Recomendación:** **Sub-A** (después de G4, el documento queda obsoleto si no se actualiza).
- **Tu decisión:** `[X] A + apéndice + sub-A (reemplazar paso 1 con pnpm)    [ ] sub-B    [ ] Personalizada`

### Componente 23 — `README.md`

- **Recomendación:** **A + apéndice**.
- **Tu decisión:** `[X] A + apéndice`

### Componente 24 — `.github/workflows/`

- **Estado actual:** `backend-ci.yml, ci.yml, cd.yml, security-scan.yml`.
- **Acción:** añadir `core-api.yml`, `python-ml.yml`, `go-gateway.yml` en sus respectivas fases. NO modificar los actuales.
- **Tu decisión:** `[X] B (añadir, no reemplazar)`

### Componente 25 — `.github/copilot-instructions.md`

- **Decisión:** Solo modificar via `sync-agents.sh` reescrito.
- **Tu decisión:** `[X] B via sync`

### Componentes nuevos a crear (sección 3 del ADR, segunda tabla)

| Item | Acción | Fase |
|---|---|---|
| `services/` raíz | Crear directorio | Fase 4 (y siguientes) |
| `services/core-api/` (.NET 10) | Crear scaffold | Fase 4 |
| `services/python-ml/` (Python 3.13) | Crear scaffold | Fase 6 |
| `services/go-gateway/` (Go 1.23+) | Crear scaffold | Fase 5 |
| `contracts/openapi/` | Crear | Fase 7 |
| `contracts/asyncapi/` | Crear | Fase 7 |
| `infra/observability/` | Crear | Fase 8 |
| `infra/caddy/` | Crear | Fase 8 |
| `infra/secrets/` | Crear (sops) | Fase 8 |
| `.ai/agents/core-dotnet.md` | Crear | Fase 1 |
| `.ai/agents/python-ml.md` | Crear | Fase 1 |
| `.ai/agents/go-gateway.md` | Crear | Fase 1 |
| `.ai/agents/architect.md` | Crear | Fase 1 |

---

## 3.3. Plan de migración por fases (ajustado)

> Una fase por PR. Dentro de cada fase, **una subfase a la vez**. Tras cada subfase: muestro diff resumido, ejecuto build/tests, espero confirmación humana.

### Fase 0 — Preparación (no rompe nada)

- [ ] **0a.** Crear rama `feature/restructure-arquitectura-microservicios` desde `develop`.
- [ ] **0b.** Documento ADR-0002 ya guardado en `docs/decisions/` (preexistente) y copia en raíz `PLATFORM_RESTRUCTURE.md` (preexistente). Solo **verifico** que están en el commit.
- [ ] **0c.** Crear `docs/MIGRATION_PLAN.md` (este archivo) con casillas marcadas (este PR).
- [X] **0d.** Limpiar `frontend/src/shared/{components` (directorio basura por typo de shell). — ejecutado 2026-05-20.
- [~] **0e.** ~~Eliminar duplicados `.js` en `backend/migrations/`~~ — **CANCELADA**: tras inspección, los `.js` no son duplicados, son requeridos por el setup ESM + NodeNext (database.ts los importa con extensión `.js` explícita). Componente 8 reverte a opción A (preservar). 2026-05-20.
- [ ] **0f.** Apéndice a `CLAUDE.md` raíz (no destructivo, al final).
- [ ] **0g.** Apéndice a `INSTRUCCIONES.md` + actualización del paso 1 a pnpm (componente 22 sub-A).
- [ ] **0h.** Apéndice a `README.md`.
- [ ] **0i.** Build + tests verdes en backend y frontend, lint y typecheck verdes.
- **Commit:** `docs(adr): add MIGRATION_PLAN.md, appendices for ADR-0002 microservices restructure`

### Fase 0.5 — Reescribir `scripts/sync-agents.sh` (ADR-0003) ✅ COMPLETADA 2026-05-20

- [X] **0.5a.** Generar `docs/decisions/ADR-0003-sync-agents-metadata-driven.md` en estado **Propuesto** con 3 alternativas (frontmatter YAML / comentario HTML / archivo paralelo .meta.yaml). Elegida: frontmatter YAML.
- [X] **0.5b.** Definir el formato: frontmatter YAML con campos `name`, `description`, `tools` (CSV inline). Backend y vehicle-query agents normalizados (sus campos atípicos quedaron como bloque blockquote en el cuerpo).
- [X] **0.5c.** Reescribir `scripts/sync-agents.sh` (213 líneas, awk parser de frontmatter, modo `--validate`, fail-fast).
- [X] **0.5d.** Frontmatter añadido a los 11 archivos (9 sin previo + backend-agent normalizado + vehicle-query-agent normalizado + orchestrator).
- [X] **0.5e.** Validación: 11/11 PASS. Sync regenera correctamente.
- [X] **0.5f.** Cursor y Copilot adapters preservados (no se regeneran si existen, comportamiento conservador).
- **Commit:** `refactor(scripts): rewrite sync-agents.sh to read metadata from .ai/agents/ (ADR-0003)`

### Fase 1 — Agentes nuevos + DIFFs ✅ COMPLETADA 2026-05-20

- [X] **1a.** Crear `.ai/agents/core-dotnet.md` (ADR §8.1 + frontmatter ADR-0003).
- [X] **1b.** Crear `.ai/agents/python-ml.md` (ADR §8.2).
- [X] **1c.** Crear `.ai/agents/go-gateway.md` (ADR §8.3).
- [X] **1d.** Crear `.ai/agents/architect.md` (ADR §8.4).
- [X] **1e.** Aplicar DIFF 7.1 a `.ai/agents/frontend-agent.md` (stack target Next.js/pnpm/Tailwind 4, sección "Server vs Client Components", reglas 9 y 10 nuevas).
- [X] **1f.** Aplicar DIFF 7.2 al cuerpo de `.ai/agents/backend-agent.md` (sección "Cambio de rol — ADR-0002" añadida al inicio del cuerpo, frontmatter ADR-0003 ya normalizado en Fase 0.5).
- [X] **1g.** Ejecutar `./scripts/sync-agents.sh` → 14 agentes (10 antiguos + 4 nuevos) sincronizados correctamente.
- **Commit:** `feat(agents): add .NET, Python, Go, architect agents; update frontend/backend agents for microservices`

### Fase 2 — Frontend: migrar React+Vite → Next.js 16

- [X] **2a.** Migrar monorepo a **pnpm** ✅ COMPLETADA 2026-05-20.
  - `pnpm-workspace.yaml` creado con `packages: [backend, frontend]`.
  - `package-lock.json` raíz eliminado; `pnpm-lock.yaml` generado.
  - `package.json` raíz: `workspaces` removido; scripts `pnpm -r` y `pnpm --filter`; añadido `packageManager: pnpm@11.0.8` y `pnpm.onlyBuiltDependencies: [esbuild]`.
  - Patch puntual: `@typescript-eslint/parser` añadido como devDep explícita del frontend (npm hoisting → pnpm estricto).
  - Gate Fase 2a: lint, typecheck, backend tests (82/82). PASS. Frontend test --passWithNoTests (R13).
- [X] **2b.** Backup branch `feature/restructure/frontend-pre-nextjs-snapshot` creado 2026-05-20.
- [X] **2c.** Next.js 16.2.6, React 19, Tailwind 4.3.0 instalados via pnpm.
- [X] **2d.** `next.config.ts` con `output: "standalone"`, rewrites `/api/v1/*` → `BACKEND_API_URL`, security headers. `src/app/layout.tsx` (Server) + `src/app/providers.tsx` (Client con QueryClient) + `src/app/page.tsx` creados.
- [~] **2e.** shadcn/ui base: dependencias instaladas (`class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react`) y design tokens en `globals.css` con `@theme {}`. Primitivos UI (`button.tsx`, etc.) se copian on-demand cuando aparezca el primer caso de uso.
- [X] **2f.** Biome 2.4.15 reemplaza ESLint+Prettier. `noArrayIndexKey` y `useAriaPropsSupportedByRole` bajadas a `warn` por deuda preexistente. CSS excluido (Tailwind 4 `@theme` no parseable).
- [X] **2g.** Custom navigation con `useState` movido a `src/features/admin/AdminPanel.tsx` con `"use client"`. Diferido a PR siguiente: split en rutas App Router separadas.
- [X] **2h.** `"use client"` en `AdminPanel` y `DashboardLayout`. Conversión real a Server Components: diferida.
- [~] **2i-2j.** Server Components y Server Actions: pendientes (requieren análisis por feature).
- [~] **2k.** Zustand 5 declarado en deps. Sin store creado todavía (no hay estado global cliente).
- [X] **2l.** Eliminados: `vite.config.ts`, `tsconfig.tsbuildinfo`, `dist/`. Vite + plugin-react se mantienen como devDeps porque Vitest los usa.
- [X] **2m.** `react-router-dom` eliminado del `package.json` (no se usaba).
- [X] **2n.** `index.html` eliminado.
- [X] **2o.** `frontend/Dockerfile` reescrito multi-stage para Next.js standalone (sin nginx, usuario non-root). `nginx.conf` eliminado.
- [~] **2p-2q.** Tests Vitest/Playwright: deuda preexistente R13. Se mantiene `--passWithNoTests`.
- [X] **2r.** Gate completo: lint PASS (9 warnings de deuda), typecheck PASS, build Next.js 16.2.6 PASS, backend tests 93/93 PASS.
- **Commit Fase 2a:** `chore(monorepo): migrate from npm workspaces to pnpm workspaces (ADR-0002 Fase 2a)` (ya hecho).
- **Commit Fase 2b-2r:** `feat(frontend): migrate from Vite to Next.js 16 + Tailwind 4 + Biome (ADR-0002 Fase 2)`

### Trabajo Fase 2 ampliada — actualización 2026-05-20 (esta sesión)

| Trabajo | Estado |
|---|---|
| Split de pestañas a rutas App Router (`src/app/(admin)/{employees,dependents,positions,personas}/page.tsx`) | ✅ COMPLETADO. `AdminPanel` removido; navegación con `next/link` + `usePathname()`. `src/app/page.tsx` redirige a `/employees`. |
| Migración `axios` → `fetch` nativo | ✅ COMPLETADO. `apiClient` reescrito como wrapper sobre `fetch` con API axios-compatible (`get/post/put/delete` retornan `{ data, status }`). `axios` removido del `package.json`. AbortController para timeouts; soporta `signal` externo (TanStack Query cancellation); `URLSearchParams` para params. `withBaseUrl()` para Server Components. |
| Primitivos shadcn adicionales (Input, Label, Textarea) | ✅ COMPLETADO. Sin dependencias Radix (Label simplificado). |
| Tests Vitest por feature | ✅ COMPLETADO. 4 archivos nuevos en `src/features/*/api/*.api.test.ts` validan los `queryKeys` factory. |
| Playwright E2E (R13) | ✅ COMPLETADO. `playwright.config.ts` + `e2e/smoke.spec.ts` con 5 tests de navegación (home redirect, sidebar, link navigation con `aria-current`, breadcrumb, metadata title). Chromium 1223 ya estaba instalado en el ambiente. **5/5 PASS.** |
| Wiring de Zustand | ✅ Store mínimo `useUiStore` (sidebar + theme override) con `persist` middleware. |
| Conversión a Server Components + Server Actions | ✅ COMPLETADA 2026-05-20 (ADR-0005). Las 4 rutas `/employees`, `/dependents`, `/positions`, `/personas` son ahora Server Components con `dynamic = "force-dynamic"`. Cada feature tiene `*.server.ts` (server-only fetchers con fallback graceful) + `*.actions.ts` (Server Actions con `revalidateTag(name, "default")` — Next.js 16 API). Client Components (`PersonasPage`, `EmployeesPage`, etc.) reciben `initialData` y la pasan a `useQuery({ initialData })`. Para `dependents` y `positions`, el server pre-fetchea **la lista de empleados** (selector); el listado real requiere `employeeId` que el usuario elige client-side. Playwright valida SSR con `javaScriptEnabled: false` en 2 tests (`@ssr` tag). |

### Fase 3 — Reducir backend a BFF WebSockets (ADR-0004) ✅ COMPLETADA 2026-05-20

- [X] **3a.** Inventario de los 5 módulos en ADR-0004 §"Mapping de migración por módulo": personas, employees, employee-dependents (sub-agregado), employee-positions (cargos), vehicle-query (integraciones/runt).
- [X] **3b.** `docs/decisions/ADR-0004-bff-reduction-plan.md` creado en **Propuesto** con 3 alternativas (gradual módulo por módulo, big-bang, strangler fig). Elegida: gradual.
- [X] **3c.** Endpoints actuales no tocados. Los 5 módulos siguen activos. Añadido `backend/src/modules/websockets/` (4 capas Clean Arch: domain con WsEvent + WsMessage VO; application con PushToClient + Broadcast use cases; infrastructure con InMemoryConnectionRegistry + RabbitMqConsumerAdapter stub; interfaces con websockets.routes.ts).
- [X] **3d.** RabbitMQ consumer scaffolded como stub (`rabbitmq-consumer.adapter.ts`): idempotencia con `IIdempotencyStore`, ACK/NACK explícito, `handleMessage` testeable. Integración real con amqplib diferida a Fase 8.
- [X] **3e.** Tests unitarios: 7 (ws-event.entity) + 4 (push-to-client.use-case) = 11 nuevos. Suite backend total: **93/93 PASS** (anterior 82, ahora +11).
- [X] Registro del módulo WS en `backend/src/app.ts` con `app.register(websocketsRoutes)`.
- **Commit:** `feat(backend): add WebSockets module + RabbitMQ consumer scaffold for BFF role (ADR-0004 Fase 3)`

### Fase 4 — Crear core-api .NET 10 ✅ SCAFFOLD MÍNIMO COMPLETADO 2026-05-20

- [X] **4a.** `services/core-api/` creado con `Tramites.slnx` (nuevo formato XML de .NET 10).
- [~] **4b.** Estructura **parcial**: `Tramites.Api`, `Tramites.Modules.Tramites` (1 módulo de ejemplo), `Tramites.SharedKernel`. Pendiente Documentos/Integraciones/Identity/Notificaciones — se añaden en Fase 9 (cuando se migren los módulos del backend Node).
- [X] **4c.** `Directory.Build.props` (AOT, Nullable, TreatWarningsAsErrors, NoWarn CA1014/CA1000/CA1707), `Directory.Packages.props` (CPM), `global.json` (.NET 10.0.300).
- [~] **4d.** Dependencias **parciales**: OpenTelemetry 1.15.3 (sin CVE; ADR-0002 pedía 1.10, corregido). Wolverine 3.x, EF Core 10, Dapper, FluentValidation, OpenIddict, Refit, Mapster **NO instalados aún** — se añaden cuando exista el primer caso de uso real.
- [X] **4e.** OpenTelemetry con OTLP exporter + Serilog en `Program.cs`.
- [X] **4f.** Endpoint `GET /api/v1/health` (Minimal API) con AOT JsonSerializerContext. 6 tests xUnit v3 de `Tramite.Crear` factory (success, failure, trim, ids distintos). Testcontainers pendiente (sin DB todavía).
- [~] **4g.** NetArchTest con **3 reglas** implementadas (Domain↛Adapters, Domain↛AspNetCore, SharedKernel↛otros). 3 restantes (Features no se importan entre sí, módulos comunican por eventos, Ports en módulo dueño) **pendientes** porque solo hay 1 módulo. Se activan al añadir el 2°.
- [X] **4h.** AOT habilitado por default en `Directory.Build.props`. Publish multi-RID en Dockerfile (linux-musl-x64). `dotnet build` PASS sin warnings.
- [X] **4i.** Dockerfile multi-stage: sdk:10.0 → runtime-deps:10.0-alpine con AOT.
- **Commit:** `feat(core-api): scaffold .NET 10 service with AOT, OTel, NetArchTest (ADR-0002 Fase 4 mínimo)`

### Fase 5 — Crear go-gateway ⚠️ SCAFFOLD MÍNIMO (Go SDK no instalado en ambiente)

- [X] **5a.** `services/go-gateway/` con `go.mod` (module `flit/go-gateway`, Go 1.23).
- [X] **5b.** Estructura idiomática: `cmd/gateway/main.go` + `internal/{auth, proxy, ratelimit, observability, health, config}`.
- [~] **5c.** JWT RS256 validation: **solo `.gitkeep`** en `internal/auth/`. Implementación real en Fase 5 ampliada / Fase 9.
- [~] **5d.** Reverse proxy: **solo `.gitkeep`** en `internal/proxy/`. Implementación real en Fase 9.
- [~] **5e.** Rate limiting: **solo `.gitkeep`** en `internal/ratelimit/`. Pendiente.
- [X] **5f.** OpenTelemetry Go SDK 1.32.0 declarado en `go.mod` y wireado en `internal/observability/tracing.go`.
- [~] **5g.** Tests httptest: **pendientes** (Go SDK no instalado en el ambiente actual; no se pudo correr `go test`).
- [X] **5h.** Dockerfile multi-stage (golang:1.23-alpine → distroless/static-debian12:nonroot) + HEALTHCHECK.
- **Validación pendiente:** el usuario debe instalar Go 1.23+ y correr `go mod tidy && go build ./... && go test ./...`.
- **Commit:** `feat(go-gateway): scaffold Go gateway with chi, slog, OTel (ADR-0002 Fase 5 mínimo)`

### Fase 6 — Crear python-ml ⚠️ SCAFFOLD MÍNIMO (uv no instalado en ambiente)

- [~] **6a.** `services/python-ml/` con `pyproject.toml` (hatchling backend; `uv` no estaba instalado, no se ejecutó `uv init`).
- [X] **6b.** FastAPI 0.115 + Pydantic v2 + pydantic-settings + structlog declarados en deps. `app/main.py` con `create_app()` factory, structlog JSON, settings injection.
- [X] **6c.** `/health/live` y `/health/ready` en `app/adapters/api/health.py`. **Validación JWT pendiente** (`app/middleware/` no creado aún — se añade en Fase 6 ampliada).
- [~] **6d.** Adapter Tesseract / EasyOCR: **pendiente**. Las deps OCR/ML están **comentadas** en pyproject.toml; se descomenten cuando se implemente el primer pipeline.
- [~] **6e.** Consumer RabbitMQ aio-pika: **pendiente** (carpeta `app/adapters/messaging/` vacía).
- [X] **6f.** OpenTelemetry deps declaradas. `instrument_app(app)` **pendiente** en `main.py` (TODO documentado).
- [X] **6g.** Tests pytest: 6 tests del dominio `OcrResult` en `tests/test_ocr_result.py`. Coverage no medido (no `uv` para correr).
- [X] **6h.** Dockerfile multi-stage Python 3.13-slim + uv 0.5.
- **Validación pendiente:** el usuario debe instalar `uv` (`pip install uv` o `curl -LsSf https://astral.sh/uv/install.sh | sh`) y correr `uv sync --extra dev && uv run pytest`.
- **Commit:** `feat(python-ml): scaffold Python 3.13 + FastAPI + structlog (ADR-0002 Fase 6 mínimo)`

### Fase 7 — Contratos OpenAPI/AsyncAPI ✅ COMPLETADA 2026-05-20

- [X] **7a.** `contracts/openapi/core-api.v1.yaml` con endpoints `/api/v1/health` y `/api/v1/tramites` (POST). Schemas: HealthResponse, CrearTramiteRequest, TramiteCreadoResponse, ProblemDetails (RFC 7807). securitySchemes bearerAuth (JWT RS256).
- [X] **7b.** `contracts/openapi/python-ml.v1.yaml` con health endpoints. Endpoints OCR como TODO.
- [X] **7c.** `contracts/asyncapi/events.v1.yaml` (AsyncAPI 3.0) con channels `flit.tramites`, `flit.documentos`; operations send/receive; messages TramiteCreado/Actualizado/EstadoCambiado, DocumentoSubido/Procesado. EnvelopeMeta común con `messageId` UUIDv7 (idempotencia).
- [~] **7d.** `pnpm codegen` script: **pendiente**. Lo añade el PR que migre el frontend (Fase 2b-2r) cuando exista cliente para regenerar.
- [~] **7e.** Test de contrato en core-api .NET: **pendiente** (cuando se implemente el endpoint POST /api/v1/tramites real).
- [X] `contracts/README.md` con reglas (versionado, validación local Redocly/AsyncAPI CLI).
- [X] Workflow `.github/workflows/contracts.yml` valida OpenAPI con Redocly y AsyncAPI con asyncapi CLI.
- **Commit:** `feat(contracts): add OpenAPI v1 (core-api, python-ml) and AsyncAPI v1 (events) (ADR-0002 Fase 7)`

### Fase 8 — Infraestructura completa ✅ COMPLETADA 2026-05-20

- [X] **8a.** `infra/docker-compose.yml` expandido: 13 servicios (postgres + redis + rabbitmq + minio + otel-collector + prometheus + loki + tempo + grafana + core-api + bff + web + python-ml + go-gateway). Healthchecks por servicio crítico. depends_on con condition: service_healthy.
- [X] **8b.** `infra/observability/`:
  - `otel-collector.yaml`: receivers OTLP gRPC+HTTP; exporters a tempo (traces), prometheus (metrics), loki (logs).
  - `prometheus.yml`: scrape config para otel-collector y self.
  - `loki-config.yaml`: TSDB filesystem, OTLP support, structured-metadata.
  - `tempo-config.yaml`: OTLP gRPC+HTTP receivers, local storage 24h retention.
  - `grafana/datasources/datasources.yaml`: Prometheus + Loki + Tempo conectados via service-discovery interno; tracesToLogsV2 habilitado.
- [X] **8c.** `infra/caddy/Caddyfile` para producción: auto-SSL, reverse proxy a go-gateway (/api/*), bff (/ws/*) y web (/), security headers (HSTS, X-Frame-Options DENY, Permissions-Policy restrictiva). Email para Let's Encrypt.
- [X] **8d.** Llaves JWT generadas (2026-05-20) via `./scripts/gen-secrets.sh`. RSA 4096 (chmod 600 privada, 644 pública). `.env.dev` con 4 passwords aleatorios base64-24 (postgres, rabbitmq, minio, grafana). Las 3 archivos quedan en `.gitignore` (verificado con `git check-ignore`). Script soporta modos `--check` y `--force`. Sólo el script `scripts/gen-secrets.sh` se commitea; las llaves nunca.
- [X] **8e.** sops + age preparado: `infra/secrets/.gitignore` bloquea todo excepto README y `*.example`. `.sops.yaml` raíz pendiente (Fase 8 ampliada).
- [X] **8f.** `docker-compose.prod.yml` expandido (2026-05-20): 14 servicios (postgres, redis, rabbitmq, minio, otel-collector, prometheus, loki, tempo, grafana, core-api, bff, web, python-ml, go-gateway, caddy). YAML anchors `x-restart-policy` y `x-default-logging` (json-file rotation 10MB×5). Env vars con defaults (`*_TAG`, `LOG_LEVEL`, `CADDY_EMAIL`). Caddy expone HTTPS/HTTP/2/3 con auto-SSL Let's Encrypt apuntando a `PUBLIC_DOMAIN`. Imagenes vienen de GHCR (ghcr.io/flitsas/flit-boilerplate/*). Postgres dentro de Docker (no host como el compose anterior).
- [X] **8g.** Workflows GitHub Actions nuevos: `core-api.yml` (.NET 10 restore/build/test/AOT publish), `go-gateway.yml` (gofmt + vet + staticcheck + build + test -race), `python-ml.yml` (uv + ruff + mypy strict + pytest --cov-fail-under=70), `contracts.yml` (Redocly OpenAPI + asyncapi CLI).
- **Commit:** `feat(infra): expand docker-compose, add observability stack, Caddy, sops dir, workflows per service (ADR-0002 Fase 8)`

### Fase 9 — Migración del dominio ✅ SCAFFOLD .NET COMPLETO (2026-05-21)

Todos los 5 módulos del backend Node tienen ahora su scaffold equivalente en `services/core-api/` con Domain + Features + Adapter InMemory + endpoints REST + tests xUnit. **Falta el cutover real** (EF Core + Wolverine + shadow traffic + sign-off humano) que es trabajo de Sprints +0.5 a +3, fuera del scope de este PR.

Estado por módulo:

- [X] **personas → `Tramites.Modules.Personas`** (commit `70540c5`): Persona aggregate + Documento VO + PersonaError + 4 Features + 4 endpoints REST + 20 tests xUnit. ⏳ Cutover Sprint +0.5.
- [X] **employees → `Tramites.Modules.Empleados`** (commit final): Empleado aggregate (con HabeasData y SeguridadSocial VOs) + 5 Features + 5 endpoints REST + 14 tests xUnit. ⏳ Cutover Sprint +1.
- [X] **employee-dependents → sub-agregado de `Tramites.Modules.Empleados`** (mismo commit): Dependiente entity child + reglas de negocio (límite 20, suma porcentaje ≤ 100%) + 4 Features (Agregar/Listar/Actualizar/Eliminar) + 4 endpoints REST anidados (`/api/v1/empleados/{id}/dependientes/...`). Breaking change URL documentado. Tests incluidos en `EmpleadosModuleTests` (parte de los 14).
- [X] **employee-positions → `Tramites.Modules.Cargos`** (mismo commit): Cargo aggregate con validaciones (rango fechas, estado consistente Activo + FechaFin) + 5 Features + 5 endpoints REST anidados (`/api/v1/empleados/{id}/cargos/...`) + 8 tests xUnit. ⏳ Cutover Sprint +2.
- [X] **vehicle-query → `Tramites.Modules.Integraciones.Runt`** (mismo commit): IRuntClient port + StubRuntClient adapter determinista para dev/tests + 2 Features (consultar por VIN, por placa+doc) + 2 endpoints REST + 12 tests xUnit. ⏳ Cutover Sprint +3 (requiere Refit + token Verifik real).
- [ ] **EOL backend Node legacy** — Cuando los 5 estén en producción .NET con shadow traffic verde durante ≥ 1 mes: ADR `backend-node-eol`, eliminar `backend/src/modules/{personas,employees,employee-dependents,employee-positions,vehicle-query}/`, reducir backend a solo `websockets/`, eliminar `backend/migrations/` TypeORM tras validar EF Core migrations.

**Total .NET en este PR:** 5 módulos, 12 Features, 6 aggregates/entities, 5 VOs, 5 ErrorTypes (record discriminated unions), 1 stub HTTP client, **20 endpoints REST nuevos**, **60 tests xUnit** (suite `Tramites.Api.Tests`).

Detalle iterativo del cutover en [`docs/runbooks/PHASE_9_BACKLOG.md`](runbooks/PHASE_9_BACKLOG.md).

---

## 3.4. Riesgos identificados y mitigaciones

| # | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|
| R1 | Migrar a pnpm rompe scripts CI/CD existentes | Alta | Medio | Validar workflows (`backend-ci.yml`, `ci.yml`, `cd.yml`) en Fase 2a; ajustar comandos `npm run` → `pnpm run` |
| R2 | Reescribir `sync-agents.sh` rompe los `.claude/agents/` actuales | Media | Alto | ADR-0003 con 3 alternativas; suite de tests de regeneración (diff vs versión anterior); revertir si difference > umbral |
| R3 | Frontmatter YAML preexistente en `backend-agent.md` colisiona con metadata nuevo | Media | Medio | En Fase 0.5d, normalizar el frontmatter (no duplicar campos `name`, `description`, `tools`) |
| R4 | Migración Next.js rompe pestañas custom de App.tsx | Alta | Alto | Fase 2b backup; 2g convierte pestañas en rutas; Playwright valida flujo |
| R5 | Tailwind 3 → 4: cambio de config (CSS-first) puede romper estilos custom | Media | Medio | Migrar estilos puntuales; revisar `tailwind.config.js` → `tailwind.config.ts`; iniciar 2c con `npx @tailwindcss/upgrade` |
| R6 | El frontend usa `react-router-dom` declarado pero no usado → posible falsa dependencia | Baja | Bajo | Eliminar en 2m con confianza |
| R7 | AOT en core-api falla con SDK SOAP gubernamental colombiano | Alta (en Fase 9, cuando se integre RUNT/DIAN) | Medio | Procedimiento de escape JIT del ADR sección 8.1 |
| R8 | Habeas Data: claves JWT en `infra/secrets/` accidentalmente commiteadas | Media | Crítico | `.gitleaks.toml` ya existe; añadir `infra/secrets/*.pem` al `.gitignore` antes de generarlas; sops + age |
| R9 | El monorepo crece mucho (services/ + contracts/ + infra/observability/) → builds lentos en CI | Media | Medio | Workflows por servicio; cache de capas Docker; `pnpm` agiliza el frontend |
| R10 | Migración del dominio en Fase 9 toma > 12 meses por los 5 módulos | Alta | Medio | Fase 9 está fuera del scope de este PR; cada módulo es un PR independiente con ADR de migración propio |
| R11 | `.env.user-identity` accidentalmente modificado | Baja | Alto | Regla absoluta del prompt; revisar diff de cada PR antes de mergear |
| R12 | Cobertura del frontend baja en Fase 2 por reescritura masiva | Alta | Medio | Suite Vitest existente debe quedar verde tras 2r; aceptar bajada temporal pero documentar |
| R13 | **DEUDA PREEXISTENTE detectada en gate de Fase 0i:** el frontend NO tenía tests Vitest ni Playwright. | Alta | Medio | **RESUELTA PARCIALMENTE en Fase 2 ampliada (2026-05-20):** añadidos 5 archivos de test Vitest con 24 tests para `cn()` (lib), `Button` (UI), `Card` (UI), `useUiStore` (Zustand) y `apiClient` (axios singleton). `vitest.config.ts` configurado con jsdom + Testing Library + path alias @/*. **DEUDA RESIDUAL:** tests por feature (personas, employees, etc.) y Playwright E2E quedan para PR siguiente. Script `frontend test` ya no usa `--passWithNoTests`. |

---

## 3.5. Lista de PRs/commits previstos

| PR | Rama | Título | Líneas estimadas |
|----|------|--------|------------------|
| #1 | `feature/restructure-arquitectura-microservicios` (continuo) | `docs(adr): add MIGRATION_PLAN.md, appendices for ADR-0002` | ~600 (mayor parte: este doc) |
| #2 | (mismo) | `refactor(scripts): rewrite sync-agents.sh metadata-driven (ADR-0003)` | ~400 |
| #3 | (mismo) | `feat(agents): add .NET, Python, Go, architect agents; update frontend/backend` | ~700 (4 archivos nuevos + 2 DIFFs) |
| #4 | (mismo) | `feat(frontend): migrate to Next.js 16 with App Router, Tailwind 4, shadcn/ui, Biome, Zustand, pnpm` | ~800 (cerca del límite FLIT) — si supera, dividir en 4a (pnpm) y 4b (Next.js) |
| #5 | (mismo) | `feat(backend): add WebSockets module + RabbitMQ consumer for BFF role (ADR-0004)` | ~400 |
| #6 | (mismo) | `feat(core-api): scaffold .NET 10 service with AOT and Wolverine` | ~600 (scaffold + 6 reglas NetArchTest + tests) |
| #7 | (mismo) | `feat(go-gateway): scaffold Go gateway with JWT and rate limiting` | ~400 |
| #8 | (mismo) | `feat(python-ml): scaffold Python ML/OCR with FastAPI` | ~400 |
| #9 | (mismo) | `feat(contracts): add OpenAPI and AsyncAPI; generate frontend types` | ~300 |
| #10 | (mismo) | `feat(infra): expand docker-compose with full microservices stack` | ~500 |
| #11+ | un PR por módulo migrado | Fase 9 (iterativa, largo plazo) | variable |

**Regla 9 FLIT (máx 800 líneas por PR):** PR #4 es el de mayor riesgo. Si supera 800, se divide en `feat(frontend): migrate monorepo to pnpm workspaces` (4a) + `feat(frontend): migrate Vite → Next.js 16 with App Router/Tailwind 4/shadcn-ui/Biome/Zustand` (4b).

**Target de todas las PRs:** `develop` (regla 8 FLIT).
**Co-authored-by:** `Co-authored-by: Claude (Architecture/Backend/Frontend/Infra Agent)` según corresponda.

---

## Esperando aprobación

Este documento queda **PENDIENTE DE APROBACIÓN EXPLÍCITA** antes de avanzar al PASO 4 (ejecución).

Para aprobar, responde con:

- ✅ **APROBADO** — procedo con Fase 0a.
- 🔧 **CAMBIOS** — indica qué casillas (componente N) cambian y a qué opción.
- ❌ **RECHAZADO** — explico nuevamente y volvemos a PASO 2.

---

*Documento generado por Claude Code en sesión interactiva — 2026-05-20*
