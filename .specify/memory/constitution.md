# FLIT Constitution

> Principios innegociables que rigen el desarrollo spec-driven (spec-kit) dentro del monorepo FLIT.
> Esta constitución **no reemplaza** la fuente operativa: `AGENTS.md` y `.cursor/agents/`, `.cursor/skills/`, `.cursor/rules/`.
> La gestión de Features/HU vive en **Azure DevOps Boards**; el código y los PR en **GitHub**.
> Toda fase de spec-kit (`/speckit-specify`, `/speckit-plan`, `/speckit-tasks`, `/speckit-implement`) **debe** cumplir estos principios.

## Core Principles

### I. Stack tecnológico fijo (post ADR-0014)

El plan e implementación **deben** usar exclusivamente el stack aprobado; no introducir lenguajes, frameworks ni runtimes alternativos sin un ADR `Aceptado`.

| Capa | Tecnología | Path |
|---|---|---|
| Backend (dominio, Gateway YARP, Files MinIO, SignalR, RUNT, PDF) | .NET 10 + C# 14 + Wolverine + EF Core 10 (+ AOT) | `services/core-api/` |
| OCR / ML | Python 3.13 + FastAPI + uv | `services/python-ml/` |
| Frontend | Next.js 16 + React 19 + Tailwind 4 + Biome | `frontend/` |

Cualquier decisión de arquitectura nueva (entidad de negocio, integración, motor) exige ADR en `docs/decisions/` (estado `Propuesto`; solo el Líder Técnico humano promueve a `Aceptado`).

### II. Spec-driven con gestión en Azure DevOps (NO GitHub Issues)

El ciclo es `constitution → specify → (clarify) → plan → tasks → [puente ADO] → implement → dev-tester → integration`.
- `spec.md` (el **qué/por qué**) origina un **Feature** en ADO vía `feature-creator`.
- `tasks.md` origina **Historias de Usuario** en ADO vía `flit-crear-hu`, agrupando por capa `[FRONTEND]` / `[BACKEND]`.
- El puente es la skill `flit-spec-to-ado`, que reusa el contrato `flit-azure-devops` (MCP → REST → CLI → fallback `.md`).
- **PROHIBIDO** crear GitHub Issues como work items. `/speckit-taskstoissues` está reenrutado a ADO.

### III. Gates humanos innegociables

Ninguna fase automatizada los omite, ni siquiera ante un "hazlo igual":
- **Activar HU** (pasar a `Active`): requiere confirmación humana explícita y tag `DOR`.
- **Merge de PR**: requiere "sí" textual del humano/Líder Técnico y ≥1 reviewer humano.
- **Cerrar Feature**: exclusivo del Product Owner humano.
- `AssignedTo` siempre humano (nunca un agente, nunca vacío).

### IV. Calidad y pruebas antes de integrar

- Tests unitarios + evidencias en ADO (`Custom.Evidences`) vía `dev-tester` antes de marcar la HU `Resolved`.
- E2E con Playwright para flujos de usuario (`qa-agent` / `playwright-runner`).
- Pre-merge verde: Code Review + Security `succeeded`, cero threads sin resolver.
- Persistencia: toda migración/repositorio valida contra `docs/database-conventions.md` y `docs/data-access-conventions.md` mediante `db-schema-validator` (estado `BLOCKED` detiene el merge).

### V. Seguridad y privacidad (Habeas Data)

- Sin datos sensibles ni secretos en descripciones, specs, planes o commits.
- SAST/SCA/secretos vía `security-agent` (`flit-inline-security-detector`, gitleaks) antes del merge.
- Cumplimiento Habeas Data en cualquier manejo de PII; respetar `docs/data-retention.md`.

## Additional Constraints — Convenciones FLIT

- **Branches:** `feature/AB-1234-descripcion`. Nunca `git push --force` / `--force-with-lease` en ramas compartidas.
- **Commits:** `HU1234: descripción breve`. `Co-authored-by` completo en commits de merge.
- **PRs:** target `develop` (flujo de HU); máximo 800 líneas.
- **Sprint:** el siguiente al activo (NUNCA el activo). **Story Points:** Fibonacci estricto (1, 2, 3, 5, 8) — NO 4, 6, 7.
- **Bugs en PDN** → Líder Técnico (nunca directo al desarrollador).
- **Skills externas:** auditadas antes de instalar (procedimiento de `AGENTS.md`).
- **No-go zones** (no se modifican sin OK humano): `agent-templates/`, `.env*`, `infra/secrets/`, `.ai/_legacy/`, `.ai/_generated/`, ADRs `Aceptado`, migraciones EF Core ya aplicadas.

## Development Workflow — capa spec-driven

1. `/speckit-constitution` — mantiene este documento.
2. `/speckit-specify <descripción>` → `specs/NNN-*/spec.md` → `flit-spec-to-ado` crea el **Feature** ADO (gate de aprobación del borrador).
3. `/speckit-clarify` (opcional) — de-risk antes de planear.
4. `/speckit-plan` → `plan.md` alineado al stack (Principio I) y a los ADR vigentes.
5. `/speckit-tasks` → `tasks.md` → `flit-spec-to-ado` crea las **HU** ADO (gate: activar HU es humano).
6. `/speckit-analyze` (opcional) — consistencia entre artefactos.
7. `/speckit-implement` — implementa guiado por esta constitución (stack fijo, convenciones).
8. `dev-tester` → evidencias ADO; `code-review-agent` + `security-agent` → calidad.
9. `integration-agent` → PR GitHub + `Custom.Commits` + Deploy* en ADO (Modo A/B). El merge y el deploy real son gates humanos / `infra-agent`.

## Governance

- Esta constitución gobierna las fases de spec-kit; la **fuente canónica operativa** sigue siendo `AGENTS.md` y los archivos en `agent-templates/` (`definition-of-ready.md`, `definition-of-done.md`, `state-transitions.md`, `code-style-guide.md`, `security-checklist.md`).
- Ante conflicto entre esta constitución y `AGENTS.md`/`agent-templates/`, **prevalece `AGENTS.md`** y debe abrirse una enmienda aquí.
- Las enmiendas requieren PR, revisión humana y registro en el historial de versión. Los ADR solo los promueve a `Aceptado` el Líder Técnico humano.
- Todo PR/review verifica cumplimiento de estos principios; la complejidad añadida debe justificarse.

**Version**: 1.0.0 | **Ratified**: 2026-06-09 | **Last Amended**: 2026-06-09
