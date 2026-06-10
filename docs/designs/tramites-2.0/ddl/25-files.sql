-- =====================================================================================
-- FLIT 2.0 · DDL 25 — Archivos (schema files). Substrato binario MinIO (ADR-0006) a convención.
-- Referenciado por file_id explícito desde companies/procedures/identity_verification (sin FK polimórfica).
-- =====================================================================================
SET search_path TO files;

CREATE TABLE files (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id         uuid        NOT NULL,
  bucket            text        NOT NULL,
  object_key        text        NOT NULL,
  original_filename text        NULL,
  content_type      text        NOT NULL,
  size_bytes        bigint      NOT NULL DEFAULT 0 CHECK (size_bytes >= 0),
  sha256            char(64)    NULL,
  status            text        NOT NULL DEFAULT 'pending'
                                CHECK (status IN ('pending','ready','deleted')),
  metadata          jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  deleted_at        timestamptz NULL,
  deleted_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_files_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_files_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_files_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_files_bucket_object_key UNIQUE (bucket, object_key)
);
CREATE INDEX ix_files_tenant_id ON files (tenant_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_files_tenant_id_status ON files (tenant_id, status);
ALTER TABLE files ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON files
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_files_before_update_row_version
  BEFORE UPDATE ON files FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_files_audit
  AFTER INSERT OR UPDATE OR DELETE ON files FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE files IS '@context:files Metadatos de objetos MinIO. El binario vive en object storage; aquí solo referencia.';
COMMENT ON COLUMN files.object_key IS 'Llave del objeto en el bucket (no exponer en URLs públicas; usar URLs prefirmadas).';

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS files.files CASCADE;
