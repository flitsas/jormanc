-- =====================================================================================
-- FLIT 2.0 · DDL 80 — Runtime de trámites (schema procedures)
-- Instancia radicada con SNAPSHOT inmutable de la config resuelta (ADR-0010).
-- Editable solo en 'borrador'. Transiciones de estado validadas por trigger.
-- =====================================================================================
SET search_path TO procedures;

-- Guard de transiciones de la máquina de estados (#9408 FR-5, #9409 CF-G4)
CREATE OR REPLACE FUNCTION procedures.validate_state_transition()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE allowed text[];
BEGIN
  IF NEW.state = OLD.state THEN RETURN NEW; END IF;
  allowed := CASE OLD.state
    WHEN 'borrador'     THEN ARRAY['asignado','q_validacion','anulado']
    WHEN 'asignado'     THEN ARRAY['q_validacion','borrador','anulado']
    WHEN 'q_validacion' THEN ARRAY['pendiente','rechazado','borrador','anulado']
    WHEN 'pendiente'    THEN ARRAY['aprobado','rechazado','anulado']
    WHEN 'aprobado'     THEN ARRAY['enviado','anulado']
    WHEN 'enviado'      THEN ARRAY['entregado','rechazado']
    WHEN 'rechazado'    THEN ARRAY['borrador','anulado']
    ELSE ARRAY[]::text[]  -- entregado / anulado = terminales
  END;
  IF NOT (NEW.state = ANY(allowed)) THEN
    RAISE EXCEPTION 'Transición de estado inválida: % -> %', OLD.state, NEW.state
      USING ERRCODE = 'check_violation';
  END IF;
  RETURN NEW;
END;
$$;

-- -------------------------------------------------------------------------------------
-- procedure_instances — trámite radicado con snapshot de config inmutable
-- -------------------------------------------------------------------------------------
CREATE TABLE procedure_instances (
  id                  uuid          PRIMARY KEY DEFAULT uuidv7(),
  tenant_id           uuid          NOT NULL,
  procedure_type_id   uuid          NOT NULL,
  traffic_agency_id   uuid          NULL,
  reference_number    text          NOT NULL,
  state               text          NOT NULL DEFAULT 'borrador'
                      CHECK (state IN ('borrador','asignado','q_validacion','pendiente','aprobado','enviado','entregado','rechazado','anulado')),
  config_snapshot     jsonb         NOT NULL DEFAULT '{}'::jsonb,  -- config resuelta congelada (aristas/campos/docs/reglas)
  config_schema_version integer     NOT NULL DEFAULT 1,
  assigned_to_user_id uuid          NULL,
  radicated_at        timestamptz   NULL,
  completed_at        timestamptz   NULL,
  total_amount        numeric(15,2) NOT NULL DEFAULT 0 CHECK (total_amount >= 0),
  currency_code       char(3)       NOT NULL DEFAULT 'COP',
  metadata            jsonb         NOT NULL DEFAULT '{}'::jsonb,
  created_at          timestamptz   NOT NULL DEFAULT now(),
  created_by          uuid          NOT NULL,
  updated_at          timestamptz   NOT NULL DEFAULT now(),
  updated_by          uuid          NOT NULL,
  deleted_at          timestamptz   NULL,
  deleted_by          uuid          NULL,
  row_version         integer       NOT NULL DEFAULT 1,
  CONSTRAINT fk_procedure_instances_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_instances_types FOREIGN KEY (procedure_type_id) REFERENCES procedures_config.procedure_types (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_instances_traffic_agencies FOREIGN KEY (traffic_agency_id) REFERENCES ot.traffic_agencies (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_instances_users_assignee FOREIGN KEY (assigned_to_user_id) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_instances_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_instances_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_procedure_instances_reference_number UNIQUE (tenant_id, reference_number)
);
CREATE INDEX ix_procedure_instances_tenant_id_state ON procedure_instances (tenant_id, state) WHERE deleted_at IS NULL;
CREATE INDEX ix_procedure_instances_tenant_id_agency_created ON procedure_instances (tenant_id, traffic_agency_id, created_at DESC);
CREATE INDEX ix_procedure_instances_tenant_id_assignee ON procedure_instances (tenant_id, assigned_to_user_id);
CREATE INDEX ix_procedure_instances_procedure_type_id ON procedure_instances (procedure_type_id);
CREATE INDEX ix_procedure_instances_created_at ON procedure_instances (created_at DESC);
ALTER TABLE procedure_instances ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_instances
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_procedure_instances_before_update_state
  BEFORE UPDATE ON procedure_instances FOR EACH ROW EXECUTE FUNCTION procedures.validate_state_transition();
CREATE TRIGGER tr_procedure_instances_before_update_row_version
  BEFORE UPDATE ON procedure_instances FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_procedure_instances_audit
  AFTER INSERT OR UPDATE OR DELETE ON procedure_instances FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE procedure_instances IS '@context:procedures @entity:trámite Instancia radicada; config_snapshot inmutable (ADR-0010).';
COMMENT ON COLUMN procedure_instances.config_snapshot IS '@semi-structured Config resuelta y congelada al radicar; el runtime no depende de ediciones posteriores.';

-- -------------------------------------------------------------------------------------
-- procedure_field_values — valores capturados (normalizado, Híbrido)
-- -------------------------------------------------------------------------------------
CREATE TABLE procedure_field_values (
  id                    uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id             uuid        NOT NULL,
  procedure_instance_id uuid        NOT NULL,
  edge_role             text        NULL,
  field_key             text        NOT NULL,
  value                 jsonb       NOT NULL DEFAULT 'null'::jsonb,
  data_type             text        NOT NULL,
  created_at            timestamptz NOT NULL DEFAULT now(),
  created_by            uuid        NOT NULL,
  updated_at            timestamptz NOT NULL DEFAULT now(),
  updated_by            uuid        NOT NULL,
  deleted_at            timestamptz NULL,
  deleted_by            uuid        NULL,
  row_version           integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_procedure_field_values_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_field_values_instances FOREIGN KEY (procedure_instance_id) REFERENCES procedure_instances (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_field_values_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_field_values_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE UNIQUE INDEX uq_procedure_field_values_instance_edge_field
  ON procedure_field_values (procedure_instance_id, COALESCE(edge_role,''), field_key) WHERE deleted_at IS NULL;
CREATE INDEX ix_procedure_field_values_tenant_id_instance ON procedure_field_values (tenant_id, procedure_instance_id);
ALTER TABLE procedure_field_values ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_field_values
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_procedure_field_values_before_update_row_version BEFORE UPDATE ON procedure_field_values FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();

-- -------------------------------------------------------------------------------------
-- procedure_actors — actores por arista (#9409). Jurídica → sub-actor representante legal.
-- -------------------------------------------------------------------------------------
CREATE TABLE procedure_actors (
  id                    uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id             uuid        NOT NULL,
  procedure_instance_id uuid        NOT NULL,
  edge_role             text        NOT NULL CHECK (edge_role IN ('propietario','comprador','locatario')),
  person_kind           text        NOT NULL CHECK (person_kind IN ('natural','juridica')),
  document_type_id      uuid        NOT NULL,
  document_number       text        NOT NULL,
  full_name             text        NULL,
  email                 text        NULL,
  phone                 text        NULL,
  created_at            timestamptz NOT NULL DEFAULT now(),
  created_by            uuid        NOT NULL,
  updated_at            timestamptz NOT NULL DEFAULT now(),
  updated_by            uuid        NOT NULL,
  deleted_at            timestamptz NULL,
  deleted_by            uuid        NULL,
  row_version           integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_procedure_actors_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_actors_instances FOREIGN KEY (procedure_instance_id) REFERENCES procedure_instances (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_actors_document_types FOREIGN KEY (document_type_id) REFERENCES catalogs.document_types (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_actors_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_actors_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_procedure_actors_instance_edge UNIQUE (procedure_instance_id, edge_role)
);
CREATE INDEX ix_procedure_actors_tenant_id_instance ON procedure_actors (tenant_id, procedure_instance_id);
ALTER TABLE procedure_actors ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_actors
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_procedure_actors_before_update_row_version BEFORE UPDATE ON procedure_actors FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_procedure_actors_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_actors FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN procedure_actors.document_number IS '@pii:high';
COMMENT ON COLUMN procedure_actors.full_name IS '@pii:medium';

-- procedure_actor_representatives — representante legal (jurídica). 1:1 con actor jurídico.
CREATE TABLE procedure_actor_representatives (
  id                  uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id           uuid        NOT NULL,
  procedure_actor_id  uuid        NOT NULL,
  document_type_id    uuid        NOT NULL,
  document_number     text        NOT NULL,
  full_name           text        NOT NULL,
  email               text        NULL,
  phone               text        NULL,
  created_at          timestamptz NOT NULL DEFAULT now(),
  created_by          uuid        NOT NULL,
  updated_at          timestamptz NOT NULL DEFAULT now(),
  updated_by          uuid        NOT NULL,
  deleted_at          timestamptz NULL,
  deleted_by          uuid        NULL,
  row_version         integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_actor_representatives_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_actor_representatives_actors FOREIGN KEY (procedure_actor_id) REFERENCES procedure_actors (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_actor_representatives_document_types FOREIGN KEY (document_type_id) REFERENCES catalogs.document_types (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_actor_representatives_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_actor_representatives_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_actor_representatives_actor UNIQUE (procedure_actor_id)
);
CREATE INDEX ix_actor_representatives_tenant_id ON procedure_actor_representatives (tenant_id);
ALTER TABLE procedure_actor_representatives ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_actor_representatives
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_actor_representatives_before_update_row_version BEFORE UPDATE ON procedure_actor_representatives FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_actor_representatives_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_actor_representatives FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN procedure_actor_representatives.document_number IS '@pii:high';

-- -------------------------------------------------------------------------------------
-- procedure_vehicles — arista vehículo (placa o VIN). 1:1 con instancia.
-- -------------------------------------------------------------------------------------
CREATE TABLE procedure_vehicles (
  id                    uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id             uuid        NOT NULL,
  procedure_instance_id uuid        NOT NULL,
  vehicle_subkind       text        NOT NULL CHECK (vehicle_subkind IN ('automovil','moto','otro','maquinaria','remolque')),
  license_plate         text        NULL,
  vin                   text        NULL,
  make_id               uuid        NULL,
  line_id               uuid        NULL,
  class_id              uuid        NULL,
  color_id              uuid        NULL,
  fuel_id               uuid        NULL,
  runt_snapshot         jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at            timestamptz NOT NULL DEFAULT now(),
  created_by            uuid        NOT NULL,
  updated_at            timestamptz NOT NULL DEFAULT now(),
  updated_by            uuid        NOT NULL,
  deleted_at            timestamptz NULL,
  deleted_by            uuid        NULL,
  row_version           integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_procedure_vehicles_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_vehicles_instances FOREIGN KEY (procedure_instance_id) REFERENCES procedure_instances (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_vehicles_makes FOREIGN KEY (make_id) REFERENCES catalogs.vehicle_makes (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_vehicles_lines FOREIGN KEY (line_id) REFERENCES catalogs.vehicle_lines (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_vehicles_classes FOREIGN KEY (class_id) REFERENCES catalogs.vehicle_classes (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_vehicles_colors FOREIGN KEY (color_id) REFERENCES catalogs.colors (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_vehicles_fuel FOREIGN KEY (fuel_id) REFERENCES catalogs.fuel_types (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_vehicles_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_vehicles_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_procedure_vehicles_instance UNIQUE (procedure_instance_id),
  CONSTRAINT ck_procedure_vehicles_plate_or_vin CHECK (license_plate IS NOT NULL OR vin IS NOT NULL),
  CONSTRAINT ck_procedure_vehicles_plate_format CHECK (
    license_plate IS NULL
    OR license_plate ~ '^[A-Z]{3}[0-9]{3}$'
    OR license_plate ~ '^[A-Z]{3}[0-9]{2}[A-Z]$'
    OR license_plate ~ '^[A-Z]{1,2}[0-9]{4,5}$'
  )
);
CREATE INDEX ix_procedure_vehicles_tenant_id ON procedure_vehicles (tenant_id);
CREATE INDEX ix_procedure_vehicles_license_plate ON procedure_vehicles (license_plate) WHERE license_plate IS NOT NULL;
CREATE INDEX ix_procedure_vehicles_make_id ON procedure_vehicles (make_id);
CREATE INDEX ix_procedure_vehicles_line_id ON procedure_vehicles (line_id);
CREATE INDEX ix_procedure_vehicles_class_id ON procedure_vehicles (class_id);
CREATE INDEX ix_procedure_vehicles_color_id ON procedure_vehicles (color_id);
CREATE INDEX ix_procedure_vehicles_fuel_id ON procedure_vehicles (fuel_id);
ALTER TABLE procedure_vehicles ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_vehicles
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_procedure_vehicles_before_update_row_version BEFORE UPDATE ON procedure_vehicles FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_procedure_vehicles_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_vehicles FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- procedure_query_results — SNAPSHOT de resultados de consultas externas (1 por fuente)
-- -------------------------------------------------------------------------------------
CREATE TABLE procedure_query_results (
  id                    uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id             uuid        NOT NULL,
  procedure_instance_id uuid        NOT NULL,
  query_connector_code  text        NOT NULL,
  edge_role             text        NULL,
  source                text        NOT NULL,
  status                text        NOT NULL CHECK (status IN ('ok','failed','partial')),
  result                jsonb       NOT NULL DEFAULT '{}'::jsonb,
  requested_at          timestamptz NOT NULL DEFAULT now(),
  responded_at          timestamptz NULL,
  integration_call_id   uuid        NULL,
  created_at            timestamptz NOT NULL DEFAULT now(),
  created_by            uuid        NOT NULL,
  updated_at            timestamptz NOT NULL DEFAULT now(),
  updated_by            uuid        NOT NULL,
  deleted_at            timestamptz NULL,
  deleted_by            uuid        NULL,
  row_version           integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_procedure_query_results_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_query_results_instances FOREIGN KEY (procedure_instance_id) REFERENCES procedure_instances (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_query_results_calls FOREIGN KEY (integration_call_id) REFERENCES integrations.external_query_calls (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_query_results_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_query_results_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
-- Unicidad por (instancia, fuente, arista) tratando edge_role NULL como '' (expresión → índice único)
CREATE UNIQUE INDEX uq_procedure_query_results_instance_source
  ON procedure_query_results (procedure_instance_id, query_connector_code, COALESCE(edge_role,'')) WHERE deleted_at IS NULL;
CREATE INDEX ix_procedure_query_results_tenant_id_instance ON procedure_query_results (tenant_id, procedure_instance_id);
ALTER TABLE procedure_query_results ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_query_results
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_procedure_query_results_before_update_row_version BEFORE UPDATE ON procedure_query_results FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
COMMENT ON TABLE procedure_query_results IS '@context:procedures Snapshot por fuente (fallo aislado #9409 CF-D7). Re-ejecución solo en borrador (app).';

-- -------------------------------------------------------------------------------------
-- procedure_documents — adjuntos (upload) + generados (auto_generated)
-- -------------------------------------------------------------------------------------
CREATE TABLE procedure_documents (
  id                    uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id             uuid        NOT NULL,
  procedure_instance_id uuid        NOT NULL,
  kind                  text        NOT NULL CHECK (kind IN ('upload','auto_generated')),
  document_type_id      uuid        NOT NULL,
  edge_role             text        NULL,
  actor_id              uuid        NULL,
  file_id               uuid        NULL,
  template_version_id   uuid        NULL,
  data_snapshot         jsonb       NOT NULL DEFAULT '{}'::jsonb,  -- valores de marcadores resueltos (doc generado)
  is_required           boolean     NOT NULL DEFAULT true,
  display_order         integer     NOT NULL DEFAULT 0,
  status                text        NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','uploaded','generated','rejected')),
  created_at            timestamptz NOT NULL DEFAULT now(),
  created_by            uuid        NOT NULL,
  updated_at            timestamptz NOT NULL DEFAULT now(),
  updated_by            uuid        NOT NULL,
  deleted_at            timestamptz NULL,
  deleted_by            uuid        NULL,
  row_version           integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_procedure_documents_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_documents_instances FOREIGN KEY (procedure_instance_id) REFERENCES procedure_instances (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_documents_document_types FOREIGN KEY (document_type_id) REFERENCES catalogs.document_types (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_documents_actors FOREIGN KEY (actor_id) REFERENCES procedure_actors (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_documents_files FOREIGN KEY (file_id) REFERENCES files.files (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_procedure_documents_template_versions FOREIGN KEY (template_version_id) REFERENCES procedures_config.document_template_versions (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_documents_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_procedure_documents_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_procedure_documents_tenant_id_instance ON procedure_documents (tenant_id, procedure_instance_id);
CREATE INDEX ix_procedure_documents_file_id ON procedure_documents (file_id);
CREATE INDEX ix_procedure_documents_template_version_id ON procedure_documents (template_version_id);
CREATE INDEX ix_procedure_documents_document_type_id ON procedure_documents (document_type_id);
CREATE INDEX ix_procedure_documents_actor_id ON procedure_documents (actor_id);
ALTER TABLE procedure_documents ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_documents
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_procedure_documents_before_update_row_version BEFORE UPDATE ON procedure_documents FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_procedure_documents_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_documents FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- procedure_state_history — historial de transiciones (append-only)
-- -------------------------------------------------------------------------------------
CREATE TABLE procedure_state_history (
  id                    uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id             uuid        NOT NULL,
  procedure_instance_id uuid        NOT NULL,
  from_state            text        NULL,
  to_state              text        NOT NULL,
  reason                text        NULL,
  metadata              jsonb       NOT NULL DEFAULT '{}'::jsonb,
  changed_by            uuid        NOT NULL,
  changed_at            timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT fk_procedure_state_history_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_state_history_instances FOREIGN KEY (procedure_instance_id) REFERENCES procedure_instances (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_procedure_state_history_users FOREIGN KEY (changed_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_procedure_state_history_tenant_id_instance ON procedure_state_history (tenant_id, procedure_instance_id, changed_at DESC);
ALTER TABLE procedure_state_history ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_state_history
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);

-- -------------------------------------------------------------------------------------
-- procedure_identity_validations — enlace a validación de identidad reutilizable
-- -------------------------------------------------------------------------------------
CREATE TABLE procedure_identity_validations (
  id                      uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id               uuid        NOT NULL,
  procedure_instance_id   uuid        NOT NULL,
  verification_session_id uuid        NOT NULL,
  actor_id                uuid        NULL,
  verdict                 text        NULL,
  linked_at               timestamptz NOT NULL DEFAULT now(),
  created_at              timestamptz NOT NULL DEFAULT now(),
  created_by              uuid        NOT NULL,
  updated_at              timestamptz NOT NULL DEFAULT now(),
  updated_by              uuid        NOT NULL,
  deleted_at              timestamptz NULL,
  deleted_by              uuid        NULL,
  row_version             integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_proc_id_validations_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_proc_id_validations_instances FOREIGN KEY (procedure_instance_id) REFERENCES procedure_instances (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_proc_id_validations_sessions FOREIGN KEY (verification_session_id) REFERENCES identity_verification.verification_sessions (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_proc_id_validations_actors FOREIGN KEY (actor_id) REFERENCES procedure_actors (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_proc_id_validations_users_creator FOREIGN KEY (created_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_proc_id_validations_users_updater FOREIGN KEY (updated_by) REFERENCES identity.users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_proc_id_validations_instance_actor UNIQUE (procedure_instance_id, actor_id)
);
CREATE INDEX ix_proc_id_validations_tenant_id_instance ON procedure_identity_validations (tenant_id, procedure_instance_id);
CREATE INDEX ix_proc_id_validations_session_id ON procedure_identity_validations (verification_session_id);
CREATE INDEX ix_proc_id_validations_actor_id ON procedure_identity_validations (actor_id);
ALTER TABLE procedure_identity_validations ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON procedure_identity_validations
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_proc_id_validations_before_update_row_version BEFORE UPDATE ON procedure_identity_validations FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_proc_id_validations_audit AFTER INSERT OR UPDATE OR DELETE ON procedure_identity_validations FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS procedures.procedure_identity_validations, procedures.procedure_state_history,
--   procedures.procedure_documents, procedures.procedure_query_results, procedures.procedure_vehicles,
--   procedures.procedure_actor_representatives, procedures.procedure_actors,
--   procedures.procedure_field_values, procedures.procedure_instances CASCADE;
-- DROP FUNCTION IF EXISTS procedures.validate_state_transition();
