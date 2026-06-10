# Diseño arquitectónico — Trámites de Tránsito FLIT 2.0

**Fecha**: 2026-06-02 · **Autor**: architecture-agent + database-agent (asistidos) · **Estado**: Propuesto
**Features**: #9369 #9370 #9378 #9379 #9381 #9383 #9408 #9409 #9410 (FLIT - EVOLUTION, Sprint 1)

---

## Contexto

Se reconstruye desde cero el dominio de **trámites de tránsito** de FLIT, conservando arquitectura,
framework y lenguajes actuales. El principio rector (reglas estándar + #9408/#9409/#9410) es la
**parametrización total**: ningún trámite, documento, actor, consulta o regla "quemado" en código.
Todo se crea/modifica/desactiva desde administración **sin desplegar**. Este documento define el
diseño arquitectónico y el modelo de datos; el DDL completo vive en `./ddl/` y el ER en
`./01-er-diagram.md`. Las decisiones que sientan precedente están en ADR-0009..0012.

---

## Decisión arquitectónica

**Stack (respetado):** .NET 10 / C# 14, modular monolith, Clean Architecture (Domain/Application/
Adapters/Ports) por módulo `Flit.Modules.*`; EF Core 10 + Npgsql; **PostgreSQL 17+**; Gateway YARP;
MinIO (archivos, ADR-0006); RabbitMQ + outbox; QuestPDF (PDF, ADR-0005) + ClosedXML (Excel, ADR-0002);
frontend React 19 + Vite + PrimeReact (Feature-Sliced).

**Módulos backend nuevos:** `Flit.Modules.Identity` (rebuild), `Flit.Modules.Companies`,
`Flit.Modules.TrafficAgencies`, `Flit.Modules.ProceduresConfig`, `Flit.Modules.Procedures` (rebuild),
`Flit.Modules.Integrations`, `Flit.Modules.IdentityVerification`, `Flit.Modules.Dashboard`.

**11 schemas (bounded contexts):** `catalogs` < `identity` < `files` < `companies` < `ot` <
`procedures_config` < `integrations` < `identity_verification` < `procedures` < `dashboard`; `audit`
transversal. La jerarquía evita ciclos de FK (referencias alto→bajo); el runtime **snapshotea** en
vez de FK "hacia arriba".

**Decisiones confirmadas (input usuario):** tenant = Compañía B2B; identidad reconstruida; versionado
por snapshot al radicar; reglas/forms híbrido.

**Las 3 decisiones difíciles (ver ADRs):**
- Matriz de conformación como junction única + overrides por arista (ADR-0009).
- Capas global/tenant/OT por overrides delgados + maestros globales tenant-exentos (ADR-0009).
- Reglas como árbol JSONB con gramática cerrada validada por BD (ADR-0011).
- (+) Snapshot inmutable al radicar (ADR-0010) y OT cross-tenant sin `tenant_id` (ADR-0012).

---

## Sequence diagram — radicación de trámite dinámico (#9408/#9409)

```mermaid
sequenceDiagram
  actor Op as Operador FLIT
  participant FE as Frontend (React)
  participant GW as Gateway (YARP)
  participant API as ProceduresConfig/Procedures
  participant RES as Resolver de config
  participant DB as PostgreSQL
  participant EXT as Conectores (RUNT/SIMIT/RUES/FASECOLDA)

  Op->>FE: Selecciona tipo de trámite (solo activos)
  FE->>GW: GET /procedures/types
  GW->>API: tipos activos (is_active=true)
  API->>DB: procedure_types ⋈ activations (tenant/OT)
  DB-->>FE: lista de tipos
  Op->>FE: Abre tipo
  FE->>API: GET /procedures/types/{code}/configuration
  API->>RES: resolver (global → tenant → OT)
  RES->>DB: matriz + forms + docs + queries + rules
  DB-->>FE: config efectiva (aristas/campos/documentos)
  Op->>FE: Diligencia; campo desencadenante
  FE->>API: ejecuta consultas por arista activa
  API->>EXT: RUNT+SIMIT / RUES / FASECOLDA (concurrente, failover)
  EXT-->>API: resultados (timeout/circuit breaker)
  API->>DB: procedure_query_results (snapshot por fuente)
  Op->>FE: Finaliza wizard
  FE->>API: POST /procedures/instances
  API->>RES: congela config_snapshot (ADR-0010)
  API->>DB: procedure_instances (state=borrador) + actores + vehículo
  API-->>FE: Borrador (editable) + dispara validación de identidad
```

