# Diseño: Feature #9566 — ADMIN-OT (Administración de Organismos de Tránsito)

**Fecha:** 2026-06-10
**Autor:** Architecture Agent v2.0
**Estado:** Propuesto
**ADRs aplicables:** ADR-0009 (Híbrido JSONB), ADR-0010 (RLS), ADR-0011 (Strategy — Quipux connector)
**Módulo backend:** `Flit.Modules.OT`
**Feature frontend:** `features/ot-admin`
**Depende de:** Feature #9565 (ADMIN-COMPAÑÍAS), #9729 (CONSOLIDACIÓN-DOCUMENTAL)

---

## 1. Resumen y Alcance

### IN (incluido)
- Consolidación del menú en súper-sección "Trámites" con sub-secciones: Dashboard y cola QX.
- Switch maestro: **Modo Dashboard** (nativo FLIT) vs **Modo QX** (integración Quipux).
- En Modo QX: ocultar botones aprobar/rechazar (solo lectura + auditoría); hot-update asíncrono de estados via webhooks/callbacks de Quipux.
- Tablero de logs de integración Quipux: payloads JSON, tiempos de respuesta, códigos HTTP.
- Motor de reglas dinámico del OT con hot-swapping sin despliegue (JSONB, sin migraciones).
- Pestaña de prelación documental: drag-and-drop de documentos por tipo de trámite.
- Recálculo de orden de prelación < 500 ms; el PDF consolidado adopta el orden de inmediato.
- CRUD de etiquetas de documentos personalizados con confirmaciones de exclusión (impacto en adjuntos existentes).

