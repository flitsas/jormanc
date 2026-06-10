# ADR-0008: Agente especializado de base de datos y convenciones normativas de persistencia

**Fecha**: 2026-06-02
**Status**: Propuesto
**Deciders**: Líder Técnico FLIT
**Tags**: arquitectura, backend, datos, agentes-ia, postgresql, ef-core, multi-tenant

---

## Contexto

FLIT opera sobre **PostgreSQL 17+** con **multi-tenancy compartido** (`tenant_id` + RLS), **UUID v7**, soft delete, auditoría transversal y cumplimiento **Habeas Data** (Ley 1581). El stack backend canónico es **.NET 10 + EF Core 10** en `services/core-api/` ([ADR-0004](ADR-0004-consolidacion-stack-dotnet-python.md)), con Clean Architecture ([ADR-0001](ADR-0001-clean-architecture-backend.md)).

Hasta junio 2026, el diseño de schema y las migraciones recaían implícitamente en el `architecture-agent` (DDL de referencia en diseños) y en el `backend-agent` (implementación de repositorios y migraciones EF Core). No existía:

- Un documento normativo único de convenciones de BD verificable por máquina.
- Convenciones explícitas de la capa de repositorio y acceso a datos.
- Un agente con ownership del schema, RLS, triggers, índices y catálogos.
- Validación automática pre-merge de migraciones (`db-schema-validator`).

El dominio (trámites de tránsito y vehículos en Colombia) exige catálogos regulatorios (DIVIPOLA, RUNT, CNT), PII etiquetada, constraints de placa vehicular y aislamiento estricto por tenant. Las violaciones de convención (tablas en `public`, FK sin índice, montos en `float`, ausencia de RLS) son costosas de detectar en code review manual y arriesgan incumplimiento normativo.

Se requiere formalizar **quién decide el modelo**, **quién materializa el DDL** y **quién valida** antes del merge, sin romper la cadena existente de agentes.

---

## Decisión

Introducir el **`database-agent`** como agente especializado dueño de las convenciones de persistencia, junto con:

1. **`docs/database-conventions.md`** — convenciones normativas de schema PostgreSQL.
2. **`docs/data-access-conventions.md`** — convenciones de repositorio y acceso a datos (EF Core 10, Clean Architecture).
3. **Skill `db-schema-validator`** — validación automática pre-merge contra ambos documentos.
4. **Integración en workflows** — Fase 2b en `requirement-to-delivery.md`, Fase 3c en `implement-story.md`, routing explícito en `orchestrator-agent.md`.

**Frontera de responsabilidades:**

| Agente | Rol en persistencia |
|--------|---------------------|
| `architecture-agent` | Modelo de datos **conceptual**, ADR, DDL de referencia |
| `database-agent` | Schema **detallado**, migraciones, RLS, triggers, catálogos, validación DDL |
| `backend-agent` | Repositorios, use cases, EF Core configs (sigue `data-access-conventions.md`) |
| `tech-lead-agent` | HUs de schema antes de HUs dependientes; deuda técnica de schema (Modo D) |

---

## Alternativas consideradas

### Opción 1: Agente `database-agent` dedicado + convenciones normativas + skill validadora

**Descripción**: Agente especializado con 4 modos (diseño schema, migración, validación, catálogos), documentos normativos y skill `db-schema-validator` integrada en el pipeline.

**Pros:**
- Ownership claro del schema; reduce ambigüedad entre arquitectura e implementación.
- Convenciones verificables por máquina antes del merge.
- Alineado con multi-tenant + RLS + PII del dominio tránsito Colombia.
- El `backend-agent` se enfoca en use cases sin mezclar DDL complejo.
- Encaja con el modelo de agentes especializados FLIT v2.0.

**Cons:**
- Un agente más en el pipeline → más pasos de orquestación.
- Requiere mantener sincronizados 3 artefactos (convenciones + agente + skill).
- Curva inicial para el equipo al aprender cuándo invocar `database-agent` vs `backend-agent`.

