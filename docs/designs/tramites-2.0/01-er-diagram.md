# Diagrama ER — Trámites de Tránsito FLIT 2.0

> Resumen de relaciones de los 11 schemas. Las columnas estándar (`id`, `tenant_id`,
> `created/updated/deleted_*`, `row_version`) se omiten en los bloques de atributos por brevedad;
> se muestran solo las columnas distintivas. Cardinalidad: `||--o{` = uno a muchos, `||--o|` = uno a
> cero/uno, `}o--||` = muchos a uno.

## 1. Núcleo de parametrización + runtime (corazón del diseño)

```mermaid
erDiagram
  procedure_families ||--o{ procedure_types : "1:N"
  procedure_types ||--o{ procedure_type_edges : "matriz"
  edges ||--o{ procedure_type_edges : "matriz"
  procedure_types ||--o{ procedure_type_activations : "override tenant/OT"
  procedure_types ||--o{ form_sections : ""
  procedure_type_edges ||--o{ form_sections : "override arista"
  form_sections ||--o{ form_fields : ""
  procedure_types ||--o{ procedure_type_query_configs : ""
  procedure_type_edges ||--o{ procedure_type_query_configs : "override arista"
  query_connectors ||--o{ procedure_type_query_configs : ""
  procedure_types ||--o{ required_documents : ""
  procedure_type_edges ||--o{ required_documents : "override arista"
  document_templates ||--o{ document_template_versions : "versiones"
  required_documents }o--o| document_templates : "auto_generated"
  procedure_types ||--o{ rules : "no-code"
  procedure_types ||--o{ procedure_instances : "instancias"
  traffic_agencies ||--o{ procedure_instances : "radicado en"
  tenants ||--o{ procedure_instances : "RLS"
  procedure_instances ||--o{ procedure_field_values : ""
  procedure_instances ||--o{ procedure_actors : ""
  procedure_actors ||--o| procedure_actor_representatives : "rep. legal"
  procedure_instances ||--o| procedure_vehicles : ""
  procedure_instances ||--o{ procedure_query_results : "snapshot"
  procedure_instances ||--o{ procedure_documents : ""
  procedure_instances ||--o{ procedure_state_history : ""
  procedure_instances ||--o{ procedure_identity_validations : ""
  verification_sessions ||--o{ procedure_identity_validations : "reusada"

  procedure_families {
    uuid id PK
    text code UK
    text name
    boolean is_active
  }
  procedure_types {
    uuid id PK
    uuid family_id FK
    text code UK
    text slug UK
    int max_steps
    boolean is_active
  }
  edges {
    uuid id PK
    text code UK
    text edge_kind "vehicle|person"
  }
  procedure_type_edges {
    uuid id PK
    uuid procedure_type_id FK
    uuid edge_id FK
    boolean is_active
    boolean is_required
    int display_order
    text role_label
  }
  procedure_type_activations {
    uuid id PK
    uuid tenant_id FK
    uuid procedure_type_id FK
    uuid traffic_agency_id FK
    boolean is_active
    jsonb overrides
  }
  form_sections {
    uuid id PK
    uuid procedure_type_id FK
    uuid edge_id FK "nullable"
    text section_key
    text ui_mode
  }
  form_fields {
    uuid id PK
    uuid section_id FK
    text field_key
    text data_type
    boolean is_required
    boolean is_trigger
    text ui_state
  }
  query_connectors {
    uuid id PK
    text code UK "RUNT|SIMIT|RNMC|RESOLUCIONES|RUES|FASECOLDA"
  }
  procedure_type_query_configs {
    uuid id PK
    uuid procedure_type_id FK
    uuid edge_id FK "nullable"
    uuid query_connector_id FK
    boolean is_mandatory
    boolean is_omitible
    text person_kind_filter
  }
  required_documents {
    uuid id PK
    uuid procedure_type_id FK
    uuid edge_id FK "nullable"
    uuid document_type_id FK
    text kind "upload|auto_generated"
    uuid template_id FK "nullable"
  }
  document_templates {
    uuid id PK
    text code UK
    text scope "global|tenant"
    int current_version
  }
  document_template_versions {
    uuid id PK
    uuid template_id FK
    int version
    uuid body_file_id FK
    jsonb marker_map
    boolean is_current
  }
  rules {
    uuid id PK
    uuid tenant_id FK
    uuid procedure_type_id FK
    jsonb condition_tree
    jsonb actions
    int priority
    boolean is_active
  }
  procedure_instances {
    uuid id PK
    uuid tenant_id FK
    uuid procedure_type_id FK
    uuid traffic_agency_id FK
    text reference_number
    text state
    jsonb config_snapshot "inmutable"
    uuid assigned_to_user_id FK
    numeric total_amount
  }
  procedure_field_values {
    uuid id PK
    uuid procedure_instance_id FK
    text edge_role
    text field_key
    jsonb value
  }
  procedure_actors {
    uuid id PK
    uuid procedure_instance_id FK
    text edge_role
    text person_kind "natural|juridica"
    uuid document_type_id FK
    text document_number "pii:high"
  }
  procedure_actor_representatives {
    uuid id PK
    uuid procedure_actor_id FK
    uuid document_type_id FK
    text document_number "pii:high"
  }
  procedure_vehicles {
    uuid id PK
    uuid procedure_instance_id FK
    text vehicle_subkind
    text license_plate "nullable"
    text vin "nullable"
    jsonb runt_snapshot
  }
  procedure_query_results {
    uuid id PK
    uuid procedure_instance_id FK
    text query_connector_code
    text status "ok|failed|partial"
    jsonb result
  }
  procedure_documents {
    uuid id PK
    uuid procedure_instance_id FK
    text kind "upload|auto_generated"
    uuid document_type_id FK
    uuid file_id FK
    uuid template_version_id FK
  }
  procedure_state_history {
    uuid id PK
    uuid procedure_instance_id FK
    text from_state
    text to_state
    uuid changed_by FK
  }
  procedure_identity_validations {
    uuid id PK
    uuid procedure_instance_id FK
    uuid verification_session_id FK
    uuid actor_id FK
    text verdict
  }
  verification_sessions {
    uuid id PK
    uuid tenant_id FK
    text subject_document_number "pii:high"
    text status
    text verdict
  }
```

