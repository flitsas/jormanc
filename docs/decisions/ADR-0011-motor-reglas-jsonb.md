# ADR-0011: Motor de reglas no-code con árbol JSONB validado por la base de datos

**Fecha**: 2026-06-02
**Status**: Propuesto
**Deciders**: Líder Técnico FLIT
**Tags**: arquitectura, datos, reglas, jsonb, seguridad, tramites

---

## Contexto

El Feature #9410 (PRD-025) pide un **motor de reglas de negocio no-code**: un constructor visual
If/Else que evalúa datos del trámite en tiempo real y dispara acciones (popup, inyección de sección,
consumo de endpoint). Operadores: `equal, notEqual, greater, less, contains, isEmpty, isNotEmpty`;
valores estáticos o dinámicos (otro campo); agrupadores AND/OR anidados. Las reglas se activan/
desactivan en caliente (toggle), no deben romper instancias radicadas al eliminarse, y conviven con
las reglas a nivel OT (#9378). La decisión de producto fue **representación "Híbrida"**: forms
normalizados, reglas como árbol JSONB.

Riesgos: inyección (si se almacenara/evaluara SQL o expresiones arbitrarias), árboles malformados,
fuga de credenciales de endpoints.

---

## Decisión

Persistir condiciones y acciones como **árbol JSONB con gramática cerrada**, validado en **dos
capas**:

- **Forma del árbol:** grupos `{op: AND|OR, children:[...]}` y hojas
  `{field, operator∈conjunto-cerrado, value:{kind: static|field, ...}}`; acciones como arreglo tipado
  `{type∈(popup_modal|inject_section|call_endpoint|block), params}`. `call_endpoint` referencia
  `procedures_config.endpoint_catalog` **por código** (los secretos viven por referencia, nunca
  inline).
- **Validación en BD:** funciones `public.is_valid_rule_condition(jsonb)` y
  `public.is_valid_rule_actions(jsonb)` (IMMUTABLE) usadas en **CHECK constraints** de
  `procedures_config.rules` y `ot.ot_rules`. La BD rechaza árboles malformados. Columna
  `schema_version` para evolución.
- **Validación en app:** FluentValidation/JSON-Schema en escritura (campos válidos para el tipo,
  tipos compatibles, endpoint existente y autorizado).
- **Seguridad:** nunca se almacena ni evalúa SQL (solo claves de campo); reglas `is_active=false` no
  evalúan; prioridad por `priority`; eliminar una regla es borrado lógico y el set aplicado vive en
  el snapshot ([ADR-0010](ADR-0010-snapshot-config-al-radicar.md)), de modo que no rompe radicados.

---

## Alternativas consideradas

### Opción 1 — Árbol JSONB con gramática cerrada + CHECK en BD (elegida)
**Pros:** flexibilidad no-code; validación verificable por máquina en la BD (no solo en app); sin
inyección (conjuntos cerrados, solo claves); reutilizable en OT. **Contras:** validadores plpgsql a
mantener; evaluación recursiva en app. **Esfuerzo:** M.

### Opción 2 — Reglas totalmente normalizadas (tablas de condiciones/operadores/valores/acciones)
**Pros:** máxima integridad referencial y queryabilidad. **Contras:** muchas tablas/joins; rígido
ante operadores/acciones nuevos (requiere migración); UX del constructor más difícil de mapear.
**Esfuerzo:** L.

### Opción 3 — Expresión embebida (mini-DSL/script) evaluada en runtime
**Pros:** muy expresivo. **Contras:** superficie de inyección/seguridad; difícil de validar y de
construir visualmente; opaco para auditoría. **Esfuerzo:** M-L. **Riesgo:** alto (seguridad).

### Sobre `pg_jsonschema`
Si la extensión `pg_jsonschema` está disponible en el motor, los CHECK pueden migrarse a
`jsonb_matches_schema(...)` con un JSON Schema versionado. Mientras tanto, las funciones plpgsql son
el camino portable (no requiere extensión externa).

---

## Consecuencias

**Positivas:** cumple #9410 (constructor, toggle hot-swap, no rompe radicados, badges/acciones);
mismo motor para `rules` (tenant) y `ot_rules` (OT); seguridad por diseño.

**Negativas / mitigaciones:** mantener sincronizados los conjuntos cerrados entre validador plpgsql,
validación app y UI; cubrir con pruebas los escenarios QA del PRD (Tesla OR Eléctrico, Placa→SIMIT
onBlur, RTM vencida → popup).

**Relacionados:** [ADR-0009](ADR-0009-modelo-parametrizacion-tramites.md),
[ADR-0010](ADR-0010-snapshot-config-al-radicar.md). DDL: validadores en
`docs/designs/tramites-2.0/ddl/00-extensions-and-schemas.sql`; reglas en `50-procedures_config.sql`.
