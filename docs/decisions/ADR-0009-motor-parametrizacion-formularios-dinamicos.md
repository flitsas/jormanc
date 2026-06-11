# ADR-0009: Motor de Parametrización de Formularios Dinámicos — JSONB vs Tablas Relacionales vs Híbrido

**Fecha:** 2026-06-10
**Status:** Propuesto
**Deciders:** Líder Técnico FLIT
**Autor:** Architecture Agent (architecture-agent v2.0)
**Tags:** arquitectura, backend, postgresql, parametrizacion, jsonb, formularios-dinamicos
**Relaciona:** ADR-0008 (convenciones persistencia), ADR-0010 (multi-tenant RLS)
**Feature ADO:** #9568 PARAMETRIZADOR-TRÁMITES

---

## Contexto

El Feature #9568 exige un **motor low-code de parametrización** donde SuperAdmin puede definir:
- Tipos de trámite agrupados en familias (Matrícula Inicial, Traspasos, Otros).
- Un pipeline visual de pasos con mínimo 4 pasos.
- Secciones de formulario dinámicas con múltiples tipos de campo (Texto, Dropdown, Checkbox, Numérico, Adjuntos, Listas) ordenables.
- Consumo declarativo de APIs REST externas (endpoint, verbo, parámetros enlazados a variables de pasos previos).
- Diseñador de reglas de negocio (AND/OR, operadores, acciones UI).
- Simulador de coherencia que bloquea guardado ante conflictos.

El principio rector del sistema es la **parametrización total**: ningún trámite ni regla puede estar hardcodeado. Adicionalmente, la configuración debe versionarse (snapshot al radicar, ADR-0010). El stack usa PostgreSQL 17+ con soporte nativo a JSONB y operadores GIN.

### Restricciones

| # | Restricción |
|---|---|
| C1 | Queries sobre estructura de configuración (¿qué trámites tienen campo "placa"?) deben ser eficientes |
| C2 | Hot-reload de configuración sin despliegue |
| C3 | Snapshot inmutable al momento de radicar un trámite |
| C4 | Versionamiento de tipos de trámite (v1, v2, v3...) |
| C5 | El simulador de coherencia necesita recorrer reglas para detectar conflictos |
| C6 | RLS por tenant (ADR-0010) |

---

## Decisión

Adoptar el **modelo híbrido (Opción C)**: entidades estructurales primarias como tablas relacionales normalizadas (ProcedureType, ProcedureStep, FormSection, FormField) con metadata extendida en columnas JSONB solo para datos verdaderamente variables (config de campo, condiciones/acciones de reglas, bindings de API).

---

## Alternativas consideradas

### Opción 1: JSONB puro — toda la configuración en una columna

**Descripción:** Una tabla `procedure_types` con una columna `config jsonb` que almacena toda la estructura (pasos, secciones, campos, reglas, conectores) como documento JSON anidado.

**Pros:**
- Máxima flexibilidad: cualquier cambio de estructura no requiere migraciones.
- Un solo `SELECT` carga la configuración completa del trámite.
- Fácil versionamiento: almacenar el documento completo como snapshot.
- Menor cantidad de tablas y JOINs en el DER.

**Cons:**
- Queries sobre estructura son costosas incluso con índices GIN (¿qué trámites tienen campo tipo "adjunto"?).
- Sin validación de integridad referencial entre entidades de la config (un field puede referenciar un step inexistente dentro del JSON).
- Difícil modelar relaciones FK entre conceptos (ej. actor_definition → query_rule).
- El simulador de coherencia debe parsear JSON en memoria para detectar conflictos — complejo en .NET.
- No alineado con `docs/database-conventions.md` §6 (catálogos como tablas, no ENUM ni JSONB puro).

**Esfuerzo estimado:** S (inicialmente simple, M-L en mantenimiento)
**Riesgos principales:** Deuda técnica acumulada; queries de reporting ineficientes; difícil hacer audit log a nivel de campo.

---

### Opción 2: Tablas relacionales puras — sin JSONB

**Descripción:** Tablas completamente normalizadas para cada concepto: `procedure_types`, `procedure_steps`, `form_sections`, `form_fields`, `field_options`, `api_connectors`, `connector_params`, `rule_sets`, `rule_conditions`, `rule_actions`, `condition_nodes`.

