# ADR-0009: Modelo de parametrización de trámites (familias/tipos/aristas/matriz + capas global/tenant/OT)

**Fecha**: 2026-06-02
**Status**: Propuesto
**Deciders**: Líder Técnico FLIT
**Tags**: arquitectura, datos, parametrizacion, multi-tenant, tramites, postgresql

---

## Contexto

Los Features #9408 (PRD-024), #9409 (DTR-FLIT2) y #9410 (PRD-025) exigen un **motor de trámites 100%
parametrizable**: Producto/Negocio debe poder encender/apagar trámites y ajustar su conformación
**sin recompilar ni desplegar**. El modelo gira en torno a una **tipología jerárquica** (familias →
tipos) cruzada con **cuatro aristas** (Vehículo, Propietario/Vendedor, Comprador, Locatario) mediante
una **matriz de conformación**, y formularios/documentos/consultas/reglas derivados de esa matriz.

Adicionalmente, la config tiene **tres dueños**: FLIT SuperAdmin (maestros globales), Tenant Admin
(ajustes por compañía) y Admin OT (ajustes por Organismo de Tránsito, p. ej. orden del consolidado
#9379). El tenant es la **Compañía B2B** (decisión de producto), y el OT es referencia cross-tenant.

El reto de datos: representar la matriz y las capas global/tenant/OT **sin duplicar el catálogo
completo por tenant** y sin romper las convenciones de `docs/database-conventions.md` (que exigen
`tenant_id` + RLS en tablas de negocio).

---

## Decisión

1. **Tipología y matriz como tablas normalizadas** en el schema `procedures_config`:
   `procedure_families (1:N) procedure_types`, `edges`, y la junction
   **`procedure_type_edges`** como **matriz de conformación** (PK natural `(procedure_type_id,
   edge_id)` con `is_active`, `is_required`, `display_order`, `role_label`, `config`). Una fila
   ausente = la arista no aplica; `is_active=false` = no se renderiza ni valida.

2. **Overrides por arista** vía FK compuesta nullable: `form_sections`, `required_documents` y
   `procedure_type_query_configs` llevan `(procedure_type_id, edge_id)` que referencia la matriz;
   con `edge_id` NULL la config es a nivel tipo. Sin explosión de tablas por concern.

3. **Capas global/tenant/OT por overrides delgados, no por copia**: los **maestros son globales**
   (definidos por SuperAdmin); las personalizaciones viven en tablas delgadas tenant-scoped
   (`procedure_type_activations` con `overrides jsonb` y `traffic_agency_id` opcional, `rules`,
   `endpoint_catalog`, variantes de plantilla por tenant) y OT-scoped (`ot_consolidated_doc_orders`,
   `ot_rules`). Un **resolver** computa la config efectiva con **precedencia OT > tenant > global**
   (merge por atributo; arrays se reemplazan, no se fusionan).

4. **Maestros globales tenant-exentos**: las tablas maestras de `procedures_config` (familias, tipos,
   aristas, matriz, forms, conectores, documentos requeridos, plantillas) **no llevan `tenant_id`**
   (son config de plataforma, análogas a `catalogs`), con **RLS de lectura abierta y escritura solo
   SuperAdmin** (`config_read`/`config_modify`), `is_active` como ciclo de vida y auditoría por
   trigger. Es una **excepción explícita** a la regla "toda tabla de negocio lleva `tenant_id`",
   justificada aquí.

---

## Alternativas consideradas

### Opción 1 — Matriz como junction única + overrides por FK compuesta + capas por override (elegida)
**Pros:** una sola tabla "matriz"; per-tipo y per-arista con orden/activación; sin duplicar catálogo;
RLS limpio (maestros globales, overrides tenant). **Contras:** el resolver debe materializar la
precedencia; FK compuesta con columna nullable requiere entender semántica MATCH SIMPLE.
**Esfuerzo:** M. **Riesgo:** resolver mal especificado → snapshots incorrectos (mitigado con
ADR-0010 + pruebas).

### Opción 2 — Tablas de override dedicadas por concern (matriz + forms_override + docs_override + queries_override)
**Pros:** cada concern explícito. **Contras:** multiplica 3-4 junctions y duplica variantes tipo/
arista; más superficie de bugs y de migraciones. **Esfuerzo:** L. **Riesgo:** drift entre tablas.

### Opción 3 — Config como documentos JSONB por tipo (matriz/forms/reglas embebidos)
**Pros:** máxima flexibilidad, menos tablas. **Contras:** mata queryabilidad/orden/integridad
referencial; viola la decisión "Híbrido" (forms normalizados); dificulta reporting y validación por
BD. **Esfuerzo:** S. **Riesgo:** alto a mediano plazo (config no auditable ni consultable).

---

## Consecuencias

**Positivas:** alta extensibilidad (un tipo nuevo = filas en BD, radicable sin deploy, #9409
CF-H1..H3); desacople real (apagar arista ⇒ no render/valida, CF-J1); auditoría de matriz por trigger
(CF-A9); RLS coherente con tenancy = Compañía.

**Negativas / mitigaciones:** la excepción tenant-exenta para maestros globales debe documentarse y
validarse (este ADR la cubre; `db-schema-validator` debe contemplarla). El resolver añade lógica de
aplicación; se acota con un contrato de snapshot ([ADR-0010](ADR-0010-snapshot-config-al-radicar.md)).

**Relacionados:** [ADR-0010](ADR-0010-snapshot-config-al-radicar.md) (versionado),
[ADR-0011](ADR-0011-motor-reglas-jsonb.md) (reglas), [ADR-0012](ADR-0012-ot-sin-tenant-id.md) (OT),
[ADR-0008](ADR-0008-database-agent-convenciones-persistencia.md) (convenciones).
DDL de referencia: `docs/designs/tramites-2.0/ddl/50-procedures_config.sql`.
