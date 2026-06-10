# AGENTS.md — FLIT Monorepo

> Guía del monorepo para agentes IA. Editar directamente en este repo.
> Fuente operativa de agentes y skills en Cursor: `.cursor/agents/`, `.cursor/skills/`, `.cursor/rules/`.
> Convenciones corporativas: `agent-templates/conventions.md`.
>
> Consumido por **OpenAI Codex**, **JetBrains Junie** y **Google Antigravity** ([agents.md](https://agents.md/)).
> Para Cursor: `.cursor/agents/` y `.cursor/rules/`. Para GitHub Copilot: `.github/copilot-instructions.md`.

## Stack (post ADR-0014, 2026-05-27)

| Capa | Tecnología | Path |
|---|---|---|
| Backend (dominio + Gateway YARP + Files MinIO + SignalR + RUNT + PDF) | **.NET 10** + C# 14 + Wolverine + EF Core 10 + AOT | `services/core-api/` |
| OCR / ML | **Python 3.13** + FastAPI + uv | `services/python-ml/` |
| Frontend | **Next.js 16** + React 19 + Tailwind 4 + Biome | `frontend/` |
| Gestión | Azure DevOps Boards (Feature → US → Task → Bug) | — |
| Calidad | Biome (frontend) + dotnet format + Ruff + SonarCloud | raíz + por servicio |

## Comandos clave

```bash
# Setup inicial (una vez)
pnpm install:all                  # deps de los 3 lenguajes
./scripts/gen-secrets.sh          # JWT + passwords containers
./scripts/setup-env.sh --interactive  # AZURE/SONAR tokens

# Dev (frontend + core-api + gateway + python-ml en paralelo)
pnpm dev

# Tests
pnpm test                         # .NET + Python + Frontend
pnpm test:e2e                     # Playwright en frontend

# Lint / format
pnpm lint                         # biome (frontend) + dotnet format + ruff
pnpm format                       # biome format + dotnet format + ruff format

# Migraciones (core-api dueño)
pnpm migrate
```

## Convenciones de datos

| Documento | Contenido |
|---|---|
| `docs/database-conventions.md` | Schema PostgreSQL: schemas, nomenclatura, RLS, PII, migraciones |
| `docs/data-access-conventions.md` | Repositorios, EF Core, tenant context, soft delete, transacciones |

Validación automática pre-merge: skill `db-schema-validator` (invocada por `database-agent`).

## Convenciones FLIT (las 18 reglas innegociables — post-refactor)

> Fuente canónica: `agent-templates/conventions.md`. Reescrituras post 2026-05-22:

- **Branches:** `feature/AB-1234-descripcion`
- **Commits:** `HU1234: descripción breve` (ejemplo: `HU1234: Agregar validación de login`)

Resumen del resto:

1. Sprint = siguiente al activo (NUNCA el activo)
2. Tag `DOR` obligatorio antes de `Active`
3. Story Points: Fibonacci estricto (1, 2, 3, 5, 8) — NO 4, 6, 7
4. AssignedTo: humano (NUNCA agente, NUNCA vacío)
5. Sin datos sensibles en descripciones
6. Sin placeholders en items `Active`
7. Bugs PDN → Líder Técnico (NUNCA al dev)
8. PRs target: `develop`, máx 800 líneas
9. Cero threads sin resolver para mergear
10. Code Review + Security `succeeded`
11. ≥1 reviewer humano
12. Sin `git push --force` en branches compartidos
13. ADRs en `Propuesto` (solo Líder Técnico humano promueve a `Aceptado`)
14. Co-authored-by completo en commits de merge
15. `Closed` de Feature: exclusivo del PO humano
16. Skills externas: auditadas antes de instalar (5 pasos en `conventions.md`)

## Agentes disponibles

| ID | Descripción | Scope |
|---|---|---|
| `architecture-agent` | Diseño técnico, ADRs, OpenAPI, modelo de datos conceptual | Setup |
| `database-agent` | Schema PostgreSQL, migraciones, RLS, catálogos; dueño de convenciones de datos | Setup / Datos |
| `tech-lead-agent` | Features, descomposición HUs, DoR/DoD, deuda técnica | Transversal |
| `backend-agent` | Use cases, repositorios EF Core, APIs | Implementación |
| `frontend-agent` | UI React/Next.js | Implementación |
| `code-review-agent` | Review formal de PRs | Calidad |
| `security-agent` | SAST, SCA, secretos, Habeas Data | Seguridad |
| `qa-agent` | TCs, E2E, bugs | QA |
| `integration-agent` | PRs GitHub, trazabilidad ADO, merge | Integración |
| `infra-agent` | Docker, CI/CD, deploy | Infra |
| `orchestrator-agent` | Flujos end-to-end | Coordinación |

## Skills

| ID | Descripción |
|---|---|
| `db-schema-validator` | Valida migraciones y capa de repositorio contra convenciones de datos |
| `dev-tester` | Tests unitarios y evidencias ADO (PASO 6) |
| `flit-azure-devops` | Lectura/escritura work items ADO |
| `flit-conventions-validator` | Convenciones FLIT en PRs (7 dimensiones) |
| `flit-dor-dod-validator` | DoR/DoD por transición de estado |
| `flit-integration-ado` | Custom.Commits, Deploy DEV/QA/PDN |
| `flit-spec-to-ado` | Puente spec-kit → ADO: `spec.md`→Feature, `tasks.md`→HU |

## Capa spec-driven (spec-kit)

Instalada con `specify init --here --ai cursor-agent` (skills bajo `.cursor/skills/speckit-*`, constitución en `.specify/memory/constitution.md`, scripts PowerShell en `.specify/scripts/powershell/`). **Aditiva**: no reemplaza ningún agente/skill FLIT.

Flujo: `constitution → /speckit-specify → (/speckit-clarify) → /speckit-plan → /speckit-tasks → [puente ADO] → /speckit-implement → dev-tester → integration-agent`.

| Fase spec-kit | Salida | Puente / agente FLIT |
|---|---|---|
| `/speckit-specify` | `specs/NNN-*/spec.md` | `flit-spec-to-ado` (Modo A) → `feature-creator` crea **Feature** ADO |
| `/speckit-plan` | `plan.md` | alineado al stack y ADRs (ver constitución) |
| `/speckit-tasks` | `tasks.md` | `flit-spec-to-ado` (Modo B) → `flit-crear-hu` crea **HU** ADO |
| `/speckit-taskstoissues` | — | **reenrutado a ADO** (override FLIT; nunca GitHub Issues) |
| `/speckit-implement` | código | motor de implementación, guiado por la constitución |

- **Constitución FLIT:** `.specify/memory/constitution.md` (18 reglas + stack + gates). Fuente canónica sigue siendo `AGENTS.md`/`agent-templates/`.
- **Gestión de work items:** SIEMPRE Azure DevOps Boards. La trazabilidad spec ↔ ADO vive en `specs/NNN-*/ado-link.json`.
- Workflow end-to-end: `.cursor/workflows/spec-driven-delivery.md`.

## Slash commands

| Trigger | Descripción |
|---|---|


## No-go zones (archivos/carpetas que NO se modifican sin OK humano)

- `agent-templates/` — corporativo FLIT
- `.env`, `.env.user-identity`, `.env.verifik`, `infra/secrets/`
- `.ai/_legacy/`, `.ai/_generated/` — auto-generados, gitignored
- `docs/decisions/ADR-*.md` en estado `Aceptado` (solo Líder Técnico)
- Migraciones EF Core ya aplicadas

## Discovery por IDE

| IDE | Archivo que consume |
|---|---|
| OpenAI Codex CLI | `AGENTS.md` (este archivo) + `services/<x>/AGENTS.override.md` |
| JetBrains Junie | `.junie/AGENTS.md` (copia de este archivo) |
| Google Antigravity | `.agents/rules/` + `.agents/workflows/` + `.agents/skills/` |
| Claude Code | `.claude/agents/`, `.claude/commands/`, `.claude/skills/` |
| Cursor | `.cursor/agents/`, `.cursor/skills/`, `.cursor/rules/*.mdc` |
| GitHub Copilot | `.github/copilot-instructions.md` + `.github/instructions/*.instructions.md` |

## Mantenimiento de agentes

Edita directamente `.cursor/agents/`, `.cursor/skills/` y las reglas en `.cursor/rules/`. No hay paso de sincronización automática en CI.