## 2. Identidad, compañías y OT

```mermaid
erDiagram
  tenants ||--o{ users : ""
  tenants ||--o| companies : "1:1"
  users ||--o| profiles : "1:1"
  roles ||--o{ role_permissions : ""
  permissions ||--o{ role_permissions : ""
  users ||--o{ user_roles : ""
  roles ||--o{ user_roles : ""
  tenants ||--o{ onboarding_invitations : ""
  roles ||--o{ onboarding_invitations : "invited_role"
  tenants ||--o{ password_policies : "1:1"
  tenants ||--o{ refresh_tokens : ""
  tenants ||--o{ support_tickets : ""

  companies ||--o{ company_module_configs : ""
  companies ||--o| signature_wallets : "1:1"
  signature_wallets ||--o{ signature_wallet_movements : "ledger"
  companies ||--o{ vehicle_ownership_rules : ""
  companies ||--o{ escrituras : ""
  escrituras ||--o{ escritura_attachments : "PDF max 5"
  document_types ||--o{ escrituras : ""

  traffic_agencies ||--o{ ot_users : ""
  ot_users ||--o{ ot_user_permissions : ""
  traffic_agencies ||--o{ ot_rules : ""
  traffic_agencies ||--o| ot_consolidated_doc_orders : "1 activo"
  ot_consolidated_doc_orders ||--o{ ot_consolidated_doc_order_items : ""
  document_types ||--o{ ot_consolidated_doc_order_items : "global"
  traffic_agencies ||--o| ot_qx_integrations : ""

  tenants {
    uuid id PK
    text nit UK
    text slug UK
    text status
  }
  users {
    uuid id PK
    uuid tenant_id FK
    citext email UK
    text account_state
    timestamptz blocked_until
  }
  permissions {
    uuid id PK
    text slug UK "modulo.x.accion"
    boolean is_system
  }
  roles {
    uuid id PK
    uuid tenant_id FK "null=global"
    text slug
    text scope "global|tenant"
  }
  companies {
    uuid id PK
    uuid tenant_id FK
    text nit UK
    jsonb modules_enabled
  }
  company_module_configs {
    uuid id PK
    uuid tenant_id FK
    text module_key
    jsonb config
  }
  escrituras {
    uuid id PK
    uuid tenant_id FK
    text deed_number
    date expiration_date
  }
  escritura_attachments {
    uuid id PK
    uuid escritura_id FK
    uuid file_id FK
    int position "1..5"
  }
  traffic_agencies {
    uuid id PK
    text code UK
    text runt_agency_code
    char dane_municipality_code
    boolean mandate_document_applies
    jsonb external_refs
  }
  ot_rules {
    uuid id PK
    uuid traffic_agency_id FK
    jsonb condition_tree
    jsonb actions
  }
  ot_consolidated_doc_order_items {
    uuid id PK
    uuid order_id FK
    uuid document_type_id FK
    text custom_label
    int position
    text source "global|custom"
  }
```

