-- =====================================================================================
-- FLIT 2.0 · Trámites de Tránsito · DDL 00 — Extensiones, schemas y auditoría base
-- =====================================================================================
-- Orden de ejecución: 00 → 10 → 20 → 25 → 30 → 40 → 50 → 70 → 75 → 80 → 90
-- Cumple docs/database-conventions.md (PostgreSQL 17+, EF Core 10, multi-tenant + RLS).
-- Idioma: estructura técnica en inglés; datos/enums de negocio en español (sin tildes/ñ).
-- Reversibilidad: bloque DOWN comentado al final de cada archivo (ver §16).
-- =====================================================================================

-- -------------------------------------------------------------------------------------
-- 1. Extensiones
-- -------------------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS pgcrypto;   -- gen_random_uuid / gen_random_bytes
CREATE EXTENSION IF NOT EXISTS citext;     -- email case-insensitive
CREATE EXTENSION IF NOT EXISTS pg_trgm;    -- búsquedas ILIKE eficientes (filtros dashboard/grids)
-- Si el motor es PostgreSQL 17 con la extensión disponible, se prefiere la nativa:
--   CREATE EXTENSION IF NOT EXISTS pg_uuidv7;
-- En PostgreSQL 18+ uuidv7() es nativo (pg_catalog). El fallback de §2 es inocuo en ambos casos
-- porque pg_catalog se resuelve antes que public en el search_path.

-- -------------------------------------------------------------------------------------
-- 2. uuidv7() — fallback portable (RFC 9562) para PostgreSQL < 18 sin pg_uuidv7
--    En PostgreSQL 18+ la función nativa pg_catalog.uuidv7() tiene precedencia.
-- -------------------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.uuidv7()
RETURNS uuid
LANGUAGE sql
VOLATILE
AS $$
  -- 48 bits de timestamp unix en milisegundos + version(7)/variant + 74 bits aleatorios
  SELECT encode(
    set_bit(
      set_bit(
        overlay(
          uuid_send(gen_random_uuid())
          PLACING substring(int8send((extract(epoch FROM clock_timestamp()) * 1000)::bigint) FROM 3)
          FROM 1 FOR 6
        ),
        52, 1
      ),
      53, 1
    ),
    'hex'
  )::uuid;
$$;

COMMENT ON FUNCTION public.uuidv7() IS
  'Fallback RFC 9562 UUIDv7 para PG<18. Ordenable temporalmente. En PG18+ se usa la nativa.';

-- -------------------------------------------------------------------------------------
-- 3. Schemas (bounded contexts). public queda vacío salvo extensiones/uuidv7.
--    Jerarquía de referencias (evita ciclos de FK; alto → bajo):
--    dashboard > procedures > identity_verification > integrations > procedures_config
--             > ot > companies > files > identity > catalogs
-- -------------------------------------------------------------------------------------
CREATE SCHEMA IF NOT EXISTS catalogs;
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS files;
CREATE SCHEMA IF NOT EXISTS companies;
CREATE SCHEMA IF NOT EXISTS ot;
CREATE SCHEMA IF NOT EXISTS procedures_config;
CREATE SCHEMA IF NOT EXISTS integrations;
CREATE SCHEMA IF NOT EXISTS identity_verification;
CREATE SCHEMA IF NOT EXISTS procedures;
CREATE SCHEMA IF NOT EXISTS dashboard;
CREATE SCHEMA IF NOT EXISTS audit;

COMMENT ON SCHEMA catalogs            IS 'Datos de referencia compartidos (sin tenant_id).';
COMMENT ON SCHEMA identity            IS 'Tenants (compañía-raíz), usuarios, roles, permisos-slug, onboarding. #9370';
COMMENT ON SCHEMA files               IS 'Metadatos de objetos en MinIO; substrato binario (referenciado por file_id).';
COMMENT ON SCHEMA companies           IS 'Maestro de compañía (1:1 tenant), config modular, firmas, escrituras. #9381 #9383';
COMMENT ON SCHEMA ot                  IS 'Organismos de Tránsito (cross-tenant, sin tenant_id), reglas OT, orden consolidado, QX. #9378 #9379';
COMMENT ON SCHEMA procedures_config   IS 'Parametrización: familias/tipos/aristas/matriz/forms/docs/consultas/reglas. #9408 #9409 #9410';
COMMENT ON SCHEMA integrations        IS 'Ejecución de conectores externos: logs, RUNT/Verifik/Intempo, webhooks QX.';
COMMENT ON SCHEMA identity_verification IS 'Liveness/biometría: sesiones, veredictos, evidencias (@pii:high). §8 reglas-estándar';
COMMENT ON SCHEMA procedures          IS 'Runtime: instancias radicadas con snapshot inmutable de config.';
COMMENT ON SCHEMA dashboard           IS 'Read model (vistas/MV) para KPIs y exportación. #9369';
COMMENT ON SCHEMA audit               IS 'Bitácora transversal de cambios y de accesos a datos.';

