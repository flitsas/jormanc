-- =====================================================================================
-- FLIT 2.0 · DDL 70 — Integraciones (schema integrations)
-- Ejecución de conectores externos: logs de llamadas, RUNT/Verifik/Intempo failover, webhooks QX.
-- Tablas append-only (sin row_version/soft-delete: son bitácoras). RLS por tenant.
-- procedure_instance_id es referencia suave (sin FK; procedures es de mayor jerarquía).
-- =====================================================================================
SET search_path TO integrations;

-- external_query_calls — log crudo de cada consulta externa (#9409 CF-I1). Particionar por mes en PDN.
CREATE TABLE external_query_calls (
  id                    uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id             uuid        NOT NULL,
  procedure_instance_id uuid        NULL,            -- soft ref a procedures.procedure_instances
  query_connector_code  text        NOT NULL,
  edge_role             text        NULL,
  request               jsonb       NOT NULL DEFAULT '{}'::jsonb,
  response              jsonb       NULL,
  http_status           integer     NULL,
  latency_ms            integer     NULL,
  succeeded             boolean     NOT NULL DEFAULT false,
  error_message         text        NULL,
  called_at             timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT fk_external_query_calls_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX ix_external_query_calls_tenant_id_called_at ON external_query_calls (tenant_id, called_at DESC);
CREATE INDEX ix_external_query_calls_procedure_instance_id ON external_query_calls (procedure_instance_id);
ALTER TABLE external_query_calls ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON external_query_calls
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
COMMENT ON TABLE external_query_calls IS '@context:integrations Bitácora de consultas externas. @pii: evitar PII innecesaria en request/response (CF-I3).';

-- runt_sync_log — trazas RUNT con failover Verifik/Intempo (#9381 contingencia)
CREATE TABLE runt_sync_log (
  id                    uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id             uuid        NOT NULL,
  provider              text        NOT NULL CHECK (provider IN ('runt','verifik','intempo')),
  operation             text        NOT NULL,
  outcome               text        NOT NULL CHECK (outcome IN ('ok','failed','timeout','circuit_open')),
  failover_from         text        NULL CHECK (failover_from IN ('runt','verifik','intempo')),
  payload               jsonb       NOT NULL DEFAULT '{}'::jsonb,
  synced_at             timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT fk_runt_sync_log_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX ix_runt_sync_log_tenant_id_synced_at ON runt_sync_log (tenant_id, synced_at DESC);
ALTER TABLE runt_sync_log ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON runt_sync_log
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);

-- endpoint_call_log — invocaciones a endpoints del catálogo desde reglas (#9410 FR-4 auditoría)
CREATE TABLE endpoint_call_log (
  id                    uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id             uuid        NOT NULL,
  endpoint_code         text        NOT NULL,
  procedure_instance_id uuid        NULL,            -- soft ref
  request               jsonb       NOT NULL DEFAULT '{}'::jsonb,
  response              jsonb       NULL,
  http_status           integer     NULL,
  succeeded             boolean     NOT NULL DEFAULT false,
  called_at             timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT fk_endpoint_call_log_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE
);
CREATE INDEX ix_endpoint_call_log_tenant_id_called_at ON endpoint_call_log (tenant_id, called_at DESC);
ALTER TABLE endpoint_call_log ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON endpoint_call_log
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);

-- webhook_events — sincronización QX (Quipux) inbound/outbound con idempotencia (#9378 QX)
CREATE TABLE webhook_events (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id         uuid        NOT NULL,
  traffic_agency_id uuid        NULL,
  direction         text        NOT NULL CHECK (direction IN ('inbound','outbound')),
  event_type        text        NOT NULL,
  payload           jsonb       NOT NULL DEFAULT '{}'::jsonb,
  signature         text        NULL,
  idempotency_key   text        NOT NULL,
  status            text        NOT NULL DEFAULT 'received' CHECK (status IN ('received','processed','failed')),
  received_at       timestamptz NOT NULL DEFAULT now(),
  processed_at      timestamptz NULL,
  CONSTRAINT fk_webhook_events_tenants FOREIGN KEY (tenant_id) REFERENCES identity.tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_webhook_events_traffic_agencies FOREIGN KEY (traffic_agency_id) REFERENCES ot.traffic_agencies (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT uq_webhook_events_idempotency UNIQUE (idempotency_key)
);
CREATE INDEX ix_webhook_events_tenant_id_received_at ON webhook_events (tenant_id, received_at DESC);
CREATE INDEX ix_webhook_events_traffic_agency_id ON webhook_events (traffic_agency_id);
ALTER TABLE webhook_events ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON webhook_events
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS integrations.webhook_events, integrations.endpoint_call_log,
--   integrations.runt_sync_log, integrations.external_query_calls CASCADE;