**Esfuerzo estimado**: M
**Riesgos principales**: Duplicación de trabajo si la frontera con `architecture-agent` no se respeta; agentes que omitan Fase 2b.

---

### Opción 2: Extender `backend-agent` con responsabilidades de schema y migraciones

**Descripción**: Sin agente nuevo; el `backend-agent` escribe migraciones y valida convenciones como parte de cada HU backend.

**Pros:**
- Menos agentes; flujo más corto.
- Un solo implementador por HU backend (código + DDL).
- Sin cambios en orquestador ni workflows.

**Cons:**
- Mezcla diseño de schema (RLS, triggers, PII comments) con lógica de negocio.
- Code review backend no garantiza cumplimiento de 20+ reglas de schema.
- El `architecture-agent` seguiría produciendo DDL sin quien lo materialice sistemáticamente.
- Mayor riesgo de anti-patterns (`float` para montos, tablas en `public`, FK sin índice).

**Esfuerzo estimado**: S
**Riesgos principales**: Deuda técnica de schema acumulada; incumplimiento multi-tenant no detectado a tiempo.

---

### Opción 3: Convenciones documentadas sin agente — solo skill `db-schema-validator` en CI

**Descripción**: Publicar `database-conventions.md` y ejecutar la validadora en pipeline CI/CD, sin agente IA dedicado.

**Pros:**
- Enforcement automático en CI; no depende de invocación manual del agente.
- Documentación normativa disponible para humanos y PRs.
- Menor complejidad en el grafo de agentes.

**Cons:**
- Nadie **produce** el DDL conforme — solo se **rechaza** lo incorrecto post-facto.
- CI no diseña schemas ni escribe migraciones reversibles con RLS/triggers.
- El `architecture-agent` no tiene contraparte operativa para materializar diseños.
- Iteraciones costosas: dev escribe mal → CI falla → corrige → repite.

**Esfuerzo estimado**: M
**Riesgos principales**: Fricción en PRs; schema diseñado ad-hoc por cada desarrollador/agente implementador.

---

## Tradeoff aceptado

Se elige la **Opción 1** porque el dominio FLIT combina requisitos de schema complejos (multi-tenant, RLS, PII, catálogos regulatorios colombianos) con un pipeline ya basado en agentes especializados. Un agente dedicado cierra la brecha entre el modelo conceptual del `architecture-agent` y la implementación del `backend-agent`, con validación machine-checkable antes del merge.

El costo de un agente adicional y fases 2b/3c en los workflows se compensa con: (a) menos bloqueantes de schema en code review, (b) convenciones como fuente única de verdad, y (c) trazabilidad clara de quién produce vs quién consume el DDL. La Opción 2 concentra demasiado riesgo en el implementador; la Opción 3 valida pero no produce.

---

## Consecuencias

### Lo que se gana

- Fuente única de verdad para schema (`database-conventions.md`) y acceso a datos (`data-access-conventions.md`).
- Validación pre-merge sistemática vía `db-schema-validator`.
- Separación clara: diseño conceptual → materialización DDL → implementación repositorios.
- Mejor cumplimiento Habeas Data (PII etiquetada en schema, auditable).
- Workflows documentados con criterios explícitos de cuándo ejecutar Fase 2b vs omitir.

### Lo que se pierde / costo aceptado

- Pipeline más largo para Features con entidades nuevas (Fase 2b obligatoria).
- Mantenimiento de 3 artefactos sincronizados cuando cambien las reglas.
- Curva de aprendizaje: el equipo debe distinguir invocaciones a `database-agent` vs `backend-agent`.

### Lo que cambia operacionalmente

