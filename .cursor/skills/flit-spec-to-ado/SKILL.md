---
name: flit-spec-to-ado
description: Puente entre los artefactos de spec-kit (spec.md, plan.md, tasks.md) y Azure DevOps Boards. Convierte una especificación en un Feature ADO y sus tasks en Historias de Usuario, reutilizando feature-creator, flit-crear-hu y el contrato flit-azure-devops. Es la ruta oficial de gestión de work items en el flujo spec-driven; reemplaza la creación de GitHub Issues. Invocar tras /speckit-specify (Feature) y tras /speckit-tasks (HU). Triggers spec-kit, taskstoissues, puente ADO, spec a Feature, tasks a HU, flit-spec-to-ado.
---

# Puente spec-kit → Azure DevOps

Convierte los artefactos de spec-kit en work items de ADO **sin duplicar** lógica de autenticación ni de creación. Este contrato **orquesta** las skills existentes:

| Artefacto spec-kit | Work item ADO | Skill que ejecuta la creación |
|--------------------|---------------|-------------------------------|
| `spec.md` | **Feature** | `feature-creator` |
| `tasks.md` (agrupadas por capa) | **Historia de Usuario** | `flit-crear-hu` |
| Auth / REST / encoding / idempotencia | — | `flit-azure-devops` (contrato base, obligatorio) |

> **PROHIBIDO** crear GitHub Issues como work items. La gestión de historias/features de FLIT vive en Azure DevOps Boards.

## Requisitos previos

1. Proyecto spec-kit inicializado (`.specify/`) con un feature activo. Leer `.specify/feature.json` → `feature_directory` (p. ej. `specs/003-user-auth`).
2. `.env.user-identity` (ver `flit-azure-devops`): `USER_REAL_NAME`, `USER_REAL_EMAIL`, `AZURE_ORG_URL`, `AZURE_PROJECT_NAME`, `AZURE_PAT`.
3. Si falta conexión/credenciales ADO → seguir el fallback de `flit-azure-devops` (entregar borradores `.md` locales; **no** intentar GitHub Issues).

## Modo A — `spec.md` → Feature ADO (tras `/speckit-specify`)

1. Leer `SPEC_FILE` = `<feature_directory>/spec.md`.
2. **Mapeo a la plantilla de `feature-creator`** (formato OBJETIVO / DESCRIPTION / CRITERIOS FUNCIONALES):

   | Sección de `spec.md` | Campo del Feature |
   |----------------------|-------------------|
   | Título / nombre corto del feature | `Título` → `[MÓDULO] - Descripción` |
   | *Primary User Scenario* / *User Scenarios* | `# OBJETIVO` |
   | *Functional Requirements* + *Key Entities* | `# DESCRIPTION` |
   | *Success Criteria* (medibles) | `# CRITERIOS FUNCIONALES` (lista `- [ ]`) |

3. Determinar el `[MÓDULO]` desde el dominio del spec (p. ej. `TRAMITES`, `HOME`, `DASHBOARD`).
4. Invocar **`feature-creator`** con ese borrador. Respetar su gate: **no registrar** en ADO hasta aprobación explícita de `USER_REAL_NAME`.
5. Tras crear el Feature, capturar `featureId`.
6. **Trazabilidad bidireccional**:
   - Escribir `<feature_directory>/ado-link.json`:
     ```json
     { "featureId": 0, "featureUrl": "", "userStoryIds": [], "prUrls": [], "specDir": "specs/NNN-slug" }
     ```
   - Comentario en el Discussion del Feature (vía `flit-azure-devops`, `System.History`):
     `<div>[spec-kit] Feature generado desde <b>specs/NNN-slug/spec.md</b></div>`

## Modo B — `tasks.md` → Historias de Usuario ADO (tras `/speckit-tasks`)

> Requiere `ado-link.json` con `featureId` (ejecutar Modo A primero). El Feature padre debería estar `Active` o superior.

1. Leer `<feature_directory>/tasks.md`, `plan.md` (contexto técnico) y `spec.md` (escenarios de aceptación).
2. **Agrupar tasks en Historias de Usuario** por capa y unidad funcional entregable:
   - Una HU `[FRONTEND]` agrupa las tasks de UI/cliente de una misma funcionalidad.
   - Una HU `[BACKEND]` agrupa use cases / repos / API / migraciones de una misma funcionalidad.
   - Evitar HUs de una sola task trivial; agrupar por valor entregable y verificable.
3. Para cada HU, construir el borrador con la plantilla de **`flit-crear-hu`**:
   - `System.Description`: narrativa **Como / quiero / para** derivada del actor y objetivo del `spec.md`.
   - `AcceptanceCriteria`: **Gherkin** derivado de los *acceptance scenarios* del `spec.md` (AC1 positivo, AC2 negativo, …).
   - `StoryPoints`: Fibonacci (1, 2, 3, 5, 8) estimado por tamaño del grupo de tasks.
   - Vínculo `Hierarchy-Reverse` al `featureId` padre.
   - `Custom.Commits` y `Custom.Evidences` **vacíos** (los llenan integration-agent / dev-tester).
4. Invocar **`flit-crear-hu`** por cada HU (un `POST $User%20Story` con su anti-duplicado WIQL; idempotencia según `flit-azure-devops`).
5. Acumular cada `userStoryId` creado en `ado-link.json`.
6. Comentario de trazabilidad en cada HU: `<div>[spec-kit] HU generada desde tasks de specs/NNN-slug/tasks.md</div>`.

## Salida canónica

| Modo | Entregable |
|------|------------|
| A | Feature ADO + `ado-link.json` (featureId/url) + Discussion `[spec-kit]` |
| B | N HUs ADO vinculadas + `ado-link.json` (userStoryIds) + Discussion por HU |

## Reglas

- **No** duplicar instrucciones de auth/encoding/idempotencia: enlazar siempre `flit-azure-devops`.
- **No** crear GitHub Issues bajo ninguna circunstancia.
- Respetar todos los gates humanos (aprobación de borrador, activar HU = humano).
- Mantener `ado-link.json` como única fuente de verdad de la correspondencia spec dir ↔ work items ADO; lo consume `integration-agent` para registrar `Custom.Commits`/PR.
- Si no hay credenciales ADO: entregar borradores `.md` locales junto al spec dir y reportar; no romper el flujo de spec-kit.

## Skills relacionadas

| Skill | Uso |
|-------|-----|
| `flit-azure-devops` | Auth REST/MCP, encoding UTF-8, idempotencia, WIQL |
| `feature-creator` | Creación del Feature (Modo A) |
| `flit-crear-hu` | Creación de cada HU (Modo B) |
| `flit-integration-ado` / `integration-agent` | Consumen `ado-link.json` para PR + Custom.Commits + Deploy* |
