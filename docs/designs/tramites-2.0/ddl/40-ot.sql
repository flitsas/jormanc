-- =====================================================================================
-- FLIT 2.0 · DDL 40 — Organismos de Tránsito (#9378) + Orden del Consolidado (#9379)
-- OT = referencia cross-tenant (SIN tenant_id, ver ADR-0012). Las tablas de CONFIG OT se
-- aíslan por traffic_agency_id (GUC app.current_agency_id) + bypass SuperAdmin.
-- =====================================================================================
SET search_path TO ot;

-- -------------------------------------------------------------------------------------
-- traffic_agencies — registro OT (estilo catálogo: is_active, sin tenant_id, sin RLS).
-- Semilla completa (~370) se adapta de services/.../SeedData/traffic_secretaries.sql.
-- -------------------------------------------------------------------------------------
CREATE TABLE traffic_agencies (
  id                            uuid        PRIMARY KEY DEFAULT uuidv7(),
  code                          text        NOT NULL,
  name                          text        NOT NULL,
  agency_type                   text        NOT NULL DEFAULT 'Organismos de Tránsito',
  address                       text        NULL,
  phone                         text        NULL,
  department_name               text        NULL,
  municipality_name             text        NULL,
  dane_municipality_code        char(5)     NULL,   -- DIVIPOLA (FK opcional tras carga completa)
  nit                           text        NULL,
  notifier_email                text        NULL,
  contact_name                  text        NULL,
  contact_phone                 text        NULL,
  runt_agency_code              text        NULL,   -- traffic_agency_code RUNT (no único: '0' repetido)
  mandate_document_applies      boolean     NOT NULL DEFAULT false,
  virtual_process_applies       boolean     NOT NULL DEFAULT false,
  requires_peace_and_safe       boolean     NOT NULL DEFAULT false,
  allows_runt_approval_queries  boolean     NOT NULL DEFAULT false,
  requires_preassignment_plate  boolean     NOT NULL DEFAULT false,
  request_issue_date_flag       boolean     NOT NULL DEFAULT false,
  external_refs                 jsonb       NOT NULL DEFAULT '{}'::jsonb,  -- {"parint_transfer":1,"parint_registration":1,"parint_otherservice":1,"divipo":"..."}
  is_active                     boolean     NOT NULL DEFAULT true,
  created_at                    timestamptz NOT NULL DEFAULT now(),
  created_by                    uuid        NULL,
  updated_at                    timestamptz NOT NULL DEFAULT now(),
  updated_by                    uuid        NULL,
  row_version                   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_traffic_agencies_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_traffic_agencies_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_traffic_agencies_code UNIQUE (code)
);
CREATE INDEX ix_traffic_agencies_name_trgm ON traffic_agencies USING gin (name gin_trgm_ops);
CREATE INDEX ix_traffic_agencies_dane_municipality_code ON traffic_agencies (dane_municipality_code);
CREATE INDEX ix_traffic_agencies_is_active ON traffic_agencies (is_active);
CREATE TRIGGER tr_traffic_agencies_before_update_row_version
  BEFORE UPDATE ON traffic_agencies FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_traffic_agencies_audit
  AFTER INSERT OR UPDATE OR DELETE ON traffic_agencies FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE traffic_agencies IS
  '@context:ot Registro de Organismos de Tránsito (referencia cross-tenant). Sin tenant_id ni RLS (ADR-0012).';
COMMENT ON COLUMN traffic_agencies.notifier_email IS '@pii:medium';

INSERT INTO traffic_agencies (code, name, department_name, municipality_name, dane_municipality_code, nit,
  notifier_email, runt_agency_code, mandate_document_applies, virtual_process_applies, requires_preassignment_plate,
  requires_peace_and_safe, allows_runt_approval_queries, external_refs, created_by) VALUES
  ('OT-BUCARAMANGA','DIR TTOyTTE BUCARAMANGA','SANTANDER','BUCARAMANGA','68001','890201222',
   'notificaciones@bucaramanga.gov.co','68001000', false,false,false,false,false,
   '{"parint_transfer":1,"parint_registration":1,"parint_otherservice":1,"divipo":"68"}'::jsonb,'00000000-0000-7000-8000-000000000001'),
  ('OT-BARBOSA','DIR TTEyTTO MCPAL BARBOSA','ANTIOQUIA','BARBOSA','05079','890980445',
   'asistenteadmsatt@gmail.com','5079000', true,true,true,false,false,
   '{"parint_transfer":1,"parint_registration":1,"parint_otherservice":1,"divipo":"05"}'::jsonb,'00000000-0000-7000-8000-000000000001'),
  ('OT-CARTAGENA','DPTO ADTVO TTOyTTE DIST CARTAGENA','BOLIVAR','CARTAGENA','13001','890480184',
   'notificacionesjudicialesadministrativo@cartagena.gov.co','13001000', false,false,false,false,false,
   '{"parint_transfer":1,"parint_registration":1,"parint_otherservice":1,"divipo":"13"}'::jsonb,'00000000-0000-7000-8000-000000000001'),
  ('OT-PASTO','DPTO ADTVO TTOYTTE MCPAL PASTO','NARIÑO','PASTO','52001','8912800003',
   'contactenos@pasto.gov.co','52001000', false,false,false,false,false,
   '{"parint_transfer":1,"parint_registration":1,"parint_otherservice":1}'::jsonb,'00000000-0000-7000-8000-000000000001');

-- -------------------------------------------------------------------------------------
-- ot_users — operadores de la consola OT (#9378 vistas base: usuarios y permisos)
-- -------------------------------------------------------------------------------------
CREATE TABLE ot_users (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  traffic_agency_id uuid        NOT NULL,
  email             citext      NOT NULL,
  full_name         text        NOT NULL,
  external_auth_ref text        NULL,
  is_active         boolean     NOT NULL DEFAULT true,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  deleted_at        timestamptz NULL,
  deleted_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_ot_users_traffic_agencies FOREIGN KEY (traffic_agency_id) REFERENCES traffic_agencies (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ot_users_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_ot_users_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_ot_users_agency_email UNIQUE (traffic_agency_id, email)
);
CREATE INDEX ix_ot_users_traffic_agency_id ON ot_users (traffic_agency_id) WHERE deleted_at IS NULL;
ALTER TABLE ot_users ENABLE ROW LEVEL SECURITY;
CREATE POLICY agency_isolation ON ot_users
  USING (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin());
CREATE TRIGGER tr_ot_users_before_update_row_version
  BEFORE UPDATE ON ot_users FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_ot_users_audit
  AFTER INSERT OR UPDATE OR DELETE ON ot_users FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN ot_users.email IS '@pii:medium';

-- -------------------------------------------------------------------------------------
-- ot_user_permissions — permisos operativos por usuario OT (traffic_agency_id denormalizado p/ RLS)
-- -------------------------------------------------------------------------------------
CREATE TABLE ot_user_permissions (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  traffic_agency_id uuid        NOT NULL,
  ot_user_id        uuid        NOT NULL,
  permission_slug   text        NOT NULL,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_ot_user_permissions_traffic_agencies FOREIGN KEY (traffic_agency_id) REFERENCES traffic_agencies (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ot_user_permissions_ot_users FOREIGN KEY (ot_user_id) REFERENCES ot_users (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ot_user_permissions_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_ot_user_permissions_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_ot_user_permissions_user_slug UNIQUE (ot_user_id, permission_slug)
);
CREATE INDEX ix_ot_user_permissions_traffic_agency_id ON ot_user_permissions (traffic_agency_id, ot_user_id);
ALTER TABLE ot_user_permissions ENABLE ROW LEVEL SECURITY;
CREATE POLICY agency_isolation ON ot_user_permissions
  USING (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin());
CREATE TRIGGER tr_ot_user_permissions_before_update_row_version
  BEFORE UPDATE ON ot_user_permissions FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();

-- -------------------------------------------------------------------------------------
-- ot_rules — constructor de reglas OT (#9378 parametrización dinámica). Árbol JSONB validado.
-- -------------------------------------------------------------------------------------
CREATE TABLE ot_rules (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  traffic_agency_id uuid        NOT NULL,
  name              text        NOT NULL,
  trigger_event     text        NOT NULL,   -- gatillo de activación (ej. on_submit, on_field_change)
  condition_tree    jsonb       NOT NULL DEFAULT '{}'::jsonb,
  actions           jsonb       NOT NULL DEFAULT '[]'::jsonb,   -- bloqueo, validación adicional, enrutamiento QX
  priority          integer     NOT NULL DEFAULT 100,
  is_active         boolean     NOT NULL DEFAULT true,
  valid_from        timestamptz NULL,
  valid_until       timestamptz NULL,
  schema_version    integer     NOT NULL DEFAULT 1,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  deleted_at        timestamptz NULL,
  deleted_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_ot_rules_traffic_agencies FOREIGN KEY (traffic_agency_id) REFERENCES traffic_agencies (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ot_rules_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_ot_rules_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT ck_ot_rules_condition_valid CHECK (public.is_valid_rule_condition(condition_tree)),
  CONSTRAINT ck_ot_rules_actions_valid   CHECK (public.is_valid_rule_actions(actions))
);
CREATE INDEX ix_ot_rules_traffic_agency_id_active ON ot_rules (traffic_agency_id, is_active) WHERE deleted_at IS NULL;
ALTER TABLE ot_rules ENABLE ROW LEVEL SECURITY;
CREATE POLICY agency_isolation ON ot_rules
  USING (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin());
CREATE TRIGGER tr_ot_rules_before_update_row_version
  BEFORE UPDATE ON ot_rules FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_ot_rules_audit
  AFTER INSERT OR UPDATE OR DELETE ON ot_rules FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- ot_consolidated_doc_orders — orden del expediente consolidado por OT (#9379). 1 activo por OT.
-- -------------------------------------------------------------------------------------
CREATE TABLE ot_consolidated_doc_orders (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  traffic_agency_id uuid        NOT NULL,
  version           integer     NOT NULL DEFAULT 1,
  is_active         boolean     NOT NULL DEFAULT true,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  deleted_at        timestamptz NULL,
  deleted_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_ot_consolidated_doc_orders_traffic_agencies FOREIGN KEY (traffic_agency_id) REFERENCES traffic_agencies (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ot_consolidated_doc_orders_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_ot_consolidated_doc_orders_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE UNIQUE INDEX uq_ot_consolidated_doc_orders_active ON ot_consolidated_doc_orders (traffic_agency_id) WHERE is_active AND deleted_at IS NULL;
ALTER TABLE ot_consolidated_doc_orders ENABLE ROW LEVEL SECURITY;
CREATE POLICY agency_isolation ON ot_consolidated_doc_orders
  USING (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin());
CREATE TRIGGER tr_ot_consolidated_doc_orders_before_update_row_version
  BEFORE UPDATE ON ot_consolidated_doc_orders FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_ot_consolidated_doc_orders_audit
  AFTER INSERT OR UPDATE OR DELETE ON ot_consolidated_doc_orders FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- ot_consolidated_doc_order_items — ítems ordenados (drag&drop). Quitar = borrar fila (no archivos).
-- -------------------------------------------------------------------------------------
CREATE TABLE ot_consolidated_doc_order_items (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  traffic_agency_id uuid        NOT NULL,
  order_id          uuid        NOT NULL,
  document_type_id  uuid        NULL,           -- tipo global (catalogs)
  custom_label      text        NULL,           -- etiqueta personalizada
  position          integer     NOT NULL,
  source            text        NOT NULL CHECK (source IN ('global','custom')),
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_ot_cons_items_traffic_agencies FOREIGN KEY (traffic_agency_id) REFERENCES traffic_agencies (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ot_cons_items_orders FOREIGN KEY (order_id) REFERENCES ot_consolidated_doc_orders (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ot_cons_items_document_types FOREIGN KEY (document_type_id) REFERENCES catalogs.document_types (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_ot_cons_items_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_ot_cons_items_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_ot_cons_items_order_position UNIQUE (order_id, position),
  CONSTRAINT ck_ot_cons_items_source CHECK (
    (source = 'global' AND document_type_id IS NOT NULL)
    OR (source = 'custom' AND custom_label IS NOT NULL)
  )
);
CREATE INDEX ix_ot_cons_items_order_id ON ot_consolidated_doc_order_items (order_id, position);
CREATE INDEX ix_ot_cons_items_document_type_id ON ot_consolidated_doc_order_items (document_type_id);
ALTER TABLE ot_consolidated_doc_order_items ENABLE ROW LEVEL SECURITY;
CREATE POLICY agency_isolation ON ot_consolidated_doc_order_items
  USING (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin());
CREATE TRIGGER tr_ot_cons_items_before_update_row_version
  BEFORE UPDATE ON ot_consolidated_doc_order_items FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
COMMENT ON TABLE ot_consolidated_doc_order_items IS
  '@context:ot Orden del consolidado (#9379). Guardado <500ms: UPDATE de position en transacción corta; índice uq por (order_id, position).';

-- -------------------------------------------------------------------------------------
-- ot_qx_integrations — modo de gestión por OT: Dashboard FLIT vs QX/colas externas (Quipux)
-- -------------------------------------------------------------------------------------
CREATE TABLE ot_qx_integrations (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  traffic_agency_id uuid        NOT NULL,
  mode              text        NOT NULL DEFAULT 'dashboard' CHECK (mode IN ('dashboard','qx')),
  callback_url      text        NULL,
  auth_config       jsonb       NOT NULL DEFAULT '{}'::jsonb,  -- referencia a secreto, no plaintext
  is_active         boolean     NOT NULL DEFAULT true,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  deleted_at        timestamptz NULL,
  deleted_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_ot_qx_integrations_traffic_agencies FOREIGN KEY (traffic_agency_id) REFERENCES traffic_agencies (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ot_qx_integrations_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_ot_qx_integrations_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_ot_qx_integrations_agency UNIQUE (traffic_agency_id)
);
ALTER TABLE ot_qx_integrations ENABLE ROW LEVEL SECURITY;
CREATE POLICY agency_isolation ON ot_qx_integrations
  USING (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (traffic_agency_id = current_setting('app.current_agency_id', true)::uuid OR identity.is_super_admin());
CREATE TRIGGER tr_ot_qx_integrations_before_update_row_version
  BEFORE UPDATE ON ot_qx_integrations FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_ot_qx_integrations_audit
  AFTER INSERT OR UPDATE OR DELETE ON ot_qx_integrations FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS ot.ot_qx_integrations, ot.ot_consolidated_doc_order_items,
--   ot.ot_consolidated_doc_orders, ot.ot_rules, ot.ot_user_permissions, ot.ot_users,
--   ot.traffic_agencies CASCADE;