**Pros:**
- Máxima integridad referencial con FK entre conceptos.
- Queries eficientes sobre cualquier dimensión de la estructura.
- Audit log a nivel de campo granular.
- Compatible con `docs/database-conventions.md` al 100%.
- Simulador de coherencia puede hacer JOINs eficientes.

**Cons:**
- Gran cantidad de tablas (~15-20 solo para la config) y JOINs costosos para cargar la configuración completa de un trámite.
- El árbol de condiciones de reglas (AND/OR anidado) es recursivo → tablas autoreferenciales complicadas.
- Versionamiento de configuración compleja requiere copiar múltiples tablas en el snapshot.
- Esquema rígido: agregar un nuevo tipo de validación de campo requiere migración.
- Carga de la config completa del trámite (para el stepper) requiere N+1 queries o JOINs complejos.

**Esfuerzo estimado:** L
**Riesgos principales:** Rigidez ante cambios de diseño de formularios; JOIN storms para cargar config; snapshot caro de implementar.

---

### Opción 3 (recomendada): Híbrido — tablas para estructura + JSONB para metadata extendida

**Descripción:** Tablas relacionales para las entidades estructurales primarias con identidad propia (ProcedureType, Step, FormSection, FormField, ActorDefinition, QueryRule, ApiConnector, RuleSet). Columnas JSONB solo para datos verdaderamente variables o no predecibles en su estructura (config de campo: opciones de dropdown, validaciones regex, rangos numéricos; condiciones/acciones de reglas: árbol AND/OR; param_bindings de API connector).

**Pros:**
- Tablas relacionales garantizan FK e integridad entre entidades principales.
- JSONB absorbe la variabilidad sin requerir migraciones para nuevos tipos de campo.
- Queries sobre entidades primarias (¿qué trámites del tenant activo tienen paso de tipo "firma"?) son eficientes con índices regulares.
- Árbol de condiciones AND/OR en JSONB es natural y evita tablas autoreferenciales complejas.
- Snapshot al radicar: clonar entidades relacionales + JSONB en una tabla `procedure_config_snapshots`.
- Alineado con el espíritu de `docs/database-conventions.md`: estructura como tablas, configuración variable como JSONB.
- Audit log granular para cambios en entidades relacionales; JSONB completo en un registro.

**Cons:**
- Queries sobre contenido de JSONB (ej. "todos los campos con validación regex alfanumérica") requieren operadores `@>` y GIN — menos ergonómicos que SQL regular.
- Dos paradigmas a mantener (ORM para tablas relacionales + manejo manual de JSONB).
- La carga completa de config para el stepper sigue requiriendo JOINs (mitigado con EF Core Include o projection).

**Esfuerzo estimado:** M
**Riesgos principales:** Que el equipo abuse de JSONB para cosas que deberían ser columnas (mitigado con la guía explícita en este ADR).

---

## Tradeoff aceptado

Se elige la **Opción 3 (Híbrido)** porque:

1. La estructura primaria (tipo→paso→sección→campo→actor→regla) tiene identidad, FK y ciclo de vida propio → tablas relacionales.
2. Los datos de configuración interna de un campo (opciones, validaciones, rangos) son altamente variables y difíciles de normalizar sin rigidez → JSONB.
3. El árbol AND/OR de condiciones de reglas es naturalmente un documento → JSONB.
4. El snapshot al radicar clona un subconjunto manejable de registros, no un documento monolítico.
5. El compromiso de rendimiento es aceptable: el simulador de coherencia opera sobre JSON en memoria después de la carga inicial.

La Opción 1 sacrifica integridad y queryability. La Opción 2 introduce rigidez y un esquema de 15-20 tablas difícil de evolucionar.

---

## Modelo de datos de referencia (parcial)

