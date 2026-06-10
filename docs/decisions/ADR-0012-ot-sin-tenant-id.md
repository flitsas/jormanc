# ADR-0012: Organismos de Tránsito como referencia cross-tenant sin `tenant_id`

**Fecha**: 2026-06-02
**Status**: Propuesto
**Deciders**: Líder Técnico FLIT
**Tags**: arquitectura, datos, multi-tenant, rls, ot, tramites

---

## Contexto

La decisión de producto fija **tenant = Compañía B2B**. El **Organismo de Tránsito (OT / Secretaría)**
es la entidad estatal **donde se radica** un trámite: una misma compañía radica ante **muchos** OT, y
un OT recibe trámites de **muchas** compañías. El Dashboard #9369 filtra Compañía (tenant) y OT por
**separado**. Existe un registro real de ~370 OT (`traffic_secretaries`) con atributos e ids de
integración por familia.

Esto choca con la convención "toda tabla de negocio lleva `tenant_id` + RLS": el registro de OT y su
configuración (usuarios/permisos OT, reglas OT, orden del consolidado #9379, modo QX) **no pertenecen
a un tenant**. Forzar `tenant_id` obligaría a duplicar cada OT por compañía (~370 × N), rompiendo la
naturaleza compartida del catálogo.

---

## Decisión

Modelar el schema `ot` como **referencia cross-tenant**, con dos posturas según el tipo de tabla:

1. **`ot.traffic_agencies`** (registro de OT) es **catálogo de plataforma**: sin `tenant_id`, ciclo
   de vida por `is_active`, **sin RLS** (lectura para todos los usuarios autenticados; escritura solo
   SuperAdmin a nivel de aplicación), auditoría por trigger (tenant sentinela de plataforma). Los ids
   de integración por familia (`id_parinttrasec_*`) van en `external_refs jsonb`.

2. **Tablas de configuración OT** (`ot_users`, `ot_user_permissions`, `ot_rules`,
   `ot_consolidated_doc_orders(+items)`, `ot_qx_integrations`) **no llevan `tenant_id`** pero sí
   `traffic_agency_id`, y se aíslan con **RLS por OT** vía una **segunda variable de sesión**
   `app.current_agency_id` (política `agency_isolation`), con bypass SuperAdmin
   (`identity.is_super_admin()`). El `traffic_agency_id` se **denormaliza** en tablas hijas para RLS
   e índices uniformes.

La aplicación setea `app.current_agency_id` al entrar en contexto de un OT (igual que setea
`app.current_tenant_id` para el tenant). Es una **excepción explícita y acotada** a la regla de
`tenant_id` obligatorio, equivalente a la excepción ya prevista para `catalogs.*`.

---

## Alternativas consideradas

### Opción 1 — OT cross-tenant: registro sin RLS + config OT con RLS por `traffic_agency_id` (elegida)
**Pros:** una sola fuente de verdad del registro de OT; coincide con el Dashboard (Compañía y OT como
ejes separados); sin duplicar OT por tenant; aislamiento de config OT por agencia. **Contras:**
introduce una 2ª variable de sesión y una política RLS distinta; excepción a la convención.
**Esfuerzo:** S-M.

### Opción 2 — OT como tenant (cada Secretaría es un tenant)
**Pros:** encaja con la convención `tenant_id` sin excepción. **Contras:** contradice "tenant =
Compañía"; rompe el Dashboard (no podrías filtrar Compañía **y** OT); las compañías operan ante
muchos OT → relación N:N inviable con tenant=OT. **Esfuerzo:** L. **Riesgo:** alto (modelo
incorrecto).

### Opción 3 — Duplicar OT por tenant (cada compañía tiene su copia del registro)
**Pros:** `tenant_id` uniforme. **Contras:** ~370 × N filas duplicadas; drift de datos regulatorios;
mantenimiento inviable. **Esfuerzo:** M. **Riesgo:** alto (calidad de datos).

---

## Consecuencias

**Positivas:** modelo fiel al negocio; Dashboard #9369 con doble filtro; #9379/#9378 con aislamiento
por OT; reutiliza el seed real de ~370 OT.

**Negativas / mitigaciones:** `db-schema-validator` debe contemplar la excepción (schema `ot` exento
de `tenant_id`); documentar el contrato de `app.current_agency_id` en `data-access-conventions.md`;
pruebas de aislamiento OT-A vs OT-B. La auditoría de cambios en OT usa el tenant sentinela de
plataforma.

**Relacionados:** [ADR-0008](ADR-0008-database-agent-convenciones-persistencia.md),
[ADR-0009](ADR-0009-modelo-parametrizacion-tramites.md). DDL: `docs/designs/tramites-2.0/ddl/40-ot.sql`.
