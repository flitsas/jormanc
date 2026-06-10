-- =====================================================================================
-- FLIT 2.0 · DDL 76 — IDSecure-Trámites (extensión identity_verification)
-- Feature ADO #9469 · Evolución de IDSecure-Traspasos → validación multi-trámite.
-- Requiere: ddl/00, ddl/20, ddl/25, ddl/50 (procedure_types), ddl/75 (base), ddl/80 (instances/actors).
-- Provider producción: vertex_ai; DEV: mock (misma interfaz IIdentityVerificationProvider).
-- =====================================================================================
SET search_path TO identity_verification;

-- -------------------------------------------------------------------------------------
-- procedure_type_verification_configs — qué participantes/aristas exigen IDSecure por tipo
-- -------------------------------------------------------------------------------------
CREATE TABLE procedure_type_verification_configs (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NOT NULL,
  procedure_type_id       uuid        NOT NULL,   -- soft ref procedures_config.procedure_types
  participant_role        text        NOT NULL,   -- arista/rol: vendedor, comprador, locatario, propietario, etc.
  is_required             boolean     NOT NULL DEFAULT true,
  email_template_key      text        NOT NULL DEFAULT 'default',
  sort_order              integer     NOT NULL DEFAULT 0,
  is_active               boolean     NOT NULL DEFAULT true,
  created_at              timestamptz NOT NULL DEFAULT now(),
  created_by              uuid        NOT NULL,
  updated_at              timestamptz NOT NULL DEFAULT now(),
  updated_by              uuid        NOT NULL,
  deleted_at              timestamptz NULL,
  deleted_by              uuid        NULL,
  row_version             integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_ptv_configs_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_ptv_configs_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_ptv_configs_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_ptv_configs_type_role UNIQUE (tenant_id, procedure_type_id, participant_role)
);
CREATE INDEX ix_ptv_configs_tenant_id_type ON procedure_type_verification_configs (tenant_id, procedure_type_id) WHERE deleted_at IS NULL;
ALTER TABLE procedure_type_verification_configs ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_type_verification_configs
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_ptv_configs_before_update_row_version BEFORE UPDATE ON procedure_type_verification_configs FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_ptv_configs_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_type_verification_configs FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- verification_invitations — token URL único, TTL 48h, un solo uso (RF-1.3/1.4)
-- -------------------------------------------------------------------------------------
CREATE TABLE verification_invitations (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NOT NULL,
  procedure_instance_id   uuid        NOT NULL,   -- soft ref procedures.procedure_instances
  procedure_actor_id      uuid        NULL,       -- soft ref procedures.procedure_actors
  participant_role        text        NOT NULL,
  recipient_email         text        NOT NULL,
  token_hash              text        NOT NULL,
  status                  text        NOT NULL DEFAULT 'pending'
                                      CHECK (status IN ('pending','sent','opened','consumed','expired','revoked')),
  expires_at              timestamptz NOT NULL,
  consumed_at             timestamptz NULL,
  resent_count            integer     NOT NULL DEFAULT 0,
  last_sent_at            timestamptz NULL,
  idempotency_key         text        NULL,
  created_at              timestamptz NOT NULL DEFAULT now(),
  created_by              uuid        NOT NULL,
  updated_at              timestamptz NOT NULL DEFAULT now(),
  updated_by              uuid        NOT NULL,
  deleted_at              timestamptz NULL,
  deleted_by              uuid        NULL,
  row_version             integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_verification_invitations_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_invitations_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_verification_invitations_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_verification_invitations_token_hash UNIQUE (token_hash)
);
CREATE INDEX ix_verification_invitations_tenant_instance ON verification_invitations (tenant_id, procedure_instance_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_verification_invitations_expires_at ON verification_invitations (expires_at) WHERE status IN ('pending','sent','opened');
ALTER TABLE verification_invitations ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON verification_invitations
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_verification_invitations_before_update_row_version BEFORE UPDATE ON verification_invitations FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_verification_invitations_audit AFTER INSERT OR UPDATE OR DELETE ON verification_invitations FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN verification_invitations.recipient_email IS '@pii:high';
COMMENT ON COLUMN verification_invitations.token_hash IS 'Solo hash; nunca almacenar token en claro.';

-- Extensión verification_sessions (IDSecure enlaza instancia + invitación)
ALTER TABLE verification_sessions
  ADD COLUMN IF NOT EXISTS procedure_instance_id uuid NULL,
  ADD COLUMN IF NOT EXISTS verification_invitation_id uuid NULL,
  ADD COLUMN IF NOT EXISTS participant_role text NULL,
  ADD COLUMN IF NOT EXISTS current_step smallint NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS provider text NOT NULL DEFAULT 'vertex_ai';
COMMENT ON COLUMN verification_sessions.provider IS 'vertex_ai | verifik | mock';

ALTER TABLE verification_sessions
  ADD CONSTRAINT fk_verification_sessions_invitations
    FOREIGN KEY (verification_invitation_id) REFERENCES verification_invitations (id) ON UPDATE CASCADE ON DELETE SET NULL;
CREATE INDEX IF NOT EXISTS ix_verification_sessions_invitation_id ON verification_sessions (verification_invitation_id);
CREATE INDEX IF NOT EXISTS ix_verification_sessions_procedure_instance_id ON verification_sessions (tenant_id, procedure_instance_id);

-- -------------------------------------------------------------------------------------
-- verification_session_steps — progreso secuencial del stepper (RF-2)
-- -------------------------------------------------------------------------------------
CREATE TABLE verification_session_steps (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NOT NULL,
  verification_session_id uuid        NOT NULL,
  step_number             smallint    NOT NULL CHECK (step_number BETWEEN 1 AND 4),
  step_code               text        NOT NULL CHECK (step_code IN ('document_capture','selfie','liveness','signature')),
  status                  text        NOT NULL DEFAULT 'pending'
                                      CHECK (status IN ('pending','in_progress','completed','failed','skipped')),
  completed_at            timestamptz NULL,
  metadata                jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at              timestamptz NOT NULL DEFAULT now(),
  created_by              uuid        NOT NULL,
  updated_at              timestamptz NOT NULL DEFAULT now(),
  updated_by              uuid        NOT NULL,
  row_version             integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_verification_session_steps_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_session_steps_sessions FOREIGN KEY (verification_session_id) REFERENCES verification_sessions (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_session_steps_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_verification_session_steps_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_verification_session_steps_session_step UNIQUE (verification_session_id, step_number)
);
CREATE INDEX ix_verification_session_steps_session_id ON verification_session_steps (verification_session_id);
ALTER TABLE verification_session_steps ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON verification_session_steps
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_verification_session_steps_before_update_row_version BEFORE UPDATE ON verification_session_steps FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_verification_session_steps_audit AFTER INSERT OR UPDATE OR DELETE ON verification_session_steps FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- evidence_type ampliado (firma digital)
ALTER TABLE verification_evidences DROP CONSTRAINT IF EXISTS verification_evidences_evidence_type_check;
ALTER TABLE verification_evidences ADD CONSTRAINT verification_evidences_evidence_type_check
  CHECK (evidence_type IN ('liveness','document_front','document_back','selfie','signature_canvas'));

-- -------------------------------------------------------------------------------------
-- verification_ocr_results — extracción OCR (RF-3.1) @pii:high
-- -------------------------------------------------------------------------------------
CREATE TABLE verification_ocr_results (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NOT NULL,
  verification_session_id uuid        NOT NULL,
  extracted_fields        jsonb       NOT NULL DEFAULT '{}'::jsonb,
  confidence_scores       jsonb       NOT NULL DEFAULT '{}'::jsonb,
  raw_provider_response   jsonb       NULL,
  processed_at            timestamptz NOT NULL DEFAULT now(),
  created_at              timestamptz NOT NULL DEFAULT now(),
  created_by              uuid        NOT NULL,
  row_version             integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_verification_ocr_results_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_ocr_results_sessions FOREIGN KEY (verification_session_id) REFERENCES verification_sessions (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_ocr_results_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_verification_ocr_results_session_id ON verification_ocr_results (verification_session_id);
ALTER TABLE verification_ocr_results ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON verification_ocr_results
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_verification_ocr_results_audit AFTER INSERT OR UPDATE OR DELETE ON verification_ocr_results FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE verification_ocr_results IS '@context:identity_verification @pii:high OCR de documento de identidad.';

-- -------------------------------------------------------------------------------------
-- verification_ai_verdicts — dictamen Approved/Rejected (RF-3.5)
-- -------------------------------------------------------------------------------------
CREATE TABLE verification_ai_verdicts (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NOT NULL,
  verification_session_id uuid        NOT NULL,
  verdict                 text        NOT NULL CHECK (verdict IN ('approved','rejected','manual_review')),
  biometric_score         numeric(5,4) NULL,
  liveness_passed         boolean     NULL,
  cross_match_passed      boolean     NULL,
  failure_reasons         jsonb       NOT NULL DEFAULT '[]'::jsonb,
  dictamen                jsonb       NOT NULL DEFAULT '{}'::jsonb,
  decided_at              timestamptz NOT NULL DEFAULT now(),
  created_at              timestamptz NOT NULL DEFAULT now(),
  created_by              uuid        NOT NULL,
  row_version             integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_verification_ai_verdicts_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_ai_verdicts_sessions FOREIGN KEY (verification_session_id) REFERENCES verification_sessions (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_ai_verdicts_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_verification_ai_verdicts_session_id ON verification_ai_verdicts (verification_session_id);
ALTER TABLE verification_ai_verdicts ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON verification_ai_verdicts
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_verification_ai_verdicts_audit AFTER INSERT OR UPDATE OR DELETE ON verification_ai_verdicts FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- verification_manual_overrides — override auditado (RF-4.7)
-- -------------------------------------------------------------------------------------
CREATE TABLE verification_manual_overrides (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NOT NULL,
  verification_session_id uuid        NOT NULL,
  previous_verdict        text        NOT NULL,
  new_verdict             text        NOT NULL CHECK (new_verdict IN ('approved','rejected')),
  reason                  text        NOT NULL,
  overridden_by           uuid        NOT NULL,
  overridden_at           timestamptz NOT NULL DEFAULT now(),
  created_at              timestamptz NOT NULL DEFAULT now(),
  created_by              uuid        NOT NULL,
  row_version             integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_verification_manual_overrides_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_manual_overrides_sessions FOREIGN KEY (verification_session_id) REFERENCES verification_sessions (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_verification_manual_overrides_users FOREIGN KEY (overridden_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_verification_manual_overrides_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_verification_manual_overrides_session_id ON verification_manual_overrides (verification_session_id);
ALTER TABLE verification_manual_overrides ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON verification_manual_overrides
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_verification_manual_overrides_audit AFTER INSERT OR UPDATE OR DELETE ON verification_manual_overrides FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- verification_email_templates — SMTP HTML por tipo de trámite (RF-1.5)
-- -------------------------------------------------------------------------------------
CREATE TABLE verification_email_templates (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NULL,       -- NULL = plantilla global SA
  template_key            text        NOT NULL,
  procedure_type_id       uuid        NULL,
  subject                 text        NOT NULL,
  html_body               text        NOT NULL,
  locale                  text        NOT NULL DEFAULT 'es-CO',
  is_active               boolean     NOT NULL DEFAULT true,
  created_at              timestamptz NOT NULL DEFAULT now(),
  created_by              uuid        NOT NULL,
  updated_at              timestamptz NOT NULL DEFAULT now(),
  updated_by              uuid        NOT NULL,
  deleted_at              timestamptz NULL,
  deleted_by              uuid        NULL,
  row_version             integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_verification_email_templates_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_verification_email_templates_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_verification_email_templates_key UNIQUE (tenant_id, template_key, procedure_type_id, locale)
);
CREATE INDEX ix_verification_email_templates_lookup ON verification_email_templates (tenant_id, template_key) WHERE deleted_at IS NULL;
ALTER TABLE verification_email_templates ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON verification_email_templates
  USING (tenant_id IS NULL OR tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id IS NULL OR tenant_id = current_setting('app.current_tenant_id', true)::uuid);

-- Bitácora de eventos IDSecure (idempotencia TRAMITE_CREATED — RF-1.7)
CREATE TABLE verification_domain_events (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NOT NULL,
  event_type              text        NOT NULL,
  procedure_instance_id   uuid        NOT NULL,
  payload                 jsonb       NOT NULL DEFAULT '{}'::jsonb,
  idempotency_key         text        NOT NULL,
  processed_at            timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT fk_verification_domain_events_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT uq_verification_domain_events_idempotency UNIQUE (tenant_id, idempotency_key)
);
CREATE INDEX ix_verification_domain_events_instance ON verification_domain_events (tenant_id, procedure_instance_id);
ALTER TABLE verification_domain_events ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON verification_domain_events
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
COMMENT ON TABLE verification_domain_events IS 'Append-only; sin soft-delete. Idempotencia de TRAMITE_CREATED.';

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS identity_verification.verification_domain_events,
--   identity_verification.verification_email_templates,
--   identity_verification.verification_manual_overrides,
--   identity_verification.verification_ai_verdicts,
--   identity_verification.verification_ocr_results,
--   identity_verification.verification_session_steps,
--   identity_verification.verification_invitations,
--   identity_verification.procedure_type_verification_configs CASCADE;
-- ALTER TABLE identity_verification.verification_sessions DROP COLUMN IF EXISTS procedure_instance_id, ...;
