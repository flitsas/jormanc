# Workflow: Spec-Driven Delivery (spec-kit + Azure DevOps)

**Objetivo:** Desarrollar una funcionalidad con **spec-kit** (spec → plan → tasks → implement) manteniendo la gestión de Features/HU en **Azure DevOps** y los PR/merge en **GitHub**, vía los agentes FLIT existentes.

**Invocación típica:**
```
Quiero desarrollar [funcionalidad] usando spec-kit
/speckit-specify [descripción de la funcionalidad]
```

**Relación con `requirement-to-delivery.md`:** este workflow es la alternativa spec-driven a las Fases 1–3 (Feature/diseño/HUs). La implementación (Fase 4+) reusa el sub-workflow `implement-story.md` o `/speckit-implement`, y la integración/deploy son idénticas.

---

## Precondiciones

- spec-kit inicializado (`.specify/` presente) y constitución vigente (`.specify/memory/constitution.md`).
- En Windows, exportar `PYTHONUTF8=1` antes de comandos `specify` por consola.
- Acceso a Azure DevOps (MCP o REST) y `.env.user-identity` con credenciales (ver `flit-azure-devops`).
- Convenciones de datos vigentes: `docs/database-conventions.md`, `docs/data-access-conventions.md`.

---

## Cadena (visión general)

```mermaid
flowchart TD
  C[/speckit-constitution/] --> S[/speckit-specify → spec.md/]
  S --> BR1[flit-spec-to-ado A → feature-creator → Feature ADO]
  BR1 --> CL[/speckit-clarify (opcional)/]
  CL --> P[/speckit-plan → plan.md/]
  P --> T[/speckit-tasks → tasks.md/]
  T --> BR2[flit-spec-to-ado B → flit-crear-hu → HUs ADO]
  BR2 --> AN[/speckit-analyze (opcional)/]
  AN --> IMP[/speckit-implement → código/]
  IMP --> DT[dev-tester → Evidences ADO]
  DT --> REV[code-review + security]
  REV --> INT[integration-agent → PR + Custom.Commits]
  INT --> INF[infra-agent → deploy DEV]
```

---

## Fases — resumen

| # | Fase | Comando / Agente | Gate humano |
|---|------|------------------|-------------|
| 0 | Constitución | `/speckit-constitution` | Solo si cambian principios |
| 1 | Especificar | `/speckit-specify` + `flit-spec-to-ado` (A) → `feature-creator` | **Aprobar borrador del Feature antes de crear en ADO** |
| 2 | Clarificar *(opcional)* | `/speckit-clarify` | — |
| 3 | Planear | `/speckit-plan` | Revisar plan vs stack/ADRs |
| 4 | Tasks → HUs | `/speckit-tasks` + `flit-spec-to-ado` (B) → `flit-crear-hu` | **Aprobar HUs; activar HU es humano** |
| 5 | Analizar *(opcional)* | `/speckit-analyze` | — |
| 6 | Implementar | `/speckit-implement` (o `implement-story.md`) | Confirmar activación de cada HU |
| 7 | Tests + evidencias | `dev-tester` / `qa-agent` | — |
| 8 | Review + integración | `code-review-agent` + `security-agent` + `integration-agent` | **Confirmación humana para cada merge** |
| 9 | Deploy DEV | `infra-agent` | — (post-merge) |

---

## Fase 1 — Especificar y crear el Feature en ADO

1. Ejecutar `/speckit-specify [descripción]` → genera `specs/NNN-slug/spec.md` y su checklist.
2. Invocar la skill **`flit-spec-to-ado` (Modo A)**: mapea el `spec.md` al formato de `feature-creator`
   (User Scenarios → OBJETIVO, Functional Requirements/Key Entities → DESCRIPTION, Success Criteria → CRITERIOS FUNCIONALES).
3. **Gate:** presentar el borrador del Feature al humano. Tras "sí", `feature-creator` lo crea en ADO.
4. Escribir `specs/NNN-slug/ado-link.json` con `featureId` y dejar comentario `[spec-kit]` en el Discussion del Feature.