## 3. Catálogos, archivos e integraciones (referencias transversales)

```mermaid
erDiagram
  divipola_departments ||--o{ divipola_municipalities : ""
  vehicle_makes ||--o{ vehicle_lines : ""
  files ||--o{ escritura_attachments : ""
  files ||--o{ verification_evidences : ""
  files ||--o{ procedure_documents : ""
  files ||--o{ document_template_versions : "body"
  verification_sessions ||--o{ verification_evidences : ""
  tenants ||--o{ files : ""
  tenants ||--o{ external_query_calls : ""
  external_query_calls ||--o{ procedure_query_results : "raw → snapshot"
  traffic_agencies ||--o{ webhook_events : "QX"

  document_types {
    uuid id PK
    text code UK "CC|NIT|..."
    text default_person_kind
  }
  files {
    uuid id PK
    uuid tenant_id FK
    text bucket
    text object_key
    text status
  }
  verification_evidences {
    uuid id PK
    uuid verification_session_id FK
    uuid file_id FK
    text evidence_type "pii:high"
  }
  external_query_calls {
    uuid id PK
    uuid tenant_id FK
    text query_connector_code
    boolean succeeded
  }
  webhook_events {
    uuid id PK
    uuid tenant_id FK
    uuid traffic_agency_id FK
    text direction
    text idempotency_key UK
  }
```

## 4. Dashboard (read model)

```mermaid
erDiagram
  procedure_instances ||..o{ v_procedure_kpis : "agrega"
  procedure_instances ||..o{ v_user_productivity : "agrega"
  procedure_instances ||..o{ v_ot_distribution : "agrega"
  procedure_instances ||..o{ mv_procedure_kpis : "materializa"

  v_procedure_kpis {
    uuid tenant_id
    uuid traffic_agency_id
    text state
    date day
    bigint total
  }
  mv_procedure_kpis {
    uuid tenant_id
    uuid traffic_agency_id
    text state
    date day
    bigint total
  }
```

> **Resolución de config y snapshot:** un *resolver* aplica precedencia **OT > tenant > global**
> sobre `procedure_type_edges` (matriz) + overrides (`procedure_type_activations.overrides`) +
> `form_*` + `required_documents` + `procedure_type_query_configs` + `rules`, y congela el resultado
> en `procedure_instances.config_snapshot` al radicar (ADR-0010). Por eso el runtime referencia el
> tipo (linaje) pero **no** depende de FKs vivas hacia cada fila de config.