-- -------------------------------------------------------------------------------------
-- 4. Auditoría — tabla central (§11) + bitácora de accesos a datos (ADR-0006 C5)
-- -------------------------------------------------------------------------------------
CREATE TABLE audit.audit_log (
  id            uuid         PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid         NOT NULL,   -- denormalizado; sentinela para config global de plataforma
  schema_name   text         NOT NULL,
  table_name    text         NOT NULL,
  record_id     uuid         NOT NULL,
  operation     char(1)      NOT NULL CHECK (operation IN ('I','U','D')),
  changed_by    uuid         NOT NULL,
  changed_at    timestamptz  NOT NULL DEFAULT now(),
  old_values    jsonb        NULL,
  new_values    jsonb        NULL,
  request_id    uuid         NULL,
  ip_address    inet         NULL
);
CREATE INDEX ix_audit_log_tenant_id_changed_at ON audit.audit_log (tenant_id, changed_at DESC);
CREATE INDEX ix_audit_log_record               ON audit.audit_log (schema_name, table_name, record_id);
COMMENT ON TABLE audit.audit_log IS '@context:audit Bitácora inmutable de cambios (INSERT/UPDATE/DELETE) por trigger.';

-- Bitácora de accesos/lecturas sensibles (emisión de URLs prefirmadas, lectura de PII).
CREATE TABLE audit.data_access_log (
  id            uuid         PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid         NOT NULL,
  actor_user_id uuid         NULL,
  schema_name   text         NOT NULL,
  table_name    text         NOT NULL,
  record_id     uuid         NULL,
  access_type   text         NOT NULL CHECK (access_type IN ('read','download','presign','export')),
  purpose       text         NULL,
  accessed_at   timestamptz  NOT NULL DEFAULT now(),
  request_id    uuid         NULL,
  ip_address    inet         NULL
);
CREATE INDEX ix_data_access_log_tenant_id_accessed_at ON audit.data_access_log (tenant_id, accessed_at DESC);
COMMENT ON TABLE audit.data_access_log IS '@context:audit Trazabilidad de accesos a datos sensibles (Habeas Data / ADR-0006).';

-- Sentinela de plataforma para auditar config GLOBAL (sin tenant). audit_log.tenant_id no tiene FK.
-- '00000000-0000-7000-8000-000000000000' = "FLIT Platform".

-- -------------------------------------------------------------------------------------
-- 5. Funciones de trigger genéricas
-- -------------------------------------------------------------------------------------

-- 5.1 Concurrencia optimista + touch updated_at (tablas de negocio con row_version)
CREATE OR REPLACE FUNCTION audit.increment_row_version()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
  NEW.row_version := OLD.row_version + 1;
  NEW.updated_at  := now();
  RETURN NEW;
END;
$$;

-- 5.2 Touch updated_at (catálogos y tablas sin row_version)
CREATE OR REPLACE FUNCTION audit.touch_updated_at()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$;

-- 5.3 Auditoría genérica AFTER I/U/D → audit.audit_log
--     Resuelve tenant/usuario desde la fila o desde variables de sesión de la app.
CREATE OR REPLACE FUNCTION audit.log_change()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
  v_old jsonb;
  v_new jsonb;
  v_op  char(1);
  v_record_id uuid;
  v_tenant_id uuid;
  v_changed_by uuid;
  v_request_id uuid;
  v_ip inet;
  c_platform constant uuid := '00000000-0000-7000-8000-000000000000';
BEGIN
  IF (TG_OP = 'INSERT') THEN
    v_op := 'I'; v_new := to_jsonb(NEW); v_old := NULL;
  ELSIF (TG_OP = 'UPDATE') THEN
    v_op := 'U'; v_new := to_jsonb(NEW); v_old := to_jsonb(OLD);
  ELSE
    v_op := 'D'; v_new := NULL; v_old := to_jsonb(OLD);
  END IF;

  v_record_id := COALESCE((v_new->>'id')::uuid, (v_old->>'id')::uuid);

  v_tenant_id := COALESCE(
    (v_new->>'tenant_id')::uuid,
    (v_old->>'tenant_id')::uuid,
    NULLIF(current_setting('app.current_tenant_id', true), '')::uuid,
    c_platform
  );

  v_changed_by := COALESCE(
    NULLIF(current_setting('app.current_user_id', true), '')::uuid,
    (v_new->>'updated_by')::uuid,
    (v_new->>'created_by')::uuid,
    (v_old->>'updated_by')::uuid,
    c_platform
  );

  v_request_id := NULLIF(current_setting('app.request_id', true), '')::uuid;
  BEGIN
    v_ip := NULLIF(current_setting('app.client_ip', true), '')::inet;
  EXCEPTION WHEN others THEN
    v_ip := NULL;
  END;

  INSERT INTO audit.audit_log (
    tenant_id, schema_name, table_name, record_id, operation,
    changed_by, old_values, new_values, request_id, ip_address
  ) VALUES (
    v_tenant_id, TG_TABLE_SCHEMA, TG_TABLE_NAME, v_record_id, v_op,
    v_changed_by, v_old, v_new, v_request_id, v_ip
  );

  IF (TG_OP = 'DELETE') THEN RETURN OLD; ELSE RETURN NEW; END IF;