**Output:** `spec.md` + Feature ADO con ID + `ado-link.json`.

---

## Fase 2 — Clarificar (opcional)

`/speckit-clarify` para resolver ambigüedades antes de planear. Actualiza `spec.md`. Sin gate.

---

## Fase 3 — Planear

`/speckit-plan` → `plan.md`. Debe alinearse al **stack fijo** (Principio I de la constitución: .NET 10 `core-api`, Python 3.13 `python-ml`, Next.js 16 `frontend`) y a los ADR vigentes en `docs/decisions/`. Si introduce entidades/tablas nuevas, marcar la necesidad de schema (HU `[BACKEND]` de migración → `database-agent`).

**Output:** `plan.md` (+ `data-model.md`, `research.md`, `contracts/` si aplica).

---

## Fase 4 — Tasks → Historias de Usuario en ADO

1. `/speckit-tasks` → `tasks.md`.
2. Invocar **`flit-spec-to-ado` (Modo B)** (equivalente a `/speckit-taskstoissues`, reenrutado a ADO):
   agrupa tasks por capa `[FRONTEND]`/`[BACKEND]` en HUs y delega cada una en `flit-crear-hu`
   (Como/quiero/para, AC Gherkin, Story Points Fibonacci, vínculo al Feature padre).
3. **Gate:** aprobar las HUs antes de crearlas; **activar** cada HU es decisión humana.
4. Acumular `userStoryIds` en `ado-link.json`.

> ⚠️ Nunca crear GitHub Issues. La gestión de work items es exclusivamente Azure DevOps.

**Output:** N HUs ADO vinculadas al Feature + `ado-link.json` actualizado.

---

## Fase 5 — Analizar (opcional)

`/speckit-analyze` para consistencia entre `spec.md`, `plan.md` y `tasks.md` antes de implementar. Sin gate.

---

## Fase 6 — Implementar

Motor de implementación: **`/speckit-implement`**, guiado por la constitución (stack y convenciones FLIT).
- Confirmar la **activación** de cada HU en ADO antes de empezar (gate humano).
- Para HUs con persistencia, materializar/validar schema con `database-agent` + `db-schema-validator` (BLOCKED detiene el merge).
- Alternativa: usar el sub-workflow `implement-story.md` por HU si se prefiere el pipeline clásico FLIT.

**Output:** código en rama `feature/AB-{id}-slug`, commits `HU{id}: ...`.

---

## Fase 7 — Tests y evidencias

`dev-tester` (unitarios + `Custom.Evidences` en ADO) y `qa-agent`/`playwright-runner` (E2E). La HU pasa a `Resolved` vía `flit-gestion-hu` (no lo hace integration-agent).

---

## Fase 8 — Review e integración

1. `code-review-agent` (+ `flit-conventions-validator`) y `security-agent`.
2. `integration-agent` **Modo A**: crear PR a `develop` + `Custom.Commits` + añadir `prUrl` a `ado-link.json`.
3. **Gate de merge:** requiere "sí" humano y ≥1 reviewer humano. Tras merge, `integration-agent` **Modo B**: Deploy* + Commits "Integrado".

---

## Fase 9 — Deploy DEV

`infra-agent` despliega a DEV tras el merge. El cierre del Feature es exclusivo del Product Owner humano.

---

## Restricciones del flujo

- Gestión de Features/HU **siempre** en Azure DevOps; `/speckit-taskstoissues` reenrutado a `flit-spec-to-ado`. Nunca GitHub Issues.
- Respetar todos los gates humanos: aprobar borradores, activar HU, merge, cerrar Feature.
- spec-kit produce artefactos de ingeniería; los agentes FLIT son dueños de ADO y de PR/merge/deploy.
- `ado-link.json` es la fuente de verdad de la correspondencia spec dir ↔ work items ADO.
- Ante conflicto entre la constitución y `AGENTS.md`/`agent-templates/`, prevalece `AGENTS.md`.