## Contrato API (mínimo, #9409)

| Método | Ruta | Propósito |
|---|---|---|
| GET | `/api/v1/procedures/types` | Tipos activos para el tenant/OT (dropdown). |
| GET | `/api/v1/procedures/types/{code}/configuration` | Config resuelta: aristas, secciones, campos, documentos, consultas, reglas. |
| POST | `/api/v1/procedures/instances` | Crea instancia validada vs config vigente + `config_snapshot`. |
| PATCH | `/api/v1/procedures/instances/{id}/state` | Transición de estado (guard de transiciones). |
| POST | `/api/v1/procedures/instances/{id}/queries/run` | Re-ejecuta consultas (solo `borrador`). |
| GET | `/api/v1/dashboard/kpis` | KPIs por estado/OT/fecha (filtros #9369). |

El detalle OpenAPI lo materializa el backend-agent.

---

## Modelo de datos

Inventario completo y DDL en `./ddl/` (orden 00→90); relaciones en `./01-er-diagram.md`. Resumen por
schema en el plan (`vamos-a-reconstruir-...md`). El **núcleo de parametrización** (`procedures_config`)
es el entregable destacado: `procedure_families → procedure_types → edges → procedure_type_edges
(matriz)`, `form_sections/form_fields`, `required_documents`, `document_templates(+versions)`,
`query_connectors + procedure_type_query_configs`, `rules + endpoint_catalog`, con capas
`procedure_type_activations`.

---

## Cobertura por Feature (criterios funcionales → tablas/mecanismos)

| Feature | Cómo lo cumple el modelo |
|---|---|
| **#9370 Identidad** | `identity.*`: tenants/users/roles/permissions(slug)/user_roles; estados de cuenta + `blocked_until` (bloqueos temporal/permanente); `onboarding_invitations` (enlace 24h, un uso); `password_policies`; `login_attempts`; `refresh_tokens`; RLS estricta + `find_user_for_auth` (login). 3 perfiles via roles globales. |
| **#9381 Compañías B2B** | `companies` + `company_module_configs` (hot-reload por `module_key`); `signature_wallets(+movements)`; contingencia RUNT en `company_module_configs(runt_contingency)` + `integrations.runt_sync_log` (failover Verifik/Intempo); `vehicle_ownership_rules` (interceptor). Filtros NIT/Nombre con índices `gin_trgm`. |
| **#9383 Escrituras** | `companies.escrituras` (visible si `modules_enabled.escrituras`) + `escritura_attachments` (PDF, `position 1..5`, reemplazo total al actualizar). |
| **#9378 OT** | `ot.traffic_agencies` (registro), `ot_users(+permissions)`, `ot_rules` (constructor: trigger/condición/acciones bloqueo-validación-QX), `ot_qx_integrations` (modo dashboard/qx). |
| **#9379 Orden consolidado** | `ot_consolidated_doc_orders(+items)`: `position`, `source global/custom`, quitar = borrar fila (no archivos); guardado <500 ms (transacción corta + `uq(order_id,position)`). |
| **#9408 Motor dinámico** | `procedure_types.is_active` (selector/URL), `form_sections/fields` (render dinámico, `is_trigger`, 4 `ui_state`), `max_steps≤4`, `procedure_instances.state` (borrador editable / resto read-only), `procedure_identity_validations`. |
| **#9409 Matriz + consultas** | `procedure_families/types` (3 familias, 13 tipos), `edges` (4), `procedure_type_edges` (matriz 2.3, auditable), `query_connectors` (6) + `procedure_type_query_configs` (enrutamiento CC/NIT, Locatario SIMIT), `procedure_query_results` (snapshot, fallo aislado), alta de tipo solo en BD (CF-H). |
| **#9410 Reglas no-code** | `rules` (condition_tree/actions JSONB validado por BD), `endpoint_catalog`, toggle `is_active`, prioridad, no rompe radicados (snapshot). |
| **#9369 Dashboard** | `dashboard.v_procedure_kpis / v_user_productivity / v_ot_distribution` (RLS via security_invoker; SuperAdmin cross-tenant); export ClosedXML/QuestPDF; MV opcional. |

---

## Archivos creados (este entregable)

```
docs/designs/tramites-2.0/
  00-diseno-arquitectonico.md   (este documento)
  01-er-diagram.md              (ER Mermaid de los 11 schemas)
  ddl/00-extensions-and-schemas.sql … 90-dashboard.sql  (11 archivos)
docs/decisions/ADR-0009 … ADR-0012  (Propuesto)
```

## Notas operativas por agente

- **database-agent:** materializar `ddl/*` como migraciones EF Core en `Flit.Infrastructure/Migrations`
  (orden 00→90), `Up`/`Down`; correr `db-schema-validator` (§16) — atender las **excepciones**
  documentadas (maestros globales de `procedures_config` y schema `ot` sin `tenant_id`).
- **backend-agent:** interceptor de conexión que setea `app.current_tenant_id`, `app.current_agency_id`,
  `app.is_super_admin`, `app.current_user_id`; repositorios por agregado (sin `IQueryable` fuera de
  infraestructura); el **resolver** de config + congelado de snapshot; conectores externos tras interfaz
  (timeouts/circuit breaker) con failover RUNT→Verifik/Intempo.
- **frontend-agent:** render dinámico desde `/configuration`; stepper ≤4; UI Kit FLIT 2.0
  (#557EFF/#FF4E00/#00DBD5/#162744, Poppins); constructor de reglas; drag&drop del consolidado.
- **qa-agent:** escenarios CF-J (#9409/#9410), aislamiento horizontal tenant y OT, transiciones de
  estado inválidas, alta de "Rematrícula" solo en BD.
- **security-agent:** revisar `@pii:*` (documento, biometría, tokens), retención/derecho al olvido vs
  `audit_log` y `config_snapshot` (PII por referencia), rate limiting login/activación.
- **infra-agent:** habilitar extensión `uuidv7`/`pg_uuidv7` (o usar el fallback de `00-*.sql`),
  aplicar migraciones por ambiente, jobs de refresh de MV si se habilita, particionado mensual de
  `login_attempts`/`external_query_calls`.

---

## Autoevaluación checklist §16 (database-conventions)

| Ítem | Estado |
|---|---|
| Tablas en schema de bounded context (no `public`) | ✅ |
| `snake_case`, plural, inglés | ✅ |
| `id uuid PRIMARY KEY DEFAULT uuidv7()` | ✅ (fallback en `00-*`) |
| Negocio: `tenant_id` + FK a `identity.tenants` | ✅ salvo excepciones con ADR (catalogs, `ot`, maestros `procedures_config`) |
| `created_at/by`, `updated_at/by` | ✅ (bootstrap identidad: `created_by` NULL documentado) |
| Soft delete (`deleted_at/by`) | ✅ negocio; catálogos/logs usan `is_active`/append-only (documentado) |
| `row_version` | ✅ negocio |
| FK patrón `fk_<t>_<ref>[_<role>]` | ✅ |
| FK con `ON DELETE`/`ON UPDATE` explícitos | ✅ |
| FK con índice cubriente | ✅ |
| RLS con política de tenant | ✅ (negocio); `ot` por `agency`; maestros config read-all/write-superadmin |
| Índices con `tenant_id` primero | ✅ |
| No reinventa tablas existentes | ✅ (rediseño autorizado; reutiliza seed OT y patrón files/MinIO) |
| Columnas PII con `@pii:*` | ✅ |
| Triggers `row_version` y `audit_log` | ✅ |
| `UP`/`DOWN` reversibles | ✅ (bloque DOWN por archivo; EF Core al materializar) |
| ADR para entidades nuevas | ✅ (ADR-0009..0012) |

## Supuestos / pendientes (con default tomado)

1. OT sin `tenant_id` → scoping por `app.current_agency_id` (ADR-0012). *Confirmar 2ª var de sesión.*
2. Máquina de estados unificada (`borrador/asignado/q_validacion/pendiente/aprobado/enviado/entregado/
   rechazado/anulado`) con guard de transiciones. *Confirmar transiciones legales finales.*
3. `reference_number`: generador por tenant. *Confirmar formato (OT+año+secuencia).*
4. Retención/PII: snapshot referencia PII por id; biometría en bucket restringido. *Derivar a security-agent.*
5. `files` reconstruido a convención conservando adapter MinIO. *Confirmar vs mantener `files` actual.*
6. `query_connectors` es la fuente única (reemplaza un catálogo `query_sources`).