END;
$$;

COMMENT ON FUNCTION audit.log_change() IS
  'Trigger genérico de auditoría. Aplicar AFTER INSERT OR UPDATE OR DELETE en tablas de negocio y config.';

-- -------------------------------------------------------------------------------------
-- 6. Validadores del árbol de reglas no-code (#9410). Usados en CHECK de ot_rules y
--    procedures_config.rules para que la BD rechace árboles malformados (sin SQL, solo claves).
--    Operadores y tipos de acción = conjuntos CERRADOS (sin inyección).
-- -------------------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.is_valid_rule_condition(node jsonb)
RETURNS boolean LANGUAGE plpgsql IMMUTABLE AS $$
DECLARE
  child jsonb;
  op    text;
BEGIN
  IF node IS NULL OR node = 'null'::jsonb THEN
    RETURN true;  -- condición vacía = regla siempre verdadera
  END IF;
  IF jsonb_typeof(node) <> 'object' THEN RETURN false; END IF;

  -- Grupo lógico AND/OR
  IF node ? 'op' THEN
    op := node->>'op';
    IF op NOT IN ('AND','OR') THEN RETURN false; END IF;
    IF jsonb_typeof(node->'children') <> 'array' THEN RETURN false; END IF;
    FOR child IN SELECT * FROM jsonb_array_elements(node->'children') LOOP
      IF NOT public.is_valid_rule_condition(child) THEN RETURN false; END IF;
    END LOOP;
    RETURN true;
  END IF;

  -- Hoja: field + operator [+ value]
  IF NOT (node ? 'field' AND node ? 'operator') THEN RETURN false; END IF;
  IF (node->>'operator') NOT IN
     ('equal','notEqual','greater','less','contains','isEmpty','isNotEmpty') THEN
    RETURN false;
  END IF;
  IF (node->>'operator') NOT IN ('isEmpty','isNotEmpty') THEN
    IF jsonb_typeof(node->'value') <> 'object' THEN RETURN false; END IF;
    IF (node->'value'->>'kind') NOT IN ('static','field') THEN RETURN false; END IF;
  END IF;
  RETURN true;
END;
$$;

CREATE OR REPLACE FUNCTION public.is_valid_rule_actions(actions jsonb)
RETURNS boolean LANGUAGE plpgsql IMMUTABLE AS $$
DECLARE a jsonb;
BEGIN
  IF actions IS NULL OR actions = 'null'::jsonb THEN RETURN true; END IF;
  IF jsonb_typeof(actions) <> 'array' THEN RETURN false; END IF;
  FOR a IN SELECT * FROM jsonb_array_elements(actions) LOOP
    IF jsonb_typeof(a) <> 'object' THEN RETURN false; END IF;
    IF (a->>'type') NOT IN ('popup_modal','inject_section','call_endpoint','block') THEN
      RETURN false;
    END IF;
  END LOOP;
  RETURN true;
END;
$$;

COMMENT ON FUNCTION public.is_valid_rule_condition(jsonb) IS 'Valida estructuralmente el árbol AND/OR de condiciones (#9410).';
COMMENT ON FUNCTION public.is_valid_rule_actions(jsonb)   IS 'Valida la lista tipada de acciones (#9410).';

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP FUNCTION IF EXISTS audit.log_change();
-- DROP FUNCTION IF EXISTS audit.touch_updated_at();
-- DROP FUNCTION IF EXISTS audit.increment_row_version();
-- DROP TABLE IF EXISTS audit.data_access_log;
-- DROP TABLE IF EXISTS audit.audit_log;
-- DROP SCHEMA IF EXISTS audit, dashboard, procedures, identity_verification, integrations,
--   procedures_config, ot, companies, files, identity, catalogs CASCADE;
-- DROP FUNCTION IF EXISTS public.uuidv7();
