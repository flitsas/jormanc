# Detalle de Features, Historias de Usuario y modelo de datos · Trámites de Tránsito FLIT 2.0

**Fecha**: 2026-06-03 · **Proyecto ADO**: FLIT - EVOLUTION · **Sprint destino HUs**: Sprint 1
**Total**: 12 Features (9 originales + 2 splits + **#9469 IDSecure**) · 67 Historias de Usuario · 374 Story Points
Abrir un work item: `https://dev.azure.com/FlitDevOps/FLIT%20-%20EVOLUTION/_workitems/edit/<id>`

> Documento de referencia para desarrolladores. Mapea cada Feature a sus HUs, y cada HU a los
> **archivos DDL** y **tablas** que debe tener en cuenta, con la **función** de cada tabla y los
> **servicios/módulos** del aplicativo. El DDL completo está en `./ddl/`, el modelo en
> `./01-er-diagram.md`, el diseño en `./00-diseno-arquitectonico.md` y el mapa funcional en
> `./diagrama-features.png`.

---

## 1. Resumen — HUs por Feature

| Feature | Título | #HUs | SP | IDs de las HUs |
|---|---|---|---|---|
| **#9370** | [IDENTIDAD-A] Core multi-tenant, schemas, login y bloqueos | 7 | 32 | IDN-01 #9412, F0-01 #9413, F0-03 #9414, IDN-02 #9415, IDN-03 #9416, SEC-01 #9422, INF-01 #9423 |
| **#9466** | [IDENTIDAD-B] RBAC, onboarding, consolas y perfil | 5 | 25 | IDN-04 #9417, IDN-05 #9418, IDN-06 #9419, IDN-07 #9420, IDN-08 #9421 |
| **#9409** | [TRAMITES-A] Parametrización, matriz y API de config | 12 | 71 | F0-02 #9424 … MTR-11 #9497, QA-01 #9432 |
| **#9467** | [TRAMITES-B] Consultas externas e integraciones (mock) | 4 | 23 | MTR-04 #9428, INT-01 #9431, MTR-12 #9498, INT-02 #9499 |
| **#9410** | [TRAMITES] Motor de reglas de negocio no-code | 7 | 44 | RGL-01 #9437, RGL-02 #9438, RGL-03 #9439, RGL-04 #9440, RGL-05 #9441, DOC-01 #9442, DOC-02 #9443 |
| **#9381** | [COMPAÑIAS] Admin B2B + contingencia RUNT | 6 | 33 | CMP-01 #9444, CMP-02 #9445, CMP-03 #9446, CMP-04 #9447, CMP-05 #9448, CMP-06 #9449 |
| **#9378** | [OT] Administración de Organismos de Tránsito | 6 | 33 | OT-01 #9454, OT-02 #9455, OT-03 #9456, OT-04 #9457, OT-05 #9458, INT-02 #9459 |
| **#9408** | [TRAMITES] Motor de trámites dinámicos | 4 | 29 | TRA-01 #9433, TRA-02 #9434, TRA-03 #9435, TRA-04 #9436 |
| **#9383** | [COMPAÑIAS] Escrituras / repositorio legal | 4 | 16 | ESC-01 #9450, ESC-02 #9451, ESC-03 #9452, FIL-01 #9453 |
| **#9379** | [OT] Orden parametrizable del consolidado | 3 | 13 | ORD-01 #9460, ORD-02 #9461, ORD-03 #9462 |
| **#9369** | [DASHBOARD] KPIs, filtros y exportación | 3 | 15 | DSH-01 #9463, DSH-02 #9464, DSH-03 #9465 |
| **#9469** | [TRAMITES] IDSecure-Trámites — validación identidad multi-trámite | 13 | 81 | IDS-01 #9478 … IDS-13 #9490 (ver `05-feature-9469-idsecure.md`) |

Convención de prefijos: `F0` fundación de datos, `IDN` identidad, `CMP` compañías, `ESC` escrituras,
`FIL` archivos, `OT` organismos de tránsito, `ORD` orden consolidado, `MTR` motor/matriz, `TRA`
runtime de trámites, `RGL` reglas, `DOC` documental, `DSH` dashboard, `INT` integraciones,
`SEC` seguridad, `QA` calidad, `INF` infraestructura, `IDS` IDSecure.

---

## 2. Archivos DDL (orden de ejecución) y su responsabilidad

| Archivo | Schema(s) | Responsabilidad |
|---|---|---|
| `ddl/00-extensions-and-schemas.sql` | `audit`, todos | Extensiones (`uuidv7`, `citext`, `pg_trgm`), creación de los 11 schemas, `audit.audit_log`, `audit.data_access_log`, funciones de trigger (`increment_row_version`, `touch_updated_at`, `log_change`) y **validadores de reglas** (`is_valid_rule_condition`, `is_valid_rule_actions`). |
| `ddl/10-catalogs.sql` | `catalogs` | Catálogos colombianos canónicos. |
| `ddl/20-identity.sql` | `identity` | Identidad, RBAC por slugs, multi-tenant, onboarding (#9370). |
| `ddl/25-files.sql` | `files` | Metadatos de objetos MinIO (substrato binario). |
| `ddl/30-companies.sql` | `companies` | Compañías, config modular, firmas, escrituras (#9381/#9383). Matriz HU↔tabla: `06-matriz-tablas-companies.md`. |
| `ddl/40-ot.sql` | `ot` | Organismos de Tránsito, reglas OT, orden consolidado, QX (#9378/#9379). |
| `ddl/50-procedures_config.sql` | `procedures_config` | **Núcleo de parametrización** (#9408/#9409/#9410). |
| `ddl/70-integrations.sql` | `integrations` | Ejecución de conectores externos y webhooks. |
| `ddl/75-identity_verification.sql` | `identity_verification` | Liveness/biometría base (#9408 FR-6). |
| `ddl/76-idsecure-tramites.sql` | `identity_verification` | IDSecure: invitaciones, pasos, OCR, dictamen (#9469). |
| `ddl/80-procedures.sql` | `procedures` | Runtime de instancias radicadas + snapshot. |
| `ddl/90-dashboard.sql` | `dashboard` | Read model (vistas/MV) para KPIs (#9369). |

---

## 3. Detalle por Feature

### #9370 — [IDENTIDAD] Núcleo de identidad, autenticación y gobernanza multi-tenant
**Objetivo:** autenticación unificada, control de accesos por **slugs dinámicos**, administración
multi-tenant con aislamiento estricto y autoservicio del colaborador. 3 perfiles: Super Admin,
Tenant Admin, Colaborador. Es la **fundación** sobre la que opera todo el ecosistema.

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| IDN-01 #9412 | BACKEND | Migración del schema de identidad y RLS | 8 |
| F0-01 #9413 | BACKEND | Migración base de schemas y auditoría | 3 |
| F0-03 #9414 | BACKEND | Interceptor de contexto multi-tenant | 5 |
| IDN-02 #9415 | BACKEND | Login unificado con cifrado (RF-1.1/1.2) | 5 |
| IDN-03 #9416 | BACKEND | Bloqueos y rehabilitación de cuenta (RF-1.3/1.4/1.5) | 5 |
| IDN-04 #9417 | BACKEND | RBAC por slugs y verificador (RF-2) | 5 |
| IDN-05 #9418 | BACKEND | Onboarding criptográfico + política de contraseña | 5 |
| IDN-06 #9419 | BACKEND | Consolas Super Admin / Tenant Admin (RF-3) | 5 |
| IDN-07 #9420 | FRONTEND | Login + 4 estados UI + perfil autoservicio | 5 |
| IDN-08 #9421 | FRONTEND | Consolas SA/TA + tickets de soporte | 5 |
| SEC-01 #9422 | BACKEND | Revisión Habeas Data y retención de PII | 3 |
| INF-01 #9423 | BACKEND | Migraciones por ambiente + extensión uuidv7 | 3 |

**Archivos DDL:** `ddl/00` (fundación), `ddl/20` (identity), `ddl/10` (catálogos base).
**Tablas y su función (schema `identity`):**
| Tabla | Función |
|---|---|
| `tenants` | Raíz multi-tenant = Compañía B2B. Frontera de aislamiento; toda fila de negocio referencia su `tenant_id`. |
| `users` | Cuentas (email global único). Estados `active/inactive/temp_blocked/permanent_blocked` + `blocked_until`. |
| `profiles` | Perfil autoservicio 1:1 con `users` (nombre/teléfono/dirección/locale). PII. |
| `permissions` | Catálogo **global** de slugs (`modulo.x.accion`). Define el universo de permisos. |
| `roles` | Roles maestros (globales, `tenant_id` NULL) y locales por tenant (`scope global/tenant`). |
| `role_permissions` | Junction rol↔permiso. |
| `user_roles` | Asignación usuario↔rol dentro de un tenant (impide escalamiento horizontal). |
| `onboarding_invitations` | Enlace firmado 24h, un solo uso (`token_hash`, `expires_at`, `consumed_at`). |
| `password_reset_tokens` | Tokens de restablecimiento (un uso). |
| `refresh_tokens` | Sesiones / refresh tokens (HttpOnly). |
| `password_policies` | Política de contraseña por tenant (longitud, complejidad, lockout). |
| `login_attempts` | Bitácora de intentos y bloqueos (append-only, particionable). |
| `support_tickets` | Tickets de soporte con contexto del colaborador. |

**Funciones SQL:** `identity.is_super_admin()` (bypass de lectura cross-tenant en políticas RLS),
`identity.find_user_for_auth(email)` (SECURITY DEFINER; resuelve la cuenta para el login antes de
fijar el contexto de tenant). **Transversal:** `audit.audit_log` registra cambios; `audit.data_access_log`
registra accesos a PII (SEC-01).
**Servicios/módulos del aplicativo:** `Flit.Modules.Identity` (rebuild), interceptor de conexión EF
Core (F0-03), Gateway YARP (validación de slugs por petición), mensajería (correos de onboarding).

---

### #9409 — [TRAMITES] Motor dinámico por matriz de aristas y consultas externas (DTR-FLIT2)
**Objetivo:** abandonar la lógica rígida por trámite y migrar a **parametrización en BD**: familias →
tipos → **4 aristas** (Vehículo, Propietario/Vendedor, Comprador, Locatario) cruzadas en una **matriz
de conformación**, con renderizado de formularios, documentos y **consultas de interoperabilidad** por
configuración. Encender/apagar trámites y ajustar conformación **sin desplegar**.

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| F0-02 #9424 | BACKEND | Carga de catálogos colombianos | 3 |
| MTR-01 #9425 | BACKEND | Migración de parametrización y matriz | 8 |
| MTR-02 #9426 | BACKEND | API de configuración y resolver (global→tenant→OT) | 8 |
| MTR-03 #9427 | BACKEND | Validación inteligente (VIN/Placa, enrutamiento CC/NIT) | 5 |
| MTR-05 #9429 | BACKEND | Excepción Locatario/Leasing (SIMIT obligatorio, firma/RTM omitible) | 3 |
| MTR-06 #9430 | FRONTEND | Renderizado dinámico del formulario (operador) | 8 |
| MTR-07 #9493 | FRONTEND | Consola admin de tipos y activación | 5 |
| MTR-08 #9494 | FRONTEND | Editor de matriz de conformación | 8 |
| MTR-09 #9495 | FRONTEND | Consola admin de secciones y campos | 8 |
| MTR-10 #9496 | FRONTEND | Consola admin de documentos requeridos | 5 |
| MTR-11 #9497 | FRONTEND | Carga de documentos en radicación | 5 |
| QA-01 #9432 | BACKEND | Suite CF-J + aislamiento horizontal | 5 |

---

### #9467 — [TRAMITES-B] Motor de consultas externas e integraciones (mock DEV)
**Objetivo:** consultas RUNT/SIMIT/RNMC/RES/RUES/FASECOLDA con abstracción de proveedor, mock en DEV,
circuit breaker y UI de resultados. Depende de #9409-A.

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| MTR-04 #9428 | BACKEND | Motor de consultas externas | 8 |
| INT-01 #9431 | BACKEND | Plumbing de conectores externos | 5 |
| MTR-12 #9498 | FRONTEND | Panel de consultas externas en radicación | 5 |
| INT-02 #9499 | FRONTEND | Consola admin de conectores y consultas | 5 |

---
**Archivos DDL:** `ddl/50` (núcleo config), `ddl/10` (catálogos), `ddl/70` (integraciones).
**Tablas y su función (schema `procedures_config`, maestros GLOBALES tenant-exentos):**
| Tabla | Función |
|---|---|
| `procedure_families` | Familias padre: MATRÍCULAS, TRASPASO, OTROS TRÁMITES. |
| `procedure_types` | ~13 tipos hijo. `is_active` controla disponibilidad (selector/URL); `max_steps`≤4. |
| `edges` | Las 4 aristas; `edge_kind` vehicle/person. |
| `procedure_type_edges` | **MATRIZ DE CONFORMACIÓN** (tipo×arista). Fila ausente = no aplica; `is_active=false` = no renderiza/valida. Auditable. |
| `form_sections` | Secciones del formulario por tipo (y override por arista vía FK compuesta). |
| `form_fields` | Campos por sección (tipo, label, obligatoriedad, orden, `ui_state`, `is_trigger`). |
| `query_connectors` | Catálogo de conectores externos (RUNT/SIMIT/RNMC/RESOLUCIONES/RUES/FASECOLDA). |
| `procedure_type_query_configs` | Qué consulta corre por tipo/arista + enrutamiento (CC→Natural, NIT→Jurídica) y obligatoriedad. |
| `required_documents` | Documentos por tipo/arista; `kind` upload/auto_generated; enlace a plantilla. |
| `procedure_type_activations` | **Capa tenant/OT**: activa/override por compañía (y opcional por OT) sin duplicar el maestro. |

**Tablas (schema `integrations`):** `external_query_calls` (bitácora cruda de cada consulta),
`runt_sync_log` (trazas con failover). **Tablas (`catalogs`):** `document_types` (incluye
`default_person_kind` para el enrutamiento), `vehicle_*`, `divipola_*`, `colors`, `fuel_types`.
**Servicios/módulos:** `Flit.Modules.ProceduresConfig` (resolver de config + snapshot),
`Flit.Modules.Integrations` (clientes RUNT/SIMIT/… tras interfaz, concurrencia + circuit breaker).
**API:** `GET /procedures/types`, `GET /procedures/types/{code}/configuration`.

---

### #9410 — [TRAMITES] Motor de reglas de negocio y lógica dinámica (PRD-025)
**Objetivo:** constructor visual **no-code** If/Else que evalúa datos del trámite en tiempo real y
dispara acciones (popup, sección condicional, consumo de endpoint), adaptando el flujo sin desarrollo.

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| RGL-01 #9437 | BACKEND | Migración de reglas y endpoints + validadores JSONB | 5 |
| RGL-02 #9438 | BACKEND | Evaluador de reglas con prioridad y hot-swap | 8 |
| RGL-03 #9439 | BACKEND | Catálogo de endpoints seguro | 5 |
| RGL-04 #9440 | FRONTEND | Constructor visual de reglas | 8 |
| RGL-05 #9441 | FRONTEND | Runtime de reglas en el operador | 5 |
| DOC-01 #9442 | BACKEND | Generación documental por plantillas (QuestPDF) | 8 |
| DOC-02 #9443 | BACKEND | Empaquetado del consolidado por OT | 5 |

**Archivos DDL:** `ddl/50` (rules/endpoint_catalog/plantillas), `ddl/00` (validadores), `ddl/70`,
`ddl/80` (documentos de instancia).
**Tablas y su función:**
| Tabla | Schema | Función |
|---|---|---|
| `rules` | procedures_config | Reglas no-code por tipo+tenant: `condition_tree`/`actions` JSONB validados por CHECK; `priority`; `is_active` (OFF no evalúa). |
| `endpoint_catalog` | procedures_config | Endpoints invocables por reglas (URL/método/auth por **referencia**, no plaintext). |
| `document_templates` | procedures_config | Cabecera de plantilla (scope global/tenant). |
| `document_template_versions` | procedures_config | Versión **inmutable** con `marker_map` (marcador→fuente de dato). |
| `endpoint_call_log` | integrations | Bitácora de invocaciones a endpoints desde reglas. |
| `procedure_documents` | procedures | Documento generado (guarda `template_version_id` y `data_snapshot`). |

**Funciones SQL:** `public.is_valid_rule_condition(jsonb)`, `public.is_valid_rule_actions(jsonb)`
(la BD rechaza árboles malformados; sin SQL embebido → sin inyección).
**Servicios/módulos:** evaluador de reglas en `Flit.Modules.ProceduresConfig`/`Procedures`;
generación documental con **QuestPDF** (`Flit.SharedKernel.Pdf`); binarios en **MinIO**.

---

### #9381 — [COMPAÑIAS] Administrador de Compañías B2B, configuración modular y contingencia RUNT
**Objetivo:** consola SaaS multi-tenant con **parametrización en caliente** (hot-reload) de políticas,
reglas, integraciones y mensajería por cliente, con **resiliencia ante caídas del RUNT** (objetivo 99.9%).

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| CMP-01 #9444 | BACKEND | Migración de compañías y configuración modular | 5 |
| CMP-02 #9445 | BACKEND | Consola de indexación B2B (filtros + Ver/Editar) | 5 |
| CMP-03 #9446 | BACKEND | Configuración modular hot-reload | 5 |
| CMP-04 #9447 | BACKEND | Contingencia RUNT con failover (Verifik/Intempo) | 8 |
| CMP-05 #9448 | BACKEND | Interceptor de propiedad vehicular | 5 |
| CMP-06 #9449 | FRONTEND | Consola B2B por pestañas | 5 |

**Archivos DDL:** `ddl/30` (companies), `ddl/70` (runt_sync_log).
**Tablas y su función (schema `companies`):**
| Tabla | Función |
|---|---|
| `companies` | Maestro de compañía 1:1 con `tenant`; `modules_enabled` controla visibilidad de pestañas (p. ej. escrituras). |
| `company_module_configs` | Config por módulo (`registration/transfers/company/runt_contingency`) en `jsonb`, hot-reload. |
| `signature_wallets` | "Bal de firmas": saldo, umbral, autorecarga. |
| `signature_wallet_movements` | Ledger append-only de consumo/recarga de firmas. |
| `vehicle_ownership_rules` | Interceptor de propiedad vehicular (reglas JSONB por tenant). |
| `runt_sync_log` (integrations) | Trazas RUNT/Verifik/Intempo con `failover_from` para la contingencia. |
**Servicios/módulos:** `Flit.Modules.Companies`; adaptadores RUNT con failover en
`Flit.Modules.Integrations`; filtros con índices `gin_trgm` (NIT/Nombre).

---

### #9383 — [COMPAÑIAS] Parametrización de Escrituras y repositorio legal por tenant
**Objetivo:** pestaña en Admin de Compañías para el repositorio legal de escrituras, **visible solo si
la compañía la tiene activada**, con CRUD y adjuntos PDF.

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| ESC-01 #9450 | BACKEND | Migración de escrituras + visibilidad condicional | 3 |
| ESC-02 #9451 | BACKEND | CRUD de escrituras con adjuntos PDF | 5 |
| ESC-03 #9452 | FRONTEND | Data grid de escrituras | 5 |
| FIL-01 #9453 | BACKEND | Substrato de archivos MinIO | 3 |

**Archivos DDL:** `ddl/30` (escrituras), `ddl/25` (files).
**Tablas y su función:**
| Tabla | Schema | Función |
|---|---|---|
| `escrituras` | companies | Registro legal por compañía (tipo doc, número, vigencia). Visible si `modules_enabled.escrituras`. |
| `escritura_attachments` | companies | Adjuntos PDF (`position` 1..5, ≤3MB, reemplazo total al actualizar). Enlaza `file_id`. |
| `files` | files | Metadatos del objeto en MinIO; servido por URL prefirmada. |
**Servicios/módulos:** `Flit.Modules.Companies` (escrituras), `Flit.Modules.Files` (MinIO, URLs
prefirmadas; validación PDF/tamaño en la capa de aplicación).

---

### #9378 — [OT] Módulo de Administración de Organismos de Tránsito
**Objetivo:** unificar la operación transaccional en una súper-sección **Trámites** (Dashboard + QX/
colas tipo Quipux) y un **motor de reglas dinámico** por OT, con vistas base (ficha + usuarios/permisos).

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| OT-01 #9454 | BACKEND | Migración de OT y semilla (~370) | 5 |
| OT-02 #9455 | BACKEND | Trámites unificados (Dashboard + QX/colas) | 8 |
| OT-03 #9456 | BACKEND | Constructor de reglas de OT | 5 |
| OT-04 #9457 | FRONTEND | Navegación de tres bloques + vistas base | 5 |
| OT-05 #9458 | FRONTEND | Interfaz del constructor de reglas OT | 5 |
| INT-02 #9459 | BACKEND | Webhooks QX inbound/outbound + idempotencia | 5 |

**Archivos DDL:** `ddl/40` (ot), `ddl/70` (webhook_events).
**Tablas y su función (schema `ot`, referencia cross-tenant, sin `tenant_id` — ADR-0012):**
| Tabla | Función |
|---|---|
| `traffic_agencies` | Registro de OT (catálogo; flags por familia, `external_refs` con ids de integración). Semilla ~370. |
| `ot_users` | Operadores de la consola OT (aislados por `traffic_agency_id`). |
| `ot_user_permissions` | Permisos operativos por usuario OT. |
| `ot_rules` | Constructor de reglas OT (gatillo/condición/acciones, toggle hot). |
| `ot_qx_integrations` | Modo de gestión por OT: `dashboard` o `qx` (callback + auth). |
| `webhook_events` (integrations) | Eventos QX inbound/outbound con `idempotency_key`. |
**Servicios/módulos:** `Flit.Modules.TrafficAgencies`, `Flit.Modules.Integrations` (webhooks QX),
GUC de sesión `app.current_agency_id` para el aislamiento por OT.

---

### #9379 — [OT] Parametrización y ordenamiento del consolidado de documentos
**Objetivo:** que el administrador gestione el **orden** de los documentos del expediente consolidado
por OT (drag&drop), sin depender de desarrollo, con guardado <500 ms.

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| ORD-01 #9460 | BACKEND | Migración del orden del consolidado | 3 |
| ORD-02 #9461 | BACKEND | Gestión del orden (reordenar/agregar/quitar <500ms) | 5 |
| ORD-03 #9462 | FRONTEND | Reordenamiento por drag and drop | 5 |

**Archivos DDL:** `ddl/40` (ot).
**Tablas y su función:**
| Tabla | Función |
|---|---|
| `ot_consolidated_doc_orders` | Cabecera del orden por OT; 1 activo por OT (índice único parcial). |
| `ot_consolidated_doc_order_items` | Ítems ordenados (`position`, `source` global/custom). Quitar = borrar la fila (no toca archivos físicos). |
**Servicios/módulos:** `Flit.Modules.TrafficAgencies`; transacción corta + índice `uq(order_id,position)`
para el SLA de <500 ms; frontend con drag&drop (PrimeReact).

---

### #9408 — [TRAMITES] Motor de trámites dinámicos y parametrizables (PRD-024)
**Objetivo:** runtime del trámite impulsado por la parametrización: wizard ≤4 pasos, consultas
parametrizadas, ciclo de vida (Borrador editable / resto read-only) y **validación de identidad**
automática a los actores. Persiste un **snapshot inmutable** de la config al radicar (ADR-0010).

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| TRA-01 #9433 | BACKEND | Migración del runtime de trámites + guard de estados | 8 |
| TRA-02 #9434 | BACKEND | Radicación con snapshot de configuración | 8 |
| TRA-03 #9435 | BACKEND | Validación de identidad reutilizable | 5 |
| TRA-04 #9436 | FRONTEND | Wizard de radicación por pasos | 8 |

**Archivos DDL:** `ddl/80` (procedures), `ddl/75` (identity_verification).
**Tablas y su función (schema `procedures`):**
| Tabla | Función |
|---|---|
| `procedure_instances` | Instancia radicada: `state` (9 estados), `config_snapshot` **inmutable**, `reference_number`, asignación, monto. |
| `procedure_field_values` | Valores capturados (normalizado: `field_key`, `edge_role`, `value` jsonb). |
| `procedure_actors` | Actores por arista (`person_kind` natural/jurídica, documento PII). |
| `procedure_actor_representatives` | Representante legal (sub-actor de persona jurídica). |
| `procedure_vehicles` | Arista vehículo (placa o VIN, catálogos, `runt_snapshot`). |
| `procedure_query_results` | **Snapshot** de resultados de consultas externas (1 por fuente; fallo aislado). |
| `procedure_documents` | Documentos adjuntos (upload) y generados (auto_generated). |
| `procedure_state_history` | Historial de transiciones (append-only). |
| `procedure_identity_validations` | Enlace a la validación de identidad reutilizada. |

**Tablas (schema `identity_verification`):** `verification_sessions` (liveness/veredicto, PII alta),
`verification_evidences` (evidencias; binario en `files`).
**Funciones SQL:** `procedures.validate_state_transition()` (guard de la máquina de estados).
**Servicios/módulos:** `Flit.Modules.Procedures` (rebuild), `Flit.Modules.IdentityVerification`
(Verifik/liveness), outbox + RabbitMQ para eventos de estado, MinIO para evidencias.

---

### #9369 — [DASHBOARD] Dashboard multitenant con KPIs, filtros y exportación
**Objetivo:** visualizar y analizar el estado de los trámites con **KPIs en tiempo real**, filtros
(fechas/OT/cliente) y exportación Excel/PDF, con **visibilidad por rol** (tenant vs cross-tenant Super
Admin).

**Historias:**
| HU | Capa | Título | SP |
|---|---|---|---|
| DSH-01 #9463 | BACKEND | Read model con visibilidad por rol | 5 |
| DSH-02 #9464 | BACKEND | Exportación Excel y PDF | 5 |
| DSH-03 #9465 | FRONTEND | KPIs, gráfico y filtros | 5 |

**Archivos DDL:** `ddl/90` (dashboard).
**Tablas/vistas y su función (schema `dashboard`):**
| Objeto | Función |
|---|---|
| `v_procedure_kpis` (vista) | Conteos por tenant/OT/estado/día (RLS vía `security_invoker`). |
| `v_user_productivity` (vista) | Productividad por usuario y estado. |
| `v_ot_distribution` (vista) | Distribución por Organismo de Tránsito. |
| `mv_procedure_kpis` (MV) | Materialización opcional para alto volumen (refresh por evento). |
| `read_model_refresh_log` | Bitácora de refresco de la MV. |
**Servicios/módulos:** `Flit.Modules.Dashboard`; exportación con **ClosedXML** (Excel, ADR-0002) y
**QuestPDF** (PDF ejecutivo, ADR-0005); `identity.is_super_admin()` habilita la vista consolidada
cross-tenant del Super Admin.

---

## 4. Tablas transversales (todas las features)

| Objeto | Schema | Función |
|---|---|---|
| `audit_log` | audit | Bitácora inmutable de cambios (INSERT/UPDATE/DELETE) por trigger en cada tabla de negocio. |
| `data_access_log` | audit | Trazabilidad de accesos/lecturas sensibles (URLs prefirmadas, PII) — Habeas Data. |
| `increment_row_version()` | audit | Trigger de concurrencia optimista (`row_version`) + `updated_at`. |
| `touch_updated_at()` | audit | Trigger de `updated_at` para catálogos. |
| `log_change()` | audit | Trigger genérico de auditoría. |
| `uuidv7()` | public | PK ordenable temporalmente (fallback portable). |

---

## 5. Documentos relacionados
- `00-diseno-arquitectonico.md` — diseño, sequence diagram, cobertura por criterio funcional.
- `01-er-diagram.md` — modelo entidad-relación (Mermaid) de los 11 schemas.
- `02-plan-implementacion.md` — plan por fases, dependencias y roadmap por sprint.
- `ddl/*.sql` — DDL completo (orden 00→90).
- `decisions/ADR-0009..0012` — decisiones de parametrización, snapshot, reglas y tenancy de OT.
- `diagrama-features.png` — mapa funcional de Features, relaciones y servicios.
