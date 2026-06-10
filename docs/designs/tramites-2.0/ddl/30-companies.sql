-- =====================================================================================
-- FLIT 2.0 · DDL 30 — Administrador de Compañías B2B (#9381) + Escrituras (#9383)
-- Compañía = 1:1 con tenant. Config modular hot-reload por module_key (jsonb).
--
-- Inventario (schema companies) — HU de consumo en ADO:
-- | Tabla                      | Feature   | HU migración / uso principal        |
-- |----------------------------|-----------|-------------------------------------|
-- | companies                  | #9381     | #9444 crear · #9445–#9449 leer    |
-- | company_module_configs     | #9381     | #9444 crear · #9446/#9447/#9449   |
-- | signature_wallets          | #9381     | #9444 crear · #9449 leer          |
-- | signature_wallet_movements | #9381     | #9444 crear · runtime firma trámite |
-- | vehicle_ownership_rules    | #9381     | #9444 crear · #9448 leer/escribir |
-- | escrituras                 | #9383     | #9450 crear · #9451/#9452 CRUD    |
-- | escritura_attachments      | #9383     | #9450 crear · #9451 adjuntos PDF  |
-- integrations.runt_sync_log → ddl/70-integrations.sql (#9447)
-- =====================================================================================
SET search_path TO companies;

-- -------------------------------------------------------------------------------------
-- companies — maestro de compañía (1:1 con identity.tenants)
-- -------------------------------------------------------------------------------------
CREATE TABLE companies (
  id              uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id       uuid        NOT NULL,
  nit             text        NOT NULL,
  legal_name      text        NOT NULL,
  commercial_name text        NULL,
  modules_enabled jsonb       NOT NULL DEFAULT '{}'::jsonb,  -- {"escrituras": true, "traspasos": true}
  created_at      timestamptz NOT NULL DEFAULT now(),
  created_by      uuid        NOT NULL,
  updated_at      timestamptz NOT NULL DEFAULT now(),
  updated_by      uuid        NOT NULL,
  deleted_at      timestamptz NULL,
  deleted_by      uuid        NULL,
  row_version     integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_companies_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_companies_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_companies_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_companies_tenant UNIQUE (tenant_id),
  CONSTRAINT uq_companies_nit UNIQUE (nit)
);
CREATE INDEX ix_companies_tenant_id ON companies (tenant_id);
CREATE INDEX ix_companies_nit_trgm ON companies USING gin (nit gin_trgm_ops);          -- filtro por NIT (#9381)
CREATE INDEX ix_companies_legal_name_trgm ON companies USING gin (legal_name gin_trgm_ops); -- filtro por Nombre
ALTER TABLE companies ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON companies
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_companies_before_update_row_version
  BEFORE UPDATE ON companies FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_companies_audit
  AFTER INSERT OR UPDATE OR DELETE ON companies FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN companies.modules_enabled IS '@semi-structured Flags de módulos activos (controla visibilidad de pestañas como Escrituras #9383).';

-- -------------------------------------------------------------------------------------
-- company_module_configs — config modular hot-reload, 1 fila por módulo (CF hot-reload #9381)
-- -------------------------------------------------------------------------------------
CREATE TABLE company_module_configs (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NOT NULL,
  module_key    text        NOT NULL
                            CHECK (module_key IN ('registration','transfers','company','runt_contingency')),
  config        jsonb       NOT NULL DEFAULT '{}'::jsonb,
  is_active     boolean     NOT NULL DEFAULT true,
  version       integer     NOT NULL DEFAULT 1,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NOT NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NOT NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_company_module_configs_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_company_module_configs_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_company_module_configs_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_company_module_configs_tenant_module UNIQUE (tenant_id, module_key)
);
CREATE INDEX ix_company_module_configs_tenant_id_module ON company_module_configs (tenant_id, module_key);
ALTER TABLE company_module_configs ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON company_module_configs
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_company_module_configs_before_update_row_version
  BEFORE UPDATE ON company_module_configs FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_company_module_configs_audit
  AFTER INSERT OR UPDATE OR DELETE ON company_module_configs FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE company_module_configs IS
  '@context:companies Config por módulo: registration | transfers | company (firmas/correos/notif/pagos) | runt_contingency (Verifik/Intempo failover).';

-- -------------------------------------------------------------------------------------
-- signature_wallets — "bal de firmas" (#9381 Configuración Empresa)
-- -------------------------------------------------------------------------------------
CREATE TABLE signature_wallets (
  id             uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id      uuid        NOT NULL,
  balance        integer     NOT NULL DEFAULT 0 CHECK (balance >= 0),
  low_threshold  integer     NOT NULL DEFAULT 0,
  auto_recharge  boolean     NOT NULL DEFAULT false,
  created_at     timestamptz NOT NULL DEFAULT now(),
  created_by     uuid        NOT NULL,
  updated_at     timestamptz NOT NULL DEFAULT now(),
  updated_by     uuid        NOT NULL,
  deleted_at     timestamptz NULL,
  deleted_by     uuid        NULL,
  row_version    integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_signature_wallets_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_signature_wallets_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_signature_wallets_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_signature_wallets_tenant UNIQUE (tenant_id)
);
ALTER TABLE signature_wallets ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON signature_wallets
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_signature_wallets_before_update_row_version
  BEFORE UPDATE ON signature_wallets FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_signature_wallets_audit
  AFTER INSERT OR UPDATE OR DELETE ON signature_wallets FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- signature_wallet_movements — ledger de consumo/recarga de firmas (append-only)
-- procedure_instance_id es referencia suave (sin FK; procedures es de mayor jerarquía).
-- -------------------------------------------------------------------------------------
CREATE TABLE signature_wallet_movements (
  id                   uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id            uuid        NOT NULL,
  wallet_id            uuid        NOT NULL,
  delta                integer     NOT NULL,
  reason               text        NOT NULL,
  balance_after        integer     NOT NULL CHECK (balance_after >= 0),
  procedure_instance_id uuid       NULL,  -- soft ref a procedures.procedure_instances
  created_at           timestamptz NOT NULL DEFAULT now(),
  created_by           uuid        NOT NULL,
  CONSTRAINT fk_signature_wallet_movements_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_signature_wallet_movements_wallets FOREIGN KEY (wallet_id) REFERENCES signature_wallets (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_signature_wallet_movements_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_signature_wallet_movements_tenant_id_wallet_id ON signature_wallet_movements (tenant_id, wallet_id, created_at DESC);
ALTER TABLE signature_wallet_movements ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON signature_wallet_movements
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
COMMENT ON TABLE signature_wallet_movements IS '@context:companies Ledger append-only del bal de firmas.';

-- -------------------------------------------------------------------------------------
-- vehicle_ownership_rules — interceptor de propiedad vehicular (#9381). Reutiliza árbol JSONB.
-- -------------------------------------------------------------------------------------
CREATE TABLE vehicle_ownership_rules (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NOT NULL,
  name          text        NOT NULL,
  rule_type     text        NOT NULL CHECK (rule_type IN ('allow','block','warn','require_exception')),
  condition     jsonb       NOT NULL DEFAULT '{}'::jsonb,
  priority      integer     NOT NULL DEFAULT 100,
  is_active     boolean     NOT NULL DEFAULT true,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NOT NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NOT NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_vehicle_ownership_rules_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_vehicle_ownership_rules_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_vehicle_ownership_rules_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_vehicle_ownership_rules_tenant_id_active ON vehicle_ownership_rules (tenant_id, is_active) WHERE deleted_at IS NULL;
ALTER TABLE vehicle_ownership_rules ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON vehicle_ownership_rules
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_vehicle_ownership_rules_before_update_row_version
  BEFORE UPDATE ON vehicle_ownership_rules FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_vehicle_ownership_rules_audit
  AFTER INSERT OR UPDATE OR DELETE ON vehicle_ownership_rules FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- escrituras — repositorio legal por compañía (#9383). Visible solo si módulo activo.
-- -------------------------------------------------------------------------------------
CREATE TABLE escrituras (
  id               uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id        uuid        NOT NULL,
  document_type_id uuid        NOT NULL,
  deed_number      text        NOT NULL,
  company_name     text        NOT NULL,
  expiration_date  date        NOT NULL,
  notes            text        NULL,
  created_at       timestamptz NOT NULL DEFAULT now(),
  created_by       uuid        NOT NULL,
  updated_at       timestamptz NOT NULL DEFAULT now(),
  updated_by       uuid        NOT NULL,
  deleted_at       timestamptz NULL,
  deleted_by       uuid        NULL,
  row_version      integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_escrituras_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_escrituras_document_types FOREIGN KEY (document_type_id) REFERENCES catalogs.document_types (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_escrituras_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_escrituras_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_escrituras_tenant_deed_number UNIQUE (tenant_id, deed_number)
);
CREATE INDEX ix_escrituras_tenant_id ON escrituras (tenant_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_escrituras_document_type_id ON escrituras (document_type_id);
CREATE INDEX ix_escrituras_tenant_id_expiration_date ON escrituras (tenant_id, expiration_date);
ALTER TABLE escrituras ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON escrituras
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_escrituras_before_update_row_version
  BEFORE UPDATE ON escrituras FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_escrituras_audit
  AFTER INSERT OR UPDATE OR DELETE ON escrituras FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- escritura_attachments — PDF adjuntos (solo PDF, ≤3MB, máx 5; reemplazo total al actualizar)
-- -------------------------------------------------------------------------------------
CREATE TABLE escritura_attachments (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NOT NULL,
  escritura_id  uuid        NOT NULL,
  file_id       uuid        NOT NULL,
  position      integer     NOT NULL CHECK (position BETWEEN 1 AND 5),
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NOT NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NOT NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_escritura_attachments_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_escritura_attachments_escrituras FOREIGN KEY (escritura_id) REFERENCES escrituras (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_escritura_attachments_files FOREIGN KEY (file_id) REFERENCES files.files (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_escritura_attachments_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_escritura_attachments_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_escritura_attachments_escritura_position UNIQUE (escritura_id, position)
);
CREATE INDEX ix_escritura_attachments_tenant_id_escritura_id ON escritura_attachments (tenant_id, escritura_id);
CREATE INDEX ix_escritura_attachments_file_id ON escritura_attachments (file_id);
ALTER TABLE escritura_attachments ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON escritura_attachments
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_escritura_attachments_before_update_row_version
  BEFORE UPDATE ON escritura_attachments FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
COMMENT ON TABLE escritura_attachments IS
  '@context:companies Validación de negocio (solo PDF, ≤3MB, máx 5) en la capa de aplicación; el límite de 5 lo refuerza el CHECK de position.';

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS companies.escritura_attachments, companies.escrituras,
--   companies.vehicle_ownership_rules, companies.signature_wallet_movements,
--   companies.signature_wallets, companies.company_module_configs, companies.companies CASCADE;