- `requirement-to-delivery.md`: Fase 2b entre diseño y descomposición cuando hay schema nuevo.
- `implement-story.md`: Fase 3c (validación datos) y routing HU schema → `database-agent`.
- `orchestrator-agent.md`: tabla de routing y manejo de `BLOCKED` del validador.
- `architecture-agent`: produce modelo conceptual + ADR; ya no es dueño del DDL detallado.
- `tech-lead-agent`: incluye HU de schema/migración antes de HUs dependientes.
- `code-review-agent`: dimensión 7 (datos) invoca `db-schema-validator` si hay persistencia.
- Nuevo agente: `.cursor/agents/database-agent.md`.
- Nueva skill: `.cursor/skills/db-schema-validator/`.

---

## ADRs relacionados

- [ADR-0001](ADR-0001-clean-architecture-backend.md) — Capas domain/application/infrastructure; repositorios en domain, EF Core en infrastructure.
- [ADR-0004](ADR-0004-consolidacion-stack-dotnet-python.md) — Stack .NET 10 + EF Core 10 + PostgreSQL.
- [ADR-0006](ADR-0006-gestion-archivos-vps-minio.md) — Metadatos de archivos en PostgreSQL; convenciones de schema aplican.

---

## Notas operativas para otros agentes

- **Architecture Agent**: Entrega modelo conceptual + DDL de referencia + ADR `Propuesto` para entidades nuevas. No escribe migraciones finales ni RLS/triggers. Referencia `docs/database-conventions.md` en diseños.
- **Database Agent**: Dueño de convenciones de datos. Modo A/B en Fase 2b; Modo C en validación pre-merge. No implementa use cases ni controllers.
- **Tech Lead Agent**: En descomposición (Modo B), HU `[BACKEND]` de schema/migración **antes** de HUs que consumen tablas nuevas. Modo D: incluir deuda de schema (sin RLS, FK sin índice).
- **Backend Agent**: Implementa repositorios y EF Core configs siguiendo `docs/data-access-conventions.md`. Redirige schema/migraciones a `database-agent`. No escribe DDL con RLS/triggers.
- **Frontend Agent**: Sin cambios directos; consumir APIs que respetan aislamiento de tenant.
- **Code Review Agent**: Invocar `db-schema-validator` (dimensión 7) cuando el PR toque `Migrations/`, `Persistence/` o repositorios. `BLOCKED` = bloqueante formal.
- **Security Agent**: Coordinar con `database-agent` cuando migraciones introducen PII nueva (`@pii:*` en COMMENT).
- **QA Agent**: TCs de regresión en módulos con cambios de schema; validar soft delete y filtros de tenant en E2E cuando aplique.
- **Infra Agent**: Aplica migraciones en DEV/QA/PDN; no las escribe. Verificar extensión `pg_uuidv7` en PostgreSQL 17.
- **Integration Agent**: PRs con migraciones deben referenciar veredicto `OK_TO_MERGE_DB` en descripción o comentario.
- **Orchestrator Agent**: No omitir Fase 2b con entidades nuevas; pausar merge si validador devuelve `BLOCKED`.

---

## Artefactos creados / modificados

| Artefacto | Path |
|-----------|------|
| Convenciones de schema | `docs/database-conventions.md` |
| Convenciones de acceso a datos | `docs/data-access-conventions.md` |
| Agente de BD | `.cursor/agents/database-agent.md` |
| Skill validadora | `.cursor/skills/db-schema-validator/` |
| Workflow E2E | `.cursor/workflows/requirement-to-delivery.md` |
| Workflow HU | `.cursor/workflows/implement-story.md` |
| Orquestador | `.cursor/agents/orchestrator-agent.md` |
| Índice agentes | `AGENTS.md` |

---

## Referencias externas

- [PostgreSQL Row Level Security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html)
- [EF Core 10 — Npgsql provider](https://www.npgsql.org/efcore/)
- Ley 1581 de 2012 (Habeas Data Colombia)

---

*Creado por: Architecture Agent / Fecha: 2026-06-02 / Estado: Propuesto*
*Para promover a Aceptado: PR separada con aprobación del Líder Técnico humano*
