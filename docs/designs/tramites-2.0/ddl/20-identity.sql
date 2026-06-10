-- =====================================================================================
-- FLIT 2.0 · DDL 20 — Identidad, autenticación y gobernanza multi-tenant (#9370)
-- Tenant = Compañía B2B. Reconstruido desde cero.
-- Patrón RLS estándar: tenant match en escritura; lectura con bypass SuperAdmin.
-- Bootstrap: tenants/users permiten created_by/updated_by NULL (excepción documentada ADR-0009).
-- =====================================================================================
SET search_path TO identity;

-- Helper de SuperAdmin (lee GUC app.is_super_admin). Usado por políticas RLS de todos los schemas.
CREATE OR REPLACE FUNCTION identity.is_super_admin()
RETURNS boolean LANGUAGE sql STABLE AS $$
  SELECT COALESCE(current_setting('app.is_super_admin', true) = 'true', false);
$$;

-- -------------------------------------------------------------------------------------
-- tenants — raíz multi-tenant (excepción §6.1: sin tenant_id). RLS por id propio + SuperAdmin.
-- -------------------------------------------------------------------------------------
CREATE TABLE tenants (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  name          text        NOT NULL,
  nit           text        NOT NULL,
  slug          text        NOT NULL,
  status        text        NOT NULL DEFAULT 'active'
                            CHECK (status IN ('active','inactive','suspended')),
  settings      jsonb       NOT NULL DEFAULT '{}'::jsonb,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT uq_tenants_nit  UNIQUE (nit),
  CONSTRAINT uq_tenants_slug UNIQUE (slug),
  CONSTRAINT ck_tenants_slug_format CHECK (slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$')
);
ALTER TABLE tenants ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_self_isolation ON tenants
  USING (id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (identity.is_super_admin());
CREATE TRIGGER tr_tenants_before_update_row_version
  BEFORE UPDATE ON tenants FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_tenants_audit
  AFTER INSERT OR UPDATE OR DELETE ON tenants FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON TABLE tenants IS '@context:identity @entity:compañía-tenant Raíz de aislamiento multi-tenant.';
COMMENT ON COLUMN tenants.nit IS '@pii:low Identificación tributaria de la compañía.';

-- -------------------------------------------------------------------------------------
-- users — cuentas (3 perfiles vía rol). Email global único → login unificado (#9370 RF-1.1).
-- -------------------------------------------------------------------------------------
CREATE TABLE users (
  id                   uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id            uuid        NOT NULL,
  email                citext      NOT NULL,
  password_hash        text        NULL,
  password_algo        text        NOT NULL DEFAULT 'argon2id',
  account_state        text        NOT NULL DEFAULT 'inactive'
                       CHECK (account_state IN ('active','inactive','temp_blocked','permanent_blocked')),
  blocked_until        timestamptz NULL,
  block_reason         text        NULL,
  must_change_password boolean     NOT NULL DEFAULT false,
  failed_attempt_count integer     NOT NULL DEFAULT 0,
  last_login_at        timestamptz NULL,
  created_at           timestamptz NOT NULL DEFAULT now(),
  created_by           uuid        NULL,
  updated_at           timestamptz NOT NULL DEFAULT now(),
  updated_by           uuid        NULL,
  deleted_at           timestamptz NULL,
  deleted_by           uuid        NULL,
  row_version          integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_users_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_users_email UNIQUE (email),
  CONSTRAINT ck_users_blocked_until CHECK (
    (account_state = 'temp_blocked' AND blocked_until IS NOT NULL)
    OR (account_state <> 'temp_blocked')
  )
);
CREATE INDEX ix_users_tenant_id ON users (tenant_id);
CREATE INDEX ix_users_tenant_id_account_state ON users (tenant_id, account_state) WHERE deleted_at IS NULL;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON users
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_users_before_update_row_version
  BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_users_audit
  AFTER INSERT OR UPDATE OR DELETE ON users FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN users.email IS '@pii:medium';
COMMENT ON COLUMN users.password_hash IS '@pii:high Nunca en logs. Hash Argon2id/bcrypt.';

-- FKs diferidas de bootstrap (tenants/users.created_by → users)
ALTER TABLE tenants
  ADD CONSTRAINT fk_tenants_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  ADD CONSTRAINT fk_tenants_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE SET NULL;
ALTER TABLE users
  ADD CONSTRAINT fk_users_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  ADD CONSTRAINT fk_users_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE SET NULL;

-- Función de autenticación cross-tenant (bypassa RLS): resuelve la cuenta por email para el login.
CREATE OR REPLACE FUNCTION identity.find_user_for_auth(p_email citext)
RETURNS TABLE (id uuid, tenant_id uuid, password_hash text, password_algo text, account_state text, blocked_until timestamptz)
LANGUAGE sql SECURITY DEFINER STABLE
SET search_path = identity, pg_temp
AS $$
  SELECT u.id, u.tenant_id, u.password_hash, u.password_algo, u.account_state, u.blocked_until
  FROM identity.users u
  WHERE u.email = p_email AND u.deleted_at IS NULL;
$$;
COMMENT ON FUNCTION identity.find_user_for_auth(citext) IS
  'Lectura mínima para autenticación previa al contexto de tenant. Solo la usa el flujo de login.';

-- -------------------------------------------------------------------------------------
-- profiles — zona perfil autoservicio (RF-4.3), 1:1 con user
-- -------------------------------------------------------------------------------------
CREATE TABLE profiles (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NOT NULL,
  user_id       uuid        NOT NULL,
  full_name     text        NOT NULL,
  phone         text        NULL,
  address       text        NULL,
  locale        text        NOT NULL DEFAULT 'es-CO',
  timezone      text        NOT NULL DEFAULT 'America/Bogota',
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NOT NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NOT NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_profiles_tenants  FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_profiles_users    FOREIGN KEY (user_id)   REFERENCES users (id)   ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_profiles_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_profiles_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_profiles_user_id UNIQUE (user_id)
);
CREATE INDEX ix_profiles_tenant_id ON profiles (tenant_id);
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON profiles
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_profiles_before_update_row_version
  BEFORE UPDATE ON profiles FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_profiles_audit
  AFTER INSERT OR UPDATE OR DELETE ON profiles FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN profiles.full_name IS '@pii:low';
COMMENT ON COLUMN profiles.phone     IS '@pii:medium';
COMMENT ON COLUMN profiles.address   IS '@pii:high';

-- -------------------------------------------------------------------------------------
-- permissions — slugs dinámicos GLOBALES (modulo.tramites.crud-total). Tenant-exento (catálogo).
-- -------------------------------------------------------------------------------------
CREATE TABLE permissions (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  slug          text        NOT NULL,
  module        text        NOT NULL,
  action        text        NOT NULL,
  description   text        NULL,
  is_assignable boolean     NOT NULL DEFAULT true,
  is_system     boolean     NOT NULL DEFAULT false,
  is_active     boolean     NOT NULL DEFAULT true,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_permissions_slug UNIQUE (slug),
  CONSTRAINT ck_permissions_slug_format CHECK (slug ~ '^[a-z0-9]+(\.[a-z0-9-]+)+$')
);
CREATE TRIGGER tr_permissions_before_update_touch
  BEFORE UPDATE ON permissions FOR EACH ROW EXECUTE FUNCTION audit.touch_updated_at();
COMMENT ON TABLE permissions IS '@context:identity Catálogo global de permisos-slug (RF-2.1). Definidos por SuperAdmin.';

INSERT INTO permissions (slug, module, action, is_system) VALUES
  ('modulo.tramites.crud-total','tramites','crud-total', true),
  ('modulo.tramites.ver','tramites','ver', true),
  ('modulo.companias.crud-total','companias','crud-total', true),
  ('modulo.ot.crud-total','ot','crud-total', true),
  ('modulo.parametrizacion.crud-total','parametrizacion','crud-total', true),
  ('modulo.dashboard.ver','dashboard','ver', true),
  ('modulo.dashboard.exportar','dashboard','exportar', true);

-- -------------------------------------------------------------------------------------
-- roles — maestros globales (tenant_id NULL) + locales por tenant
-- -------------------------------------------------------------------------------------
CREATE TABLE roles (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NULL,
  slug          text        NOT NULL,
  name          text        NOT NULL,
  scope         text        NOT NULL CHECK (scope IN ('global','tenant')),
  is_system     boolean     NOT NULL DEFAULT false,
  description   text        NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_roles_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_roles_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_roles_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT ck_roles_scope_tenant CHECK (
    (scope = 'global' AND tenant_id IS NULL) OR (scope = 'tenant' AND tenant_id IS NOT NULL)
  )
);
CREATE UNIQUE INDEX uq_roles_global_slug ON roles (slug) WHERE tenant_id IS NULL AND deleted_at IS NULL;
CREATE UNIQUE INDEX uq_roles_tenant_slug ON roles (tenant_id, slug) WHERE tenant_id IS NOT NULL AND deleted_at IS NULL;
CREATE INDEX ix_roles_tenant_id ON roles (tenant_id);
ALTER TABLE roles ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_or_global_isolation ON roles
  USING (tenant_id IS NULL OR tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (
    (tenant_id IS NULL AND identity.is_super_admin())
    OR tenant_id = current_setting('app.current_tenant_id', true)::uuid
  );
CREATE TRIGGER tr_roles_before_update_row_version
  BEFORE UPDATE ON roles FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_roles_audit
  AFTER INSERT OR UPDATE OR DELETE ON roles FOR EACH ROW EXECUTE FUNCTION audit.log_change();

INSERT INTO roles (id, tenant_id, slug, name, scope, is_system) VALUES
  ('00000000-0000-7000-8000-0000000000a1', NULL, 'super-admin',  'Super Administrador', 'global', true),
  ('00000000-0000-7000-8000-0000000000a2', NULL, 'tenant-admin', 'Administrador de Compañía', 'global', true),
  ('00000000-0000-7000-8000-0000000000a3', NULL, 'colaborador',  'Colaborador', 'global', true);

-- -------------------------------------------------------------------------------------
-- role_permissions — junction rol↔permiso (global o por tenant, espejo del scope del rol)
-- -------------------------------------------------------------------------------------
CREATE TABLE role_permissions (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NULL,
  role_id       uuid        NOT NULL,
  permission_id uuid        NOT NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_role_permissions_roles FOREIGN KEY (role_id) REFERENCES roles (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_role_permissions_permissions FOREIGN KEY (permission_id) REFERENCES permissions (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_role_permissions_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT uq_role_permissions_role_permission UNIQUE (role_id, permission_id)
);
CREATE INDEX ix_role_permissions_role_id ON role_permissions (role_id);
CREATE INDEX ix_role_permissions_permission_id ON role_permissions (permission_id);
ALTER TABLE role_permissions ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_or_global_isolation ON role_permissions
  USING (tenant_id IS NULL OR tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (
    (tenant_id IS NULL AND identity.is_super_admin())
    OR tenant_id = current_setting('app.current_tenant_id', true)::uuid
  );
CREATE TRIGGER tr_role_permissions_before_update_row_version
  BEFORE UPDATE ON role_permissions FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_role_permissions_audit
  AFTER INSERT OR UPDATE OR DELETE ON role_permissions FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- user_roles — asignación usuario↔rol dentro de un tenant (anti escalamiento horizontal)
-- -------------------------------------------------------------------------------------
CREATE TABLE user_roles (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NOT NULL,
  user_id       uuid        NOT NULL,
  role_id       uuid        NOT NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NOT NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NOT NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_user_roles_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_user_roles_users   FOREIGN KEY (user_id)   REFERENCES users (id)   ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_user_roles_roles   FOREIGN KEY (role_id)   REFERENCES roles (id)   ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_user_roles_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_user_roles_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_user_roles_tenant_user_role UNIQUE (tenant_id, user_id, role_id)
);
CREATE INDEX ix_user_roles_tenant_id_user_id ON user_roles (tenant_id, user_id);
CREATE INDEX ix_user_roles_role_id ON user_roles (role_id);
ALTER TABLE user_roles ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON user_roles
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_user_roles_before_update_row_version
  BEFORE UPDATE ON user_roles FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_user_roles_audit
  AFTER INSERT OR UPDATE OR DELETE ON user_roles FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- onboarding_invitations — alta sin contraseña; enlace firmado 24h, un solo uso (RF-4.1)
-- -------------------------------------------------------------------------------------
CREATE TABLE onboarding_invitations (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NOT NULL,
  email         citext      NOT NULL,
  invited_role_id uuid      NOT NULL,
  token_hash    text        NOT NULL,
  signature     text        NOT NULL,
  status        text        NOT NULL DEFAULT 'pending'
                            CHECK (status IN ('pending','consumed','expired','revoked')),
  expires_at    timestamptz NOT NULL,
  consumed_at   timestamptz NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NOT NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NOT NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_onboarding_invitations_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_onboarding_invitations_roles   FOREIGN KEY (invited_role_id) REFERENCES roles (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_onboarding_invitations_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_onboarding_invitations_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_onboarding_invitations_token UNIQUE (token_hash)
);
CREATE UNIQUE INDEX uq_onboarding_invitations_tenant_email_pending
  ON onboarding_invitations (tenant_id, email) WHERE status = 'pending';
CREATE INDEX ix_onboarding_invitations_tenant_id ON onboarding_invitations (tenant_id);
ALTER TABLE onboarding_invitations ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON onboarding_invitations
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_onboarding_invitations_before_update_row_version
  BEFORE UPDATE ON onboarding_invitations FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_onboarding_invitations_audit
  AFTER INSERT OR UPDATE OR DELETE ON onboarding_invitations FOR EACH ROW EXECUTE FUNCTION audit.log_change();
COMMENT ON COLUMN onboarding_invitations.token_hash IS '@pii:high Solo hash del token; nunca el token en claro.';

-- -------------------------------------------------------------------------------------
-- password_reset_tokens — restablecimiento credenciales (un solo uso)
-- -------------------------------------------------------------------------------------
CREATE TABLE password_reset_tokens (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NOT NULL,
  user_id       uuid        NOT NULL,
  token_hash    text        NOT NULL,
  expires_at    timestamptz NOT NULL,
  consumed_at   timestamptz NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NOT NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NOT NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_password_reset_tokens_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_password_reset_tokens_users   FOREIGN KEY (user_id)   REFERENCES users (id)   ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_password_reset_tokens_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_password_reset_tokens_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_password_reset_tokens_token UNIQUE (token_hash)
);
CREATE INDEX ix_password_reset_tokens_tenant_id_user_id ON password_reset_tokens (tenant_id, user_id);
ALTER TABLE password_reset_tokens ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON password_reset_tokens
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_password_reset_tokens_before_update_row_version
  BEFORE UPDATE ON password_reset_tokens FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
COMMENT ON COLUMN password_reset_tokens.token_hash IS '@pii:high';

-- -------------------------------------------------------------------------------------
-- refresh_tokens — sesiones (tokens HttpOnly/refresh, RF seguridad CF-H2)
-- -------------------------------------------------------------------------------------
CREATE TABLE refresh_tokens (
  id            uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id     uuid        NOT NULL,
  user_id       uuid        NOT NULL,
  token_hash    text        NOT NULL,
  user_agent    text        NULL,
  ip_address    inet        NULL,
  expires_at    timestamptz NOT NULL,
  revoked_at    timestamptz NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  created_by    uuid        NOT NULL,
  updated_at    timestamptz NOT NULL DEFAULT now(),
  updated_by    uuid        NOT NULL,
  deleted_at    timestamptz NULL,
  deleted_by    uuid        NULL,
  row_version   integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_refresh_tokens_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_refresh_tokens_users   FOREIGN KEY (user_id)   REFERENCES users (id)   ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_refresh_tokens_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_refresh_tokens_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_refresh_tokens_token UNIQUE (token_hash)
);
CREATE INDEX ix_refresh_tokens_tenant_id_user_id ON refresh_tokens (tenant_id, user_id) WHERE revoked_at IS NULL;
ALTER TABLE refresh_tokens ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON refresh_tokens
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_refresh_tokens_before_update_row_version
  BEFORE UPDATE ON refresh_tokens FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
COMMENT ON COLUMN refresh_tokens.token_hash IS '@pii:high';

-- -------------------------------------------------------------------------------------
-- password_policies — política de contraseñas por tenant (RF-4.2)
-- -------------------------------------------------------------------------------------
CREATE TABLE password_policies (
  id                uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id         uuid        NOT NULL,
  min_length        integer     NOT NULL DEFAULT 12 CHECK (min_length >= 8),
  require_uppercase boolean     NOT NULL DEFAULT true,
  require_lowercase boolean     NOT NULL DEFAULT true,
  require_digit     boolean     NOT NULL DEFAULT true,
  require_symbol    boolean     NOT NULL DEFAULT true,
  history_count     integer     NOT NULL DEFAULT 5,
  max_age_days      integer     NOT NULL DEFAULT 90,
  lockout_threshold integer     NOT NULL DEFAULT 5,
  lockout_minutes   integer     NOT NULL DEFAULT 15,
  created_at        timestamptz NOT NULL DEFAULT now(),
  created_by        uuid        NOT NULL,
  updated_at        timestamptz NOT NULL DEFAULT now(),
  updated_by        uuid        NOT NULL,
  deleted_at        timestamptz NULL,
  deleted_by        uuid        NULL,
  row_version       integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_password_policies_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_password_policies_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_password_policies_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT uq_password_policies_tenant UNIQUE (tenant_id)
);
ALTER TABLE password_policies ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON password_policies
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_password_policies_before_update_row_version
  BEFORE UPDATE ON password_policies FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_password_policies_audit
  AFTER INSERT OR UPDATE OR DELETE ON password_policies FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- login_attempts — auditoría de intentos y bloqueos (CF-B9). Append-only; sin RLS (audit infra).
-- -------------------------------------------------------------------------------------
CREATE TABLE login_attempts (
  id              uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id       uuid        NULL,
  user_id         uuid        NULL,
  email_attempted citext      NOT NULL,
  succeeded       boolean     NOT NULL,
  failure_reason  text        NULL CHECK (failure_reason IN
                    ('invalid_credentials','inactive','temp_blocked','permanent_blocked','rate_limited','unknown_user')),
  ip_address      inet        NULL,
  user_agent      text        NULL,
  attempted_at    timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_login_attempts_email_attempted_at ON login_attempts (email_attempted, attempted_at DESC);
CREATE INDEX ix_login_attempts_tenant_id_attempted_at ON login_attempts (tenant_id, attempted_at DESC);
COMMENT ON TABLE login_attempts IS '@context:identity Bitácora de seguridad (append-only). Particionar por mes en producción.';
COMMENT ON COLUMN login_attempts.email_attempted IS '@pii:medium';

-- -------------------------------------------------------------------------------------
-- support_tickets — soporte con contexto del colaborador (RF-4.4)
-- -------------------------------------------------------------------------------------
CREATE TABLE support_tickets (
  id                  uuid        PRIMARY KEY DEFAULT uuidv7(),
  tenant_id           uuid        NOT NULL,
  reporter_user_id    uuid        NOT NULL,
  assigned_to_user_id uuid        NULL,
  subject             text        NOT NULL,
  body                text        NOT NULL,
  category            text        NOT NULL DEFAULT 'general',
  status              text        NOT NULL DEFAULT 'open'
                                  CHECK (status IN ('open','in_progress','resolved','closed')),
  created_at          timestamptz NOT NULL DEFAULT now(),
  created_by          uuid        NOT NULL,
  updated_at          timestamptz NOT NULL DEFAULT now(),
  updated_by          uuid        NOT NULL,
  deleted_at          timestamptz NULL,
  deleted_by          uuid        NULL,
  row_version         integer     NOT NULL DEFAULT 1,
  CONSTRAINT fk_support_tickets_tenants FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_support_tickets_users_reporter FOREIGN KEY (reporter_user_id) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_support_tickets_users_assignee FOREIGN KEY (assigned_to_user_id) REFERENCES users (id) ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT fk_support_tickets_users_creator FOREIGN KEY (created_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_support_tickets_users_updater FOREIGN KEY (updated_by) REFERENCES users (id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX ix_support_tickets_tenant_id_status ON support_tickets (tenant_id, status) WHERE deleted_at IS NULL;
ALTER TABLE support_tickets ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON support_tickets
  USING (tenant_id = current_setting('app.current_tenant_id', true)::uuid OR identity.is_super_admin())
  WITH CHECK (tenant_id = current_setting('app.current_tenant_id', true)::uuid);
CREATE TRIGGER tr_support_tickets_before_update_row_version
  BEFORE UPDATE ON support_tickets FOR EACH ROW EXECUTE FUNCTION audit.increment_row_version();
CREATE TRIGGER tr_support_tickets_audit
  AFTER INSERT OR UPDATE OR DELETE ON support_tickets FOR EACH ROW EXECUTE FUNCTION audit.log_change();

-- -------------------------------------------------------------------------------------
-- Bootstrap: tenant de plataforma + usuario de sistema (para created_by de config global y seeds)
-- -------------------------------------------------------------------------------------
INSERT INTO tenants (id, name, nit, slug, status, created_by)
VALUES ('00000000-0000-7000-8000-000000000000', 'FLIT Platform', '000000000', 'flit-platform', 'active', NULL);

INSERT INTO users (id, tenant_id, email, password_hash, account_state, created_by)
VALUES ('00000000-0000-7000-8000-000000000001', '00000000-0000-7000-8000-000000000000',
        'system@flit.co', NULL, 'active', NULL);

INSERT INTO user_roles (tenant_id, user_id, role_id, created_by, updated_by)
VALUES ('00000000-0000-7000-8000-000000000000', '00000000-0000-7000-8000-000000000001',
        '00000000-0000-7000-8000-0000000000a1', '00000000-0000-7000-8000-000000000001',
        '00000000-0000-7000-8000-000000000001');

-- Seeds role_permissions (slugs efectivos post-login — IDN-02 / #9415)
INSERT INTO role_permissions (tenant_id, role_id, permission_id, created_by, updated_by)
SELECT NULL, '00000000-0000-7000-8000-0000000000a1', p.id,
       '00000000-0000-7000-8000-000000000001', '00000000-0000-7000-8000-000000000001'
FROM permissions p;

INSERT INTO role_permissions (tenant_id, role_id, permission_id, created_by, updated_by)
SELECT NULL, '00000000-0000-7000-8000-0000000000a2', p.id,
       '00000000-0000-7000-8000-000000000001', '00000000-0000-7000-8000-000000000001'
FROM permissions p
WHERE p.slug IN (
  'modulo.tramites.crud-total', 'modulo.tramites.ver',
  'modulo.companias.crud-total', 'modulo.parametrizacion.crud-total',
  'modulo.dashboard.ver', 'modulo.dashboard.exportar'
);

INSERT INTO role_permissions (tenant_id, role_id, permission_id, created_by, updated_by)
SELECT NULL, '00000000-0000-7000-8000-0000000000a3', p.id,
       '00000000-0000-7000-8000-000000000001', '00000000-0000-7000-8000-000000000001'
FROM permissions p
WHERE p.slug IN ('modulo.tramites.ver', 'modulo.dashboard.ver');

-- =====================================================================================
-- DOWN (reversa)
-- =====================================================================================
-- DROP TABLE IF EXISTS identity.support_tickets, identity.login_attempts, identity.password_policies,
--   identity.refresh_tokens, identity.password_reset_tokens, identity.onboarding_invitations,
--   identity.user_roles, identity.role_permissions, identity.roles, identity.permissions,
--   identity.profiles, identity.users, identity.tenants CASCADE;
-- DROP FUNCTION IF EXISTS identity.find_user_for_auth(citext);
-- DROP FUNCTION IF EXISTS identity.is_super_admin();
