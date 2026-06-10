-- =====================================================================================
-- FLIT 2.0 · DDL 50 — NÚCLEO DE PARAMETRIZACIÓN (#9408 #9409 #9410)
-- Maestros GLOBALES (tenant-exentos, RLS read-all / write-SuperAdmin) + capas TENANT-scoped.
-- Familias → Tipos → Aristas → Matriz de conformación → Forms/Documentos/Consultas/Reglas.
-- Seed: 3 familias, 13 tipos, 4 aristas, matriz Tabla 2.3, conectores Tabla 2.4.
-- =====================================================================================
SET search_path TO procedures_config;

-- =====================================================================================
-- A. MAESTROS GLOBALES (sin tenant_id). Patrón RLS: lectura para todos; escritura SuperAdmin.
-- =====================================================================================

-- procedure_families — MATRÍCULAS / TRASPASO / OTROS TRÁMITES (#9409 CF-A1)
CREATE TABLE procedure_families (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          text        NOT NULL,
  name          text        NOT NULL,
  display_order integer     NOT NULL DEFAULT 0,
  is_active     boolean     NOT NULL DEFAULT true,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_procedure_families_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_families_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_procedure_families_code UNIQUE (code)
);
ALTER TABLE procedure_families ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON procedure_families FOR SELECT USING (true);
CREATE POLICY config_modify ON procedure_families FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_procedure_families_before_update_row_version BEFORE UPDATE ON procedure_families FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_procedure_families_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_families FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE procedure_families IS '@context:procedures_config Familias padre de trámites (global, SuperAdmin).';

-- procedure_types — ~13 tipos hijo (#9409 CF-A2). Inactivo → no seleccionable ni accesible por URL.
CREATE TABLE procedure_types (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  family_id     uuid        NOT NULL,
  code          text        NOT NULL,
  slug          text        NOT NULL,
  name          text        NOT NULL,
  description   text        NULL,
  max_steps     integer     NOT NULL DEFAULT 4 CHECK (max_steps BETWEEN 1 AND 4),  -- stepper máx 4 (#9408 FR-3)
  display_order integer     NOT NULL DEFAULT 0,
  is_active     boolean     NOT NULL DEFAULT true,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_procedure_types_families FOREIGN KEY (family_id) REFERENCES procedure_families (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_types_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_types_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_procedure_types_code UNIQUE (code),
  CONSTRAINT uq_procedure_types_slug UNIQUE (slug)
);
CREATE INDEX ix_procedure_types_family_id ON procedure_types (family_id);
CREATE INDEX ix_procedure_types_is_active ON procedure_types (is_active);
ALTER TABLE procedure_types ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON procedure_types FOR SELECT USING (true);
CREATE POLICY config_modify ON procedure_types FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_procedure_types_before_update_row_version BEFORE UPDATE ON procedure_types FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_procedure_types_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_types FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- edges — las 4 aristas (#9409 CF-A5). edge_kind: vehicle | person.
CREATE TABLE edges (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          text        NOT NULL CHECK (code IN ('vehiculo','propietario','comprador','locatario')),
  name          text        NOT NULL,
  edge_kind     text        NOT NULL CHECK (edge_kind IN ('vehicle','person')),
  display_order integer     NOT NULL DEFAULT 0,
  is_active     boolean     NOT NULL DEFAULT true,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_edges_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_edges_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_edges_code UNIQUE (code)
);
ALTER TABLE edges ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON edges FOR SELECT USING (true);
CREATE POLICY config_modify ON edges FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_edges_before_update_row_version BEFORE UPDATE ON edges FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_edges_audit AFTER INSERT OR UPDATE OR DELETE ON edges FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- procedure_type_edges — MATRIZ DE CONFORMACIÓN (#9409 CF-A6). Auditable (who/when/before/after).
CREATE TABLE procedure_type_edges (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  procedure_type_id uuid        NOT NULL,
  edge_id           uuid        NOT NULL,
  is_active         boolean     NOT NULL DEFAULT true,  -- arista desactivada → no renderiza ni valida
  is_required       boolean     NOT NULL DEFAULT true,
  display_order     integer     NOT NULL DEFAULT 0,
  role_label        text        NULL,                   -- ej. "Vendedor", "Comprador"
  config            jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_procedure_type_edges_types FOREIGN KEY (procedure_type_id) REFERENCES procedure_types (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_type_edges_edges FOREIGN KEY (edge_id) REFERENCES edges (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_type_edges_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_type_edges_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_procedure_type_edges_type_edge UNIQUE (procedure_type_id, edge_id)  -- destino de FKs compuestas (overrides por arista)
);
CREATE INDEX ix_procedure_type_edges_procedure_type_id ON procedure_type_edges (procedure_type_id);
CREATE INDEX ix_procedure_type_edges_edge_id ON procedure_type_edges (edge_id);
ALTER TABLE procedure_type_edges ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON procedure_type_edges FOR SELECT USING (true);
CREATE POLICY config_modify ON procedure_type_edges FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_procedure_type_edges_before_update_row_version BEFORE UPDATE ON procedure_type_edges FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_procedure_type_edges_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_type_edges FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE procedure_type_edges IS '@context:procedures_config MATRIZ trámite×arista. La fila ausente = arista no aplica.';

-- form_sections — secciones por tipo (y opcional por arista vía matriz). edge_id NULL = nivel tipo.
CREATE TABLE form_sections (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  procedure_type_id uuid        NOT NULL,
  edge_id           uuid        NULL,
  section_key       text        NOT NULL,
  title             text        NOT NULL,
  display_order     integer     NOT NULL DEFAULT 0,
  ui_mode           text        NOT NULL DEFAULT 'interactive' CHECK (ui_mode IN ('read_only','interactive')),
  is_active         boolean     NOT NULL DEFAULT true,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_form_sections_types FOREIGN KEY (procedure_type_id) REFERENCES procedure_types (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_form_sections_matrix FOREIGN KEY (procedure_type_id, edge_id) REFERENCES procedure_type_edges (procedure_type_id, edge_id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_form_sections_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_form_sections_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_form_sections_type_key UNIQUE (procedure_type_id, section_key)
);
CREATE INDEX ix_form_sections_procedure_type_id ON form_sections (procedure_type_id);
CREATE INDEX ix_form_sections_procedure_type_id_edge_id ON form_sections (procedure_type_id, edge_id);
ALTER TABLE form_sections ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON form_sections FOR SELECT USING (true);
CREATE POLICY config_modify ON form_sections FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_form_sections_before_update_row_version BEFORE UPDATE ON form_sections FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_form_sections_audit AFTER INSERT OR UPDATE OR DELETE ON form_sections FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- form_fields — campos por sección (#9409 CF-B6: tipo, label, obligatoriedad, orden, 4 estados UI).
CREATE TABLE form_fields (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  section_id    uuid        NOT NULL,
  field_key     text        NOT NULL,
  data_type     text        NOT NULL CHECK (data_type IN ('text','number','date','boolean','select','multiselect','file')),
  label         text        NOT NULL,
  is_required   boolean     NOT NULL DEFAULT false,
  display_order integer     NOT NULL DEFAULT 0,
  ui_state      text        NOT NULL DEFAULT 'lleno' CHECK (ui_state IN ('vacio','cargando','error','lleno')),
  is_trigger    boolean     NOT NULL DEFAULT false,  -- campo desencadenante de consultas (#9408 FR-4)
  validation    jsonb       NOT NULL DEFAULT '{}'::jsonb,
  options       jsonb       NOT NULL DEFAULT '[]'::jsonb,
  is_active     boolean     NOT NULL DEFAULT true,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_form_fields_sections FOREIGN KEY (section_id) REFERENCES form_sections (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_form_fields_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_form_fields_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_form_fields_section_key UNIQUE (section_id, field_key)
);
CREATE INDEX ix_form_fields_section_id ON form_fields (section_id);
ALTER TABLE form_fields ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON form_fields FOR SELECT USING (true);
CREATE POLICY config_modify ON form_fields FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_form_fields_before_update_row_version BEFORE UPDATE ON form_fields FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_form_fields_audit AFTER INSERT OR UPDATE OR DELETE ON form_fields FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- query_connectors — catálogo de conectores externos (#9409 Tabla 2.4): RUNT/SIMIT/RNMC/RESOLUCIONES/RUES/FASECOLDA
CREATE TABLE query_connectors (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code          text        NOT NULL,
  name          text        NOT NULL,
  base_config   jsonb       NOT NULL DEFAULT '{}'::jsonb,  -- endpoints/timeouts; secretos por referencia
  is_active     boolean     NOT NULL DEFAULT true,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_query_connectors_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_query_connectors_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_query_connectors_code UNIQUE (code)
);
ALTER TABLE query_connectors ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON query_connectors FOR SELECT USING (true);
CREATE POLICY config_modify ON query_connectors FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_query_connectors_before_update_row_version BEFORE UPDATE ON query_connectors FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_query_connectors_audit AFTER INSERT OR UPDATE OR DELETE ON query_connectors FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- procedure_type_query_configs — consulta por tipo/arista + enrutamiento (#9409 Tabla 2.4, FR-2/FR-3)
CREATE TABLE procedure_type_query_configs (
  id                 uuid        PRIMARY KEY DEFAULT uuidv7(),
  procedure_type_id  uuid        NOT NULL,
  edge_id            uuid        NULL,
  query_connector_id uuid        NOT NULL,
  is_mandatory       boolean     NOT NULL DEFAULT false,
  is_omitible        boolean     NOT NULL DEFAULT false,  -- firma/RTM omitible (Locatario/Leasing FR-3)
  person_kind_filter text        NOT NULL DEFAULT 'any' CHECK (person_kind_filter IN ('natural','juridica','any')),
  run_condition      jsonb       NOT NULL DEFAULT '{}'::jsonb,
  display_order      integer     NOT NULL DEFAULT 0,
  is_active          boolean     NOT NULL DEFAULT true,
  created_at         timestamptz NOT NULL DEFAULT now(),
  created_by         uuid        NULL,
  updated_at         timestamptz NOT NULL DEFAULT now(),
  updated_by         uuid        NULL,
  row_version        integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_pt_query_configs_types FOREIGN KEY (procedure_type_id) REFERENCES procedure_types (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_pt_query_configs_matrix FOREIGN KEY (procedure_type_id, edge_id) REFERENCES procedure_type_edges (procedure_type_id, edge_id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_pt_query_configs_connectors FOREIGN KEY (query_connector_id) REFERENCES query_connectors (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_pt_query_configs_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_pt_query_configs_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_pt_query_configs UNIQUE (procedure_type_id, edge_id, query_connector_id, person_kind_filter)
);
CREATE INDEX ix_pt_query_configs_procedure_type_id ON procedure_type_query_configs (procedure_type_id);
CREATE INDEX ix_pt_query_configs_connector_id ON procedure_type_query_configs (query_connector_id);
ALTER TABLE procedure_type_query_configs ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON procedure_type_query_configs FOR SELECT USING (true);
CREATE POLICY config_modify ON procedure_type_query_configs FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_pt_query_configs_before_update_row_version BEFORE UPDATE ON procedure_type_query_configs FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_pt_query_configs_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_type_query_configs FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- document_templates + versions — plantillas versionadas (#9408/5.x reglas-estándar; doc generación)
CREATE TABLE document_templates (
  id               uuid        PRIMARY KEY DEFAULT uuidv7(),
  code             text        NOT NULL,
  name             text        NOT NULL,
  document_type_id uuid        NOT NULL,
  scope            text        NOT NULL DEFAULT 'global' CHECK (scope IN ('global','tenant')),
  tenant_id        uuid        NULL,            -- requerido si scope='tenant'
  current_version  integer     NOT NULL DEFAULT 0,
  is_active        boolean     NOT NULL DEFAULT true,
  created_at       timestamptz NOT NULL DEFAULT now(),
  created_by       uuid        NULL,
  updated_at       timestamptz NOT NULL DEFAULT now(),
  updated_by       uuid        NULL,
  row_version      integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_document_templates_document_types FOREIGN KEY (document_type_id) REFERENCES catalogs.document_types (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_document_templates_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_document_templates_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_document_templates_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_document_templates_code UNIQUE (code),
  CONSTRAINT ck_document_templates_scope_tenant CHECK (
    (scope = 'global' AND tenant_id IS NULL) OR (scope = 'tenant' AND tenant_id IS NOT NULL)
  )
);
CREATE INDEX ix_document_templates_document_type_id ON document_templates (document_type_id);
CREATE INDEX ix_document_templates_tenant_id ON document_templates (tenant_id);
ALTER TABLE document_templates ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON document_templates FOR SELECT USING (tenant_id IS NULL OR tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin());
CREATE POLICY config_modify ON document_templates FOR ALL USING (identity.is_super_admin() OR tenant_id = current_setting('app.current_tenant_id', true)::uuid) WITH CHECK (identity.is_super_admin() OR tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_document_templates_before_update_row_version BEFORE UPDATE ON document_templates FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_document_templates_audit AFTER INSERT OR UPDATE OR DELETE ON document_templates FOR EACH ROW EXECUTE FUNCTION audit.log_change();

CREATE TABLE document_template_versions (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  template_id   uuid        NOT NULL,
  version       integer     NOT NULL,
  body_file_id  uuid        NULL,                 -- plantilla (HTML/Word/PDF) en object storage
  body_inline   text        NULL,                 -- o cuerpo embebido (HTML con marcadores)
  marker_map    jsonb       NOT NULL DEFAULT '{}'::jsonb,  -- marcador → {source, path} (tabla_principal/actores/consulta/validacion_id)
  is_current    boolean     NOT NULL DEFAULT false,
  published_at  timestamptz NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_document_template_versions_templates FOREIGN KEY (template_id) REFERENCES document_templates (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_document_template_versions_files FOREIGN KEY (body_file_id) REFERENCES files.files (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_document_template_versions_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_document_template_versions_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_document_template_versions_template_version UNIQUE (template_id, version),
  CONSTRAINT ck_document_template_versions_body CHECK (body_file_id IS NOT NULL OR body_inline IS NOT NULL)
);
CREATE INDEX ix_document_template_versions_template_id ON document_template_versions (template_id);
ALTER TABLE document_template_versions ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON document_template_versions FOR SELECT USING (true);
CREATE POLICY config_modify ON document_template_versions FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_document_template_versions_before_update_row_version BEFORE UPDATE ON document_template_versions FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_document_template_versions_audit AFTER INSERT OR UPDATE OR DELETE ON document_template_versions FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE document_template_versions IS '@context:procedures_config Versión inmutable una vez publicada (radicado conserva su versión).';

-- required_documents — documentos por tipo/arista (#9409 FR-4 CF-F1). carga vs generación.
CREATE TABLE required_documents (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  procedure_type_id uuid        NOT NULL,
  edge_id           uuid        NULL,
  document_type_id  uuid        NOT NULL,
  kind              text        NOT NULL CHECK (kind IN ('upload','auto_generated')),
  is_required       boolean     NOT NULL DEFAULT true,
  display_order     integer     NOT NULL DEFAULT 0,
  template_id       uuid        NULL,
  actor_role        text        NULL,
  allowed_formats   jsonb       NOT NULL DEFAULT '["pdf"]'::jsonb,
  max_size_mb       integer     NULL,
  is_active         boolean     NOT NULL DEFAULT true,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_required_documents_types FOREIGN KEY (procedure_type_id) REFERENCES procedure_types (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_required_documents_matrix FOREIGN KEY (procedure_type_id, edge_id) REFERENCES procedure_type_edges (procedure_type_id, edge_id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_required_documents_document_types FOREIGN KEY (document_type_id) REFERENCES catalogs.document_types (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_required_documents_templates FOREIGN KEY (template_id) REFERENCES document_templates (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_required_documents_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_required_documents_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT ck_required_documents_template_when_auto CHECK (kind <> 'auto_generated' OR template_id IS NOT NULL)
);
CREATE INDEX ix_required_documents_procedure_type_id ON required_documents (procedure_type_id);
CREATE INDEX ix_required_documents_document_type_id ON required_documents (document_type_id);
CREATE INDEX ix_required_documents_template_id ON required_documents (template_id);
ALTER TABLE required_documents ENABLE ROW LEVEL SECURITY;
CREATE POLICY config_read   ON required_documents FOR SELECT USING (true);
CREATE POLICY config_modify ON required_documents FOR ALL USING (identity.is_super_admin()) WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_required_documents_before_update_row_version BEFORE UPDATE ON required_documents FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_required_documents_audit AFTER INSERT OR UPDATE OR DELETE ON required_documents FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- =====================================================================================
-- B. CAPAS TENANT-SCOPED (tenant_id + RLS). Activaciones / Reglas / Endpoints.
-- =====================================================================================

-- procedure_type_activations — activación/override por tenant (y opcional por OT) (#9409 CF-A3)
CREATE TABLE procedure_type_activations (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id         uuid        NOT NULL,
  procedure_type_id uuid        NOT NULL,
  traffic_agency_id uuid        NULL,
  is_active         boolean     NOT NULL DEFAULT true,
  overrides         jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  deleted_at        timestamptz NULL,
  deleted_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_pt_activations_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_pt_activations_types FOREIGN KEY (procedure_type_id) REFERENCES procedure_types (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_pt_activations_traffic_agencies FOREIGN KEY (traffic_agency_id) REFERENCES ot.traffic_agencies (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_pt_activations_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_pt_activations_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
-- Unicidad por (tipo, tenant, OT) tratando OT NULL como sentinela
CREATE UNIQUE INDEX uq_pt_activations_type_tenant_agency
  ON procedure_type_activations (procedure_type_id, tenant_id, COALESCE(traffic_agency_id, '00000000-0000-7000-8000-000000000000'))
  WHERE deleted_at IS NULL;
CREATE INDEX ix_pt_activations_tenant_id ON procedure_type_activations (tenant_id);
CREATE INDEX ix_pt_activations_traffic_agency_id ON procedure_type_activations (traffic_agency_id);
ALTER TABLE procedure_type_activations ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_type_activations
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_pt_activations_before_update_row_version BEFORE UPDATE ON procedure_type_activations FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_pt_activations_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_type_activations FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- endpoint_catalog — catálogo de endpoints para acción call_endpoint (#9410 FR-4)
CREATE TABLE endpoint_catalog (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NOT NULL,
  code          text        NOT NULL,
  name          text        NOT NULL,
  url           text        NOT NULL,
  method        text        NOT NULL DEFAULT 'GET' CHECK (method IN ('GET','POST')),
  auth_type     text        NOT NULL DEFAULT 'none' CHECK (auth_type IN ('none','api_key','bearer','basic')),
  auth_config   jsonb       NOT NULL DEFAULT '{}'::jsonb,  -- referencia a secreto (vault key), nunca plaintext
  timeout_ms    integer     NOT NULL DEFAULT 5000,
  is_active     boolean     NOT NULL DEFAULT true,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NOT NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NOT NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_endpoint_catalog_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_endpoint_catalog_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_endpoint_catalog_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_endpoint_catalog_tenant_code UNIQUE (tenant_id, code)
);
CREATE INDEX ix_endpoint_catalog_tenant_id ON endpoint_catalog (tenant_id) WHERE deleted_at IS NULL;
ALTER TABLE endpoint_catalog ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON endpoint_catalog
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_endpoint_catalog_before_update_row_version BEFORE UPDATE ON endpoint_catalog FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_endpoint_catalog_audit AFTER INSERT OR UPDATE OR DELETE ON endpoint_catalog FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN endpoint_catalog.auth_config IS '@pii:none Solo referencia a secreto (clave de vault). Nunca credenciales en claro.';

-- rules — motor de reglas no-code por tipo+tenant (#9410). Árbol JSONB validado por BD.
CREATE TABLE rules (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id         uuid        NOT NULL,
  procedure_type_id uuid        NOT NULL,
  name              text        NOT NULL,
  description       text        NULL,
  condition_tree    jsonb       NOT NULL DEFAULT '{}'::jsonb,
  actions           jsonb       NOT NULL DEFAULT '[]'::jsonb,
  priority          integer     NOT NULL DEFAULT 100,
  is_active         boolean     NOT NULL DEFAULT true,  -- OFF → no evalúa (#9410 CF-B6)
  schema_version    integer     NOT NULL DEFAULT 1,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  deleted_at        timestamptz NULL,
  deleted_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_rules_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_rules_types FOREIGN KEY (procedure_type_id) REFERENCES procedure_types (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_rules_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_rules_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_rules_tenant_type_name UNIQUE (tenant_id, procedure_type_id, name),
  CONSTRAINT ck_rules_condition_valid CHECK (public.is_valid_rule_condition(condition_tree)),
  CONSTRAINT ck_rules_actions_valid   CHECK (public.is_valid_rule_actions(actions))
);
CREATE INDEX ix_rules_tenant_id_type ON rules (tenant_id, procedure_type_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_rules_tenant_id_active_priority ON rules (tenant_id, is_active, priority);
ALTER TABLE rules ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON rules
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_rules_before_update_row_version BEFORE UPDATE ON rules FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_rules_audit AFTER INSERT OR UPDATE OR DELETE ON rules FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- =====================================================================================
-- C. SEED — Familias (3), Tipos (13), Aristas (4), Matriz Tabla 2.3, Conectores (6)
-- =====================================================================================
DO $$
DECLARE v_sys uuid := '00000000-0000-7000-8000-000000000001';
BEGIN
  -- Familias
  INSERT INTO procedure_families (id, code, name, display_order, created_by, updated_by) VALUES
    ('00000000-0000-7000-8001-000000000001','MATRICULAS','Matrículas',1,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000002','TRASPASO','Traspaso',2,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000003','OTROS_TRAMITES','Otros trámites',3,v_sys,v_sys);

  -- Aristas
  INSERT INTO edges (id, code, name, edge_kind, display_order, created_by, updated_by) VALUES
    ('00000000-0000-7000-8002-000000000001','vehiculo','Vehículo','vehicle',1,v_sys,v_sys),
    ('00000000-0000-7000-8002-000000000002','propietario','Propietario / Vendedor','person',2,v_sys,v_sys),
    ('00000000-0000-7000-8002-000000000003','comprador','Comprador','person',3,v_sys,v_sys),
    ('00000000-0000-7000-8002-000000000004','locatario','Locatario','person',4,v_sys,v_sys);

  -- Tipos (13)
  INSERT INTO procedure_types (family_id, code, slug, name, display_order, created_by, updated_by) VALUES
    ('00000000-0000-7000-8001-000000000001','MAT_ESTANDAR','matricula-estandar','Matrícula Estándar',1,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000001','MAT_LEASING','matricula-leasing','Matrícula Leasing',2,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000002','TRA_ESTANDAR','traspaso-estandar','Traspaso Estándar',3,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000002','TRA_UNILATERAL','traspaso-unilateral','Traspaso Unilateral',4,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000002','TRA_DOMINIO','transferencia-dominio','Transferencia de Dominio',5,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000003','BLINDAJE','blindaje','Blindaje',6,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000003','CAMBIO_CARROCERIA','cambio-carroceria','Cambio de Carrocería',7,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000003','CAMBIO_COLOR','cambio-color','Cambio de Color',8,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000003','CAMBIO_COMBUSTIBLE','cambio-combustible','Cambio de Combustible',9,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000003','INSCRIPCION_PRENDA','inscripcion-prenda','Inscripción de Prenda',10,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000003','LEVANTAMIENTO_PRENDA','levantamiento-prenda','Levantamiento de Prenda',11,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000003','RADICADO_CUENTA','radicado-cuenta','Radicado de Cuenta',12,v_sys,v_sys),
    ('00000000-0000-7000-8001-000000000003','TRASLADO_CUENTA','traslado-cuenta','Traslado de Cuenta',13,v_sys,v_sys);

  -- Matriz de conformación (Tabla 2.3 + defaults razonables para los demás tipos)
  INSERT INTO procedure_type_edges (procedure_type_id, edge_id, is_required, display_order, role_label, created_by, updated_by)
  SELECT t.id, e.id, m.is_required, m.display_order, m.role_label, v_sys, v_sys
  FROM (VALUES
    -- tipo_code, arista_code, requerido, orden, etiqueta
    ('MAT_ESTANDAR','vehiculo',true,1,'Vehículo'),
    ('MAT_ESTANDAR','propietario',true,2,'Propietario'),
    ('MAT_LEASING','vehiculo',true,1,'Vehículo'),
    ('MAT_LEASING','propietario',true,2,'Propietario'),
    ('MAT_LEASING','locatario',true,3,'Locatario'),
    ('TRA_ESTANDAR','vehiculo',true,1,'Vehículo'),
    ('TRA_ESTANDAR','propietario',true,2,'Vendedor'),
    ('TRA_ESTANDAR','comprador',true,3,'Comprador'),
    ('TRA_UNILATERAL','vehiculo',true,1,'Vehículo'),
    ('TRA_UNILATERAL','propietario',true,2,'Vendedor'),
    ('TRA_UNILATERAL','locatario',true,3,'Locatario'),
    ('TRA_DOMINIO','vehiculo',true,1,'Vehículo'),
    ('TRA_DOMINIO','propietario',true,2,'Vendedor'),
    ('TRA_DOMINIO','comprador',true,3,'Comprador'),
    ('BLINDAJE','vehiculo',true,1,'Vehículo'),
    ('BLINDAJE','propietario',true,2,'Propietario'),
    ('CAMBIO_CARROCERIA','vehiculo',true,1,'Vehículo'),
    ('CAMBIO_CARROCERIA','propietario',true,2,'Propietario'),
    ('CAMBIO_COLOR','vehiculo',true,1,'Vehículo'),
    ('CAMBIO_COLOR','propietario',true,2,'Propietario'),
    ('CAMBIO_COMBUSTIBLE','vehiculo',true,1,'Vehículo'),
    ('CAMBIO_COMBUSTIBLE','propietario',true,2,'Propietario'),
    ('INSCRIPCION_PRENDA','vehiculo',true,1,'Vehículo'),
    ('INSCRIPCION_PRENDA','propietario',true,2,'Propietario'),
    ('LEVANTAMIENTO_PRENDA','vehiculo',true,1,'Vehículo'),
    ('LEVANTAMIENTO_PRENDA','propietario',true,2,'Propietario'),
    ('RADICADO_CUENTA','vehiculo',true,1,'Vehículo'),
    ('RADICADO_CUENTA','propietario',true,2,'Propietario'),
    ('TRASLADO_CUENTA','vehiculo',true,1,'Vehículo'),
    ('TRASLADO_CUENTA','propietario',true,2,'Propietario')
  ) AS m(type_code, edge_code, is_required, display_order, role_label)
  JOIN procedure_types t ON t.code = m.type_code
  JOIN edges e ON e.code = m.edge_code;

  -- Conectores externos (Tabla 2.4)
  INSERT INTO query_connectors (code, name, created_by, updated_by) VALUES
    ('RUNT','Registro Único Nacional de Tránsito',v_sys,v_sys),
    ('SIMIT','Sistema Integrado de Multas y Sanciones',v_sys,v_sys),
    ('RNMC','Registro Nacional de Medidas Correctivas',v_sys,v_sys),
    ('RESOLUCIONES','Resoluciones / Normatividad',v_sys,v_sys),
    ('RUES','Registro Único Empresarial y Social',v_sys,v_sys),
    ('FASECOLDA','FASECOLDA (técnicos/avalúo/pólizas)',v_sys,v_sys);

  -- Consultas por tipo/arista (muestra: Traspaso Estándar — enrutamiento Tabla 2.4)
  INSERT INTO procedure_type_query_configs (procedure_type_id, edge_id, query_connector_id, is_mandatory, person_kind_filter, display_order, created_by, updated_by)
  SELECT t.id, e.id, qc.id, q.is_mandatory, q.person_kind_filter, q.display_order, v_sys, v_sys
  FROM (VALUES
    ('TRA_ESTANDAR','vehiculo','RUNT',true,'any',1),
    ('TRA_ESTANDAR','vehiculo','FASECOLDA',false,'any',2),
    ('TRA_ESTANDAR','propietario','RUNT',true,'natural',1),
    ('TRA_ESTANDAR','propietario','SIMIT',true,'natural',2),
    ('TRA_ESTANDAR','propietario','RUES',true,'juridica',3),
    ('TRA_ESTANDAR','comprador','RUNT',true,'natural',1),
    ('TRA_ESTANDAR','comprador','SIMIT',true,'natural',2),
    ('TRA_ESTANDAR','comprador','RUES',true,'juridica',3),
    ('MAT_LEASING','locatario','SIMIT',true,'any',1)
  ) AS q(type_code, edge_code, connector_code, is_mandatory, person_kind_filter, display_order)
  JOIN procedure_types t ON t.code = q.type_code
  JOIN edges e ON e.code = q.edge_code
  JOIN procedure_type_edges m ON m.procedure_type_id = t.id AND m.edge_id = e.id  -- garantiza arista en matriz
  JOIN query_connectors qc ON qc.code = q.connector_code;
END $$;

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS procedures_config.rules, procedures_config.endpoint_catalog,
--   procedures_config.procedure_type_activations, procedures_config.required_documents,
--   procedures_config.document_template_versions, procedures_config.document_templates,
--   procedures_config.procedure_type_query_configs, procedures_config.query_connectors,
--   procedures_config.form_fields, procedures_config.form_sections,
--   procedures_config.procedure_type_edges, procedures_config.edges,
--   procedures_config.procedure_types, procedures_config.procedure_families CASCADE;