### OUT (excluido)
- Gestión de usuarios internos del OT (Feature #9567).
- Integración Quipux de envío/recepción de expedientes (solo hot-update de estados).
- Motor de pagos/recaudo del OT.

---

## 2. Diagrama de Secuencia — Flujos Principales

### 2a. Hot-update de estado vía Quipux (Modo QX)

```mermaid
sequenceDiagram
  participant QX as Quipux (externo)
  participant GW as Flit.Gateway
  participant API as Flit.Api
  participant OT as Flit.Modules.OT
  participant PR as Flit.Modules.Procedures
  participant SIG as SignalR
  participant OP as Operador (Frontend)

  QX->>GW: POST /webhooks/quipux/{ot_slug}
    { event: "status_changed", procedure_ref, new_status, timestamp }
  GW->>API: forward (webhook token validado)
  API->>OT: QuipuxWebhookCommand { ot_slug, procedure_ref, new_status }
  OT->>OT: Lookup procedure by composite_id (procedure_ref)
  OT->>PR: UpdateProcedureStatusCommand { procedure_id, new_status }
  PR->>DB: UPDATE procedures SET status=new_status
  OT->>DB: INSERT ot_integration_logs(ot_id, event, payload, duration_ms)
  OT->>SIG: Push "procedure_status_update" { procedure_id, new_status }
  SIG-->>OP: { procedure_id: "TRASP-02_EVE-8841", status: "approved" }
```

### 2b. Drag-and-drop de prelación documental

```mermaid
sequenceDiagram
  participant TA as TenantAdmin (Frontend)
  participant API as Flit.Api
  participant OT as Flit.Modules.OT
  participant DOC as Flit.Modules.Documents
  participant DB as PostgreSQL

  TA->>API: PUT /ot/{otId}/document-order/{procedureTypeId}
    { ordered_document_type_ids: [uuid1, uuid2, uuid3] }
  API->>OT: UpdateDocumentOrderCommand { otId, procedureTypeId, orderedIds }
  OT->>DB: UPSERT ot_document_order(ordered_document_ids=jsonb)
  Note over OT,DOC: Notificar a Documents del cambio de prelación
  OT->>DOC: DocumentOrderChangedEvent { otId, procedureTypeId }
  DOC->>DOC: Next consolidation will use new order
  OT-->>API: { updated: true, order: [...] }
  API-->>TA: 200 { message: "Prelación actualizada" }
  Note over TA: Próxima descarga del PDF usa el nuevo orden
```

---

## 3. Contratos API

| Método | Ruta | Descripción | Permisos |
|---|---|---|---|
| GET | `/ot` | Lista organismos de tránsito del tenant | `ot.read` |
| POST | `/ot` | Crea organismo de tránsito | `ot.manage` |
| GET | `/ot/{id}` | Detalle del OT (config, modo, Quipux) | `ot.read` |
| PUT | `/ot/{id}` | Actualiza nombre y configuración básica | `ot.manage` |
| PUT | `/ot/{id}/mode` | Switch Modo Dashboard / Modo QX | `ot.manage` |
| PUT | `/ot/{id}/quipux-config` | Configura credenciales y endpoint Quipux | `ot.manage` |
| GET | `/ot/{id}/integration-logs` | Logs Quipux: payloads, tiempos, códigos | `ot.read` |
| GET | `/ot/{id}/rules` | Lista reglas dinámicas del OT | `ot.read` |
| POST | `/ot/{id}/rules` | Crea regla dinámica (JSONB) | `ot.manage` |
| PUT | `/ot/{id}/rules/{ruleId}` | Actualiza regla (hot-swap sin deploy) | `ot.manage` |
| DELETE | `/ot/{id}/rules/{ruleId}` | Elimina regla | `ot.manage` |
| GET | `/ot/{id}/document-order` | Prelación documental por tipo de trámite | `ot.read` |
| PUT | `/ot/{id}/document-order/{procedureTypeId}` | Actualiza orden de prelación (< 500ms) | `ot.manage` |
| GET | `/ot/{id}/labels` | Lista etiquetas personalizadas | `ot.read` |
| POST | `/ot/{id}/labels` | Crea etiqueta de documento | `ot.manage` |
| PUT | `/ot/{id}/labels/{labelId}` | Actualiza etiqueta | `ot.manage` |
| DELETE | `/ot/{id}/labels/{labelId}` | Elimina etiqueta (con confirmación de impacto) | `ot.manage` |
| GET | `/ot/{id}/labels/{labelId}/impact` | Cuenta adjuntos que usan esta etiqueta | `ot.read` |
| POST | `/webhooks/quipux/{ot_slug}` | Webhook callback de Quipux (hot-update estados) | webhook_token |

```yaml
# PUT /ot/{id}/mode
request:
  mode: "dashboard"|"qx"
  # Si mode="qx": habilitar solo-lectura (ocultar aprobar/rechazar en frontend)

# PUT /ot/{id}/document-order/{procedureTypeId}
request:
  ordered_document_type_ids: uuid[]   # orden de prelación
response:
  procedure_type_id: uuid
  ordered_documents:
    - order_index: int
      document_type: { id, name }

# POST /ot/{id}/rules
request:
  name: string
  conditions: { ... }    # mismo formato que RuleSet en procedures_config (JSONB)
  actions: { ... }
  is_active: bool
response:
  rule_id: uuid
  # Efecto inmediato: hot-swap (sin deploy)

# DELETE /ot/{id}/labels/{labelId}
# Requiere confirmación explícita si impact_count > 0
request:
  confirm: bool    # true = proceder aunque haya adjuntos usando esta etiqueta
response:
  deleted: bool
  affected_attachments: int
```

---

## 4. Modelo de Datos

### Schema: `ot`

```sql
CREATE TABLE ot.ot_organisms (
  id              uuid DEFAULT gen_ulid() PRIMARY KEY,
  tenant_id       uuid NOT NULL REFERENCES identity.tenants(id),
  slug            text NOT NULL,
  name            text NOT NULL,
  mode            text NOT NULL DEFAULT 'dashboard'
                  CHECK (mode IN ('dashboard','qx')),
  quipux_enabled  bool NOT NULL DEFAULT false,
  quipux_config   jsonb,   -- { endpoint, webhook_token_hash, auth_config }
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  deleted_at      timestamptz,
  CONSTRAINT uq_ot_slug_tenant UNIQUE (slug, tenant_id)
);
ALTER TABLE ot.ot_organisms ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON ot.ot_organisms
  USING (tenant_id = current_setting('app.tenant_id', true)::uuid);

-- Prelación documental por OT y tipo de trámite
CREATE TABLE ot.ot_document_order (
  id                  uuid DEFAULT gen_ulid() PRIMARY KEY,
  ot_id               uuid NOT NULL REFERENCES ot.ot_organisms(id),
  procedure_type_id   uuid NOT NULL REFERENCES procedures_config.procedure_types(id),
  tenant_id           uuid NOT NULL,
  ordered_document_type_ids jsonb NOT NULL DEFAULT '[]',
                      -- Array de UUIDs en el orden de prelación
  updated_by          uuid,
  updated_at          timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_ot_doc_order UNIQUE (ot_id, procedure_type_id)
);
ALTER TABLE ot.ot_document_order ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON ot.ot_document_order
  USING (tenant_id = current_setting('app.tenant_id', true)::uuid);

-- Etiquetas de documentos personalizadas
CREATE TABLE ot.ot_document_labels (
  id          uuid DEFAULT gen_ulid() PRIMARY KEY,
  ot_id       uuid NOT NULL REFERENCES ot.ot_organisms(id),
  tenant_id   uuid NOT NULL,
  slug        text NOT NULL,
  display_name text NOT NULL,
  is_active   bool NOT NULL DEFAULT true,
  created_at  timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_ot_label_slug UNIQUE (ot_id, slug)
);
ALTER TABLE ot.ot_document_labels ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON ot.ot_document_labels
  USING (tenant_id = current_setting('app.tenant_id', true)::uuid);

-- Reglas dinámicas del OT (hot-swap sin deploy)
CREATE TABLE ot.ot_rule_sets (
  id          uuid DEFAULT gen_ulid() PRIMARY KEY,
  ot_id       uuid NOT NULL REFERENCES ot.ot_organisms(id),
  tenant_id   uuid NOT NULL,
  name        text NOT NULL,
  conditions  jsonb NOT NULL,
  actions     jsonb NOT NULL,
  is_active   bool NOT NULL DEFAULT true,
  version     int NOT NULL DEFAULT 1,
  updated_at  timestamptz NOT NULL DEFAULT now(),
  created_at  timestamptz NOT NULL DEFAULT now()
);
ALTER TABLE ot.ot_rule_sets ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON ot.ot_rule_sets
  USING (tenant_id = current_setting('app.tenant_id', true)::uuid);

-- Logs de integración Quipux (inmutables)
CREATE TABLE ot.ot_integration_logs (
  id              uuid DEFAULT gen_ulid() PRIMARY KEY,
  ot_id           uuid NOT NULL REFERENCES ot.ot_organisms(id),
  tenant_id       uuid NOT NULL,
  event_type      text NOT NULL,
  procedure_ref   text,
  request_payload jsonb,
  response_payload jsonb,
  http_status     int,
  duration_ms     int,
  logged_at       timestamptz NOT NULL DEFAULT now()
);
ALTER TABLE ot.ot_integration_logs ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON ot.ot_integration_logs
  USING (tenant_id = current_setting('app.tenant_id', true)::uuid);
CREATE INDEX ix_ot_logs_ot_id_logged_at ON ot.ot_integration_logs(ot_id, logged_at DESC);
```

---

## 5. Componentes Backend

### `Flit.Modules.OT`

```
Domain/
  Entities/     OtOrganism, OtDocumentOrder, OtDocumentLabel, OtRuleSet, OtIntegrationLog
  Interfaces/   IQuipuxConnector    (implementación en Flit.Modules.Integrations)
  Events/       DocumentOrderChanged, OtModeChanged, QuipuxWebhookReceived

Application/
  Commands/
    CreateOtCommand + Handler
    UpdateOtModeCommand + Handler         ← switch Dashboard/QX
    UpdateQuipuxConfigCommand + Handler
    UpdateDocumentOrderCommand + Handler  ← < 500ms target
    QuipuxWebhookCommand + Handler        ← hot-update status desde Quipux
    CreateOtRuleSetCommand + Handler
    UpdateOtRuleSetCommand + Handler      ← hot-swap inmediato
    CreateOtLabelCommand + Handler
    DeleteOtLabelCommand + Handler        ← con validación de impacto en adjuntos
  Queries/
    ListOtOrganismsQuery + Handler
    GetOtDetailQuery + Handler
    GetDocumentOrderQuery + Handler
    GetOtRulesQuery + Handler
    GetOtLabelsQuery + Handler
    GetOtLabelImpactQuery + Handler       ← cuenta adjuntos usando la etiqueta
    GetOtIntegrationLogsQuery + Handler

Infrastructure/
  Persistence/      OtRepository, OtDocumentOrderRepository, OtRuleSetRepository
  Webhooks/
    QuipuxWebhookValidator.cs            ← valida token HMAC del webhook
  ModuleExtensions.cs
```

---

## 6. Componentes Frontend

### `features/ot-admin`

```
features/ot-admin/
├── api/
│   ├── ot-admin.schemas.ts
│   └── ot-admin.api.ts   (useOtOrganisms, useOtDetail, useOtMode, useDocumentOrder,
│                           useOtRules, useOtLabels, useOtIntegrationLogs,
│                           useUpdateDocumentOrder, useDeleteLabel)
├── components/
│   ├── OtList.tsx                    (lista OTs del tenant)
│   ├── OtModeSwitch.tsx              (toggle: Modo Dashboard ↔ Modo QX)
│   ├── QuipuxConfigForm.tsx          (endpoint, webhook token)
│   ├── OtIntegrationLogsTable.tsx    (payloads JSON expandibles, filtro por evento)
│   ├── OtRuleSetManager.tsx          (CRUD reglas dinámicas con hot-swap)
│   ├── DocumentOrderEditor/
│   │   ├── DocumentOrderEditor.tsx   (drag-and-drop por tipo de trámite)
│   │   └── DocumentOrderDragItem.tsx (un documento arrastrable)
│   └── OtLabelsManager/
│       ├── OtLabelsManager.tsx       (CRUD etiquetas)
│       └── DeleteLabelModal.tsx      (confirmación con conteo de impacto)
└── pages/
    ├── OtListPage.tsx                 (/admin/ot)
    └── OtDetailPage.tsx               (/admin/ot/:id)
```

**`DocumentOrderEditor`** — drag-and-drop performance:
- Usa `@dnd-kit/core` (o PrimeReact `OrderList`) para reordenamiento.
- En `onDragEnd`: envía `PUT /ot/{id}/document-order/{procedureTypeId}` inmediatamente.
- El servidor responde < 500ms (solo `UPSERT` en `ot_document_order` con JSONB).

**`OtModeSwitch`** — en Modo QX:
- Oculta botones "Aprobar" y "Rechazar" en la cola de trámites del OT.
- Muestra badge "Modo QX" en el header de la sección.

### Estados UI

| Componente | Vacío | Cargando | Error | Con datos |
|---|---|---|---|---|
| OtList | "No hay OTs configurados" | Skeleton | ErrorState | Lista con modo badge |
| DocumentOrderEditor | "Sin documentos configurados" | Skeleton | ErrorState | Lista draggable |
| OtIntegrationLogsTable | "Sin logs de integración" | Skeleton | ErrorState | Tabla con JSON expandible |
| OtLabelsManager | "Sin etiquetas personalizadas" | Skeleton | ErrorState | Lista con acciones |

---

## 7. Archivos a Crear / Modificar

### Backend
```
services/core-api/src/Flit.Modules.OT/              [CREAR todo el módulo]
  Flit.Modules.OT.csproj
  Domain/Entities/{OtOrganism, OtDocumentOrder, OtDocumentLabel,
                   OtRuleSet, OtIntegrationLog}.cs
  Application/Commands/{CreateOt, UpdateOtMode, UpdateDocumentOrder,
                        QuipuxWebhook, CreateOtRuleSet, UpdateOtRuleSet,
                        CreateOtLabel, DeleteOtLabel}Command.cs + Handlers
  Application/Queries/{ListOtOrganisms, GetOtDetail, GetDocumentOrder,
                       GetOtRules, GetOtLabels, GetOtLabelImpact,
                       GetOtIntegrationLogs}Query.cs + Handlers
  Infrastructure/Persistence/*.cs
  Infrastructure/Webhooks/QuipuxWebhookValidator.cs
  Infrastructure/ModuleExtensions.cs
services/core-api/src/Flit.Modules.Integrations/    [MODIFICAR]
  Infrastructure/Connectors/Quipux/QuipuxConnector.cs  [CREAR]
  Infrastructure/Connectors/Quipux/MockQuipuxConnector.cs  [CREAR]
services/core-api/src/Flit.Infrastructure/
  Persistence/FlitDbContext.cs                        [MODIFICAR]
```

### Frontend
```
frontend/src/features/ot-admin/                      [CREAR todo]
  api/{ot-admin.schemas, ot-admin.api}.ts
  components/{OtList, OtModeSwitch, QuipuxConfigForm,
              OtIntegrationLogsTable, OtRuleSetManager,
              DocumentOrderEditor/*, OtLabelsManager/*}.tsx
  pages/{OtListPage, OtDetailPage}.tsx
```

---

## 8. Notas Operativas

- **database-agent:** Schema `ot`. El campo `quipux_config` en `ot_organisms` contiene `webhook_token_hash` — el token en texto plano no se almacena. Índice en `ot_integration_logs(ot_id, logged_at DESC)` para consultas recientes eficientes. FK cross-schema a `procedures_config.procedure_types` en `ot_document_order`.
- **backend-agent:** `QuipuxWebhookCommand` valida el token HMAC (hash del body con el secret del OT). `UpdateDocumentOrderCommand` hace UPSERT del JSONB en `ot_document_order` — debe completarse en < 500ms (es solo un UPSERT de una fila). El motor de reglas del OT (`ot_rule_sets`) se carga en memoria al evaluar trámites del OT — hot-swap via Wolverine event que invalida el cache del motor.
- **frontend-agent:** `DocumentOrderEditor` — medir que el round-trip PUT + actualización visual < 500ms total. `DeleteLabelModal` llama primero a `GET /ot/{id}/labels/{labelId}/impact` para mostrar el conteo de adjuntos afectados. En Modo QX, el componente de cola de trámites recibe `mode='qx'` como prop y no renderiza los botones de acción.
- **security-agent:** El webhook endpoint `/webhooks/quipux/{ot_slug}` no requiere JWT pero sí validación HMAC del token. Auditar que `quipux_config.webhook_token_hash` se almacena como hash, nunca en texto plano. Los logs de integración no deben incluir credenciales en el payload.
- **qa-agent:** TC de switch Modo QX → verificar que botones aprobar/rechazar desaparecen de la UI. TC de webhook Quipux → estado del trámite se actualiza en < 2s. TC de drag-and-drop prelación → próximo PDF consolidado usa el nuevo orden. TC de delete label con adjuntos → modal muestra conteo correcto y fuerza confirmación.

---

## 9. Descomposición Preliminar en HUs

| # | Título | Tipo | Dependencias |
|---|---|---|---|
| HU-9566-01 | CRUD de Organismos de Tránsito y switch Modo Dashboard / Modo QX (backend) | [BACKEND] | HU-9565-01 |
| HU-9566-02 | Prelación documental drag-and-drop y etiquetas personalizadas (backend < 500ms) | [BACKEND] | HU-9566-01, HU-9729-03 |
| HU-9566-03 | Integración Quipux: webhook hot-update de estados y logs de integración | [BACKEND] | HU-9566-01, ADR-0011 |
| HU-9566-04 | Frontend: gestión de OT, switch de modo, reglas dinámicas y logs Quipux | [FRONTEND] | HU-9566-01, HU-9566-03 |
| HU-9566-05 | Frontend: editor drag-and-drop de prelación documental y CRUD de etiquetas | [FRONTEND] | HU-9566-02, HU-9566-04 |

---

*Diseño generado por: Architecture Agent v2.0 — 2026-06-10 | Estado: Propuesto*