```sql
-- Entidades relacionales primarias
CREATE TABLE procedures_config.procedure_types (
  id           uuid DEFAULT gen_ulid() PRIMARY KEY,
  tenant_id    uuid NOT NULL REFERENCES identity.tenants(id),
  slug         text NOT NULL,
  name         text NOT NULL,
  family       text NOT NULL CHECK (family IN ('matricula_inicial', 'traspasos', 'otros')),
  scope        text NOT NULL CHECK (scope IN ('global', 'company', 'ot', 'company_ot')),
  version      int NOT NULL DEFAULT 1,
  is_active    bool NOT NULL DEFAULT true,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now(),
  deleted_at   timestamptz
);

CREATE TABLE procedures_config.procedure_steps (
  id                  uuid DEFAULT gen_ulid() PRIMARY KEY,
  procedure_type_id   uuid NOT NULL REFERENCES procedures_config.procedure_types(id),
  tenant_id           uuid NOT NULL,
  order_index         int NOT NULL,
  name                text NOT NULL,
  step_type           text NOT NULL CHECK (step_type IN ('form', 'api_call', 'signature', 'review')),
  created_at          timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE procedures_config.form_fields (
  id          uuid DEFAULT gen_ulid() PRIMARY KEY,
  section_id  uuid NOT NULL REFERENCES procedures_config.form_sections(id),
  tenant_id   uuid NOT NULL,
  order_index int NOT NULL,
  name        text NOT NULL,
  field_type  text NOT NULL CHECK (field_type IN ('text', 'dropdown', 'checkbox', 'numeric', 'attachment', 'list')),
  is_required bool NOT NULL DEFAULT false,
  config      jsonb NOT NULL DEFAULT '{}',  -- ← JSONB: opciones dropdown, regex, rangos, etc.
  created_at  timestamptz NOT NULL DEFAULT now()
);

-- JSONB: árbol de condiciones de regla
CREATE TABLE procedures_config.rule_sets (
  id                  uuid DEFAULT gen_ulid() PRIMARY KEY,
  procedure_type_id   uuid NOT NULL REFERENCES procedures_config.procedure_types(id),
  tenant_id           uuid NOT NULL,
  name                text NOT NULL,
  conditions          jsonb NOT NULL,  -- ← árbol AND/OR
  actions             jsonb NOT NULL,  -- ← lista de acciones UI
  created_at          timestamptz NOT NULL DEFAULT now()
);

-- Índice GIN para queries sobre config de campos
CREATE INDEX ix_form_fields_config_gin ON procedures_config.form_fields USING GIN (config);
-- Índice GIN para queries sobre condiciones
CREATE INDEX ix_rule_sets_conditions_gin ON procedures_config.rule_sets USING GIN (conditions);
```

---

## Consecuencias

### Lo que se gana
- Integridad referencial entre ProcedureType, Step, Section, Field y Actor.
- Flexibilidad para nuevos tipos de campo sin migraciones (JSONB absorbe la variabilidad).
- Snapshot manejable para versionamiento al radicar.
- Queries eficientes sobre estructura primaria.

### Lo que se pierde / costo aceptado
- Necesidad de manejar JSONB explícitamente en EF Core (`JsonDocument` o `JsonSerializer` en mappings).
- Dos patrones en el mismo schema — requiere disciplina del equipo.

### Regla para el equipo: cuándo usar JSONB vs columna

| Dato | Columna relacional | JSONB |
|---|---|---|
| Id, nombre, orden, tipo, flags | ✅ siempre | ❌ nunca |
| Configuración interna del campo (regex, opciones, min/max) | ❌ demasiado variable | ✅ |
| Árbol AND/OR de condiciones | ❌ recursivo complejo | ✅ |
| Acciones UI (show/hide/toast/modal) | ❌ lista variable | ✅ |
| Param bindings de API connector | ❌ lista variable | ✅ |
| Resultados de consultas externas (payload RUNT) | ❌ estructura externa | ✅ |

---

## ADRs relacionados

- ADR-0008 — Convenciones normativas de persistencia (dueño: database-agent)
- ADR-0010 — Estrategia multi-tenant con RLS (tenant_id aplica a todas las tablas de este módulo)

---

## Notas operativas

- **database-agent:** Materializar DDL completo del schema `procedures_config`. Aplicar RLS. Crear índices GIN en columnas JSONB. Agregar snapshot table `procedure_config_snapshots`.
- **backend-agent:** EF Core mapping de `config jsonb` usando `JsonDocument` o `System.Text.Json`. Implementar `ProcedureConfigSnapshotService` que clona la config al radicar.
- **frontend-agent:** El simulador de coherencia puede recibir la config completa del trámite como JSON desde el endpoint y operar en memoria (no requiere múltiples roundtrips).

---

*Creado por: Architecture Agent — 2026-06-10 | Estado: Propuesto*
*Para promover a Aceptado: PR separada con aprobación del Líder Técnico humano*
