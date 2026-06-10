-- =====================================================================================
-- FLIT 2.0 · DDL 90 — Dashboard read model (schema dashboard) (#9369)
-- Estrategia: vistas SQL con security_invoker (la RLS de procedure_instances aplica por usuario;
-- SuperAdmin ve cross-tenant vía identity.is_super_admin()). MV opcional para alto volumen.
-- KPIs por estado: Total, Borrador, Asignado, Enviado, Entregado, Rechazado, Anulado (+ otros).
-- =====================================================================================
SET search_path TO dashboard;

-- v_procedure_kpis — conteos por tenant / OT / estado / día (filtrable por rango de fechas)
CREATE VIEW dashboard.v_procedure_kpis
WITH (security_invoker = true) AS
SELECT
  pi.tenant_id,
  pi.traffic_agency_id,
  pi.state,
  date_trunc('day', pi.created_at)::date AS day,
  count(*) AS total
FROM procedures.procedure_instances pi
WHERE pi.deleted_at IS NULL
GROUP BY pi.tenant_id, pi.traffic_agency_id, pi.state, date_trunc('day', pi.created_at)::date;
COMMENT ON VIEW dashboard.v_procedure_kpis IS '@context:dashboard KPIs por estado/OT/día (#9369). RLS aplicada vía security_invoker.';

-- v_user_productivity — productividad por usuario (Radicador/Gestor/Operario) y estado
CREATE VIEW dashboard.v_user_productivity
WITH (security_invoker = true) AS
SELECT
  pi.tenant_id,
  pi.assigned_to_user_id,
  pi.state,
  count(*) AS total
FROM procedures.procedure_instances pi
WHERE pi.deleted_at IS NULL
GROUP BY pi.tenant_id, pi.assigned_to_user_id, pi.state;
COMMENT ON VIEW dashboard.v_user_productivity IS '@context:dashboard Tabla de productividad por usuario (#9369 reporte PDF).';

-- v_ot_distribution — distribución por Organismo de Tránsito (cantidad + base para %)
CREATE VIEW dashboard.v_ot_distribution
WITH (security_invoker = true) AS
SELECT
  pi.tenant_id,
  pi.traffic_agency_id,
  count(*) AS total
FROM procedures.procedure_instances pi
WHERE pi.deleted_at IS NULL
GROUP BY pi.tenant_id, pi.traffic_agency_id;
COMMENT ON VIEW dashboard.v_ot_distribution IS '@context:dashboard Distribución por secretaría (#9369). El % se calcula en la capa de reporte.';

-- -------------------------------------------------------------------------------------
-- Escala (opcional, diferido): MV de KPIs refrescada por evento + bitácora de refresco.
-- La MV NO aplica RLS (datos precomputados): la app filtra por tenant_id, o la consume el
-- consolidado SuperAdmin. Habilitar cuando el volumen lo justifique (ver supuesto #8 del plan).
-- -------------------------------------------------------------------------------------
CREATE MATERIALIZED VIEW dashboard.mv_procedure_kpis AS
SELECT
  pi.tenant_id,
  pi.traffic_agency_id,
  pi.state,
  date_trunc('day', pi.created_at)::date AS day,
  count(*) AS total
FROM procedures.procedure_instances pi
WHERE pi.deleted_at IS NULL
GROUP BY pi.tenant_id, pi.traffic_agency_id, pi.state, date_trunc('day', pi.created_at)::date
WITH NO DATA;
CREATE UNIQUE INDEX uq_mv_procedure_kpis_key
  ON dashboard.mv_procedure_kpis (tenant_id, COALESCE(traffic_agency_id,'00000000-0000-7000-8000-000000000000'), state, day);
COMMENT ON MATERIALIZED VIEW dashboard.mv_procedure_kpis IS
  '@context:dashboard MV opcional para alto volumen. REFRESH MATERIALIZED VIEW CONCURRENTLY por evento/cron.';

CREATE TABLE dashboard.read_model_refresh_log (
  id           uuid        PRIMARY KEY DEFAULT uuidv7(),
  view_name    text        NOT NULL,
  refreshed_at timestamptz NOT NULL DEFAULT now(),
  row_count    bigint      NULL,
  duration_ms  integer     NULL
);
CREATE INDEX ix_read_model_refresh_log_view_name ON dashboard.read_model_refresh_log (view_name, refreshed_at DESC);

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS dashboard.read_model_refresh_log;
-- DROP MATERIALIZED VIEW IF EXISTS dashboard.mv_procedure_kpis;
-- DROP VIEW IF EXISTS dashboard.v_ot_distribution, dashboard.v_user_productivity, dashboard.v_procedure_kpis;
