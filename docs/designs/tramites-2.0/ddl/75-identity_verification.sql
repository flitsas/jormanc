-- =====================================================================================
-- FLIT 2.0 · DDL 75 — Validación de identidad (schema identity_verification)
-- Liveness/biometría (§8 reglas-estándar, #9408 FR-6). Corre una vez y se reutiliza.
-- Evidencia = dato sensible (@pii:high); el binario vive en files; aquí solo veredicto y refs.
-- =====================================================================================
SET search_path TO identity_verification;

-- verification_sessions — sesión de validación (liveness + captura). Reutilizable por el trámite.
CREATE TABLE verification_sessions (
  id                       uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id                uuid        NOT NULL,
  subject_document_type_id uuid        NOT NULL,
  subject_document_number  text        NOT NULL,
  provider                 text        NOT NULL DEFAULT 'verifik',
  status                   text        NOT NULL DEFAULT 'pending'
                                       CHECK (status IN ('pending','passed','failed','expired')),
  verdict                  text        NULL,
  score                    numeric(5,2) NULL,
  performed_at             timestamptz NULL,
  expires_at              timestamptz NULL,
  created_at               timestamptz NOT NULL DEFAULT now(),
  created_by               uuid        NOT NULL,
  updated_at               timestamptz NOT NULL DEFAULT now(),
  updated_by               uuid        NOT NULL,
  deleted_at               timestamptz NULL,
  deleted_by               uuid        NULL,
  row_version              integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_verification_sessions_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_sessions_document_types FOREIGN KEY (subject_document_type_id) REFERENCES catalogs.document_types (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_verification_sessions_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_verification_sessions_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_verification_sessions_tenant_id ON verification_sessions (tenant_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_verification_sessions_tenant_id_subject ON verification_sessions (tenant_id, subject_document_type_id, subject_document_number);
ALTER TABLE verification_sessions ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON verification_sessions
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_verification_sessions_before_update_row_version BEFORE UPDATE ON verification_sessions FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_verification_sessions_audit AFTER INSERT OR UPDATE OR DELETE ON verification_sessions FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN verification_sessions.subject_document_number IS '@pii:high @retention:según política Habeas Data';
COMMENT ON COLUMN verification_sessions.verdict IS 'Resultado disponible para plantillas de documentos (marker_map) y reglas de consulta.';

-- verification_evidences — evidencias (liveness, fotos de documento). Binario en files.
CREATE TABLE verification_evidences (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NOT NULL,
  verification_session_id uuid        NOT NULL,
  evidence_type           text        NOT NULL CHECK (evidence_type IN ('liveness','document_front','document_back','selfie')),
  file_id                 uuid        NOT NULL,
  captured_at             timestamptz NOT NULL DEFAULT now(),
  created_at              timestamptz NOT NULL DEFAULT now(),
  created_by              uuid        NOT NULL,
  updated_at              timestamptz NOT NULL DEFAULT now(),
  updated_by              uuid        NOT NULL,
  deleted_at              timestamptz NULL,
  deleted_by              uuid        NULL,
  row_version             integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_verification_evidences_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_evidences_sessions FOREIGN KEY (verification_session_id) REFERENCES verification_sessions (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_evidences_files FOREIGN KEY (file_id) REFERENCES files.files (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_verification_evidences_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_verification_evidences_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_verification_evidences_tenant_id_session ON verification_evidences (tenant_id, verification_session_id);
CREATE INDEX ix_verification_evidences_file_id ON verification_evidences (file_id);
ALTER TABLE verification_evidences ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON verification_evidences
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_verification_evidences_before_update_row_version BEFORE UPDATE ON verification_evidences FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_verification_evidences_audit AFTER INSERT OR UPDATE OR DELETE ON verification_evidences FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE verification_evidences IS '@context:identity_verification @pii:high Evidencia biométrica (dato sensible Ley 1581).';

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS identity_verification.verification_evidences,
--   identity_verification.verification_sessions CASCADE;
