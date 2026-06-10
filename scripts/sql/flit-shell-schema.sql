-- Schema del shell FLIT (identity + rbac + notifications).
-- Sin migraciones EF: aplicar con reset-local-database.ps1/.sh o pnpm db:shell-schema.

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'identity') THEN
        CREATE SCHEMA identity;
    END IF;
END $EF$;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'rbac') THEN
        CREATE SCHEMA rbac;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'notifications') THEN
        CREATE SCHEMA notifications;
    END IF;
END $EF$;

CREATE TABLE identity.identity_credentials (
    user_id uuid NOT NULL,
    password_hash character varying(512) NOT NULL,
    cambiado_en timestamp with time zone NOT NULL,
    CONSTRAINT "PK_identity_credentials" PRIMARY KEY (user_id)
);

CREATE TABLE identity.identity_refresh_tokens (
    jti uuid NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    reason character varying(100) NOT NULL,
    revoked_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_identity_refresh_tokens" PRIMARY KEY (jti)
);

CREATE TABLE identity.identity_users (
    id uuid NOT NULL,
    email character varying(255) NOT NULL,
    email_verificado boolean NOT NULL DEFAULT FALSE,
    documento_tipo character varying(10) NOT NULL,
    documento_numero character varying(20) NOT NULL,
    nombres character varying(100) NOT NULL,
    apellidos character varying(100) NOT NULL,
    fecha_nacimiento date,
    telefono character varying(20),
    rol character varying(30) NOT NULL,
    organismo_id uuid,
    activo boolean NOT NULL DEFAULT TRUE,
    bloqueado boolean NOT NULL DEFAULT FALSE,
    bloqueado_hasta timestamp with time zone,
    intentos_fallidos integer NOT NULL DEFAULT 0,
    ultimo_login timestamp with time zone,
    habeas_data_consentimiento boolean NOT NULL DEFAULT FALSE,
    habeas_data_fecha timestamp with time zone NOT NULL,
    habeas_data_politica_version character varying(20) NOT NULL,
    creado_en timestamp with time zone NOT NULL,
    actualizado_en timestamp with time zone NOT NULL,
    CONSTRAINT "PK_identity_users" PRIMARY KEY (id)
);

CREATE TABLE rbac.menu_items (
    id uuid NOT NULL,
    code character varying(100) NOT NULL,
    parent_id uuid,
    label character varying(150) NOT NULL,
    icon character varying(50),
    frontend_path character varying(255),
    sort_order smallint NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT TRUE,
    is_visible boolean NOT NULL DEFAULT TRUE,
    is_separator boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_menu_items" PRIMARY KEY (id),
    CONSTRAINT fk_mi_parent FOREIGN KEY (parent_id) REFERENCES rbac.menu_items (id) ON DELETE SET NULL
);

CREATE TABLE notifications.notification_delivery (
    id uuid NOT NULL,
    notification_type character varying(40) NOT NULL,
    channel character varying(20) NOT NULL,
    recipient_user_id uuid,
    recipient_masked character varying(255),
    procedure_id uuid,
    status character varying(15) NOT NULL,
    subject character varying(300),
    body_excerpt character varying(200),
    error_message character varying(1000),
    created_at timestamp with time zone NOT NULL,
    sent_at timestamp with time zone,
    CONSTRAINT "PK_notification_delivery" PRIMARY KEY (id)
);

CREATE TABLE rbac.permissions (
    id uuid NOT NULL,
    code character varying(100) NOT NULL,
    name character varying(150) NOT NULL,
    description character varying(500),
    module character varying(50) NOT NULL,
    is_system boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_permissions" PRIMARY KEY (id)
);

CREATE TABLE rbac.roles (
    id uuid NOT NULL,
    code character varying(50) NOT NULL,
    name character varying(100) NOT NULL,
    description character varying(500),
    is_system boolean NOT NULL DEFAULT FALSE,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_roles" PRIMARY KEY (id)
);

CREATE TABLE identity.sync_inconsistencies (
    id bigint GENERATED ALWAYS AS IDENTITY,
    type character varying(40) NOT NULL,
    user_id uuid,
    cognito_sub character varying(64),
    email character varying(255),
    detail text,
    resolved_at timestamp with time zone,
    detected_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_sync_inconsistencies" PRIMARY KEY (id),
    CONSTRAINT chk_sync_type CHECK (type IN ('COGNITO_ORPHAN','DB_ORPHAN','STATUS_DESYNCED'))
);

CREATE TABLE identity.users (
    id uuid NOT NULL,
    cognito_sub character varying(64),
    email character varying(255) NOT NULL,
    full_name character varying(200) NOT NULL,
    document_type character varying(10),
    document_number character varying(20),
    phone character varying(20),
    status character varying(20) NOT NULL,
    mfa_enabled boolean NOT NULL DEFAULT FALSE,
    mfa_secret_ciphertext bytea,
    mfa_secret_nonce bytea,
    mfa_secret_key_id character varying(64),
    mfa_enabled_at timestamp with time zone,
    last_login_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    deleted_at timestamp with time zone,
    created_by_user_id uuid,
    CONSTRAINT "PK_users" PRIMARY KEY (id),
    CONSTRAINT chk_users_status CHECK (status IN ('ACTIVE','INACTIVE','BLOCKED','DELETED')),
    CONSTRAINT fk_users_created_by FOREIGN KEY (created_by_user_id) REFERENCES identity.users (id) ON DELETE SET NULL
);

CREATE TABLE rbac.role_menu_items (
    role_id uuid NOT NULL,
    menu_item_id uuid NOT NULL,
    CONSTRAINT "PK_role_menu_items" PRIMARY KEY (role_id, menu_item_id),
    CONSTRAINT fk_rmi_mi FOREIGN KEY (menu_item_id) REFERENCES rbac.menu_items (id) ON DELETE CASCADE,
    CONSTRAINT fk_rmi_role FOREIGN KEY (role_id) REFERENCES rbac.roles (id) ON DELETE CASCADE
);

CREATE TABLE rbac.role_permissions (
    role_id uuid NOT NULL,
    permission_id uuid NOT NULL,
    assigned_at timestamp with time zone NOT NULL,
    assigned_by_user_id uuid,
    CONSTRAINT "PK_role_permissions" PRIMARY KEY (role_id, permission_id),
    CONSTRAINT fk_rp_permission FOREIGN KEY (permission_id) REFERENCES rbac.permissions (id) ON DELETE CASCADE,
    CONSTRAINT fk_rp_role FOREIGN KEY (role_id) REFERENCES rbac.roles (id) ON DELETE CASCADE
);

CREATE TABLE identity.password_reset_tokens (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    token_hash character varying(64) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    used_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_password_reset_tokens" PRIMARY KEY (id),
    CONSTRAINT fk_prt_user FOREIGN KEY (user_id) REFERENCES identity.users (id) ON DELETE CASCADE
);

CREATE TABLE identity.user_audit_log (
    id bigint GENERATED ALWAYS AS IDENTITY,
    user_id uuid NOT NULL,
    event character varying(50) NOT NULL,
    metadata jsonb,
    executed_by_user_id uuid,
    ip_address character varying(45),
    user_agent text,
    occurred_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_user_audit_log" PRIMARY KEY (id),
    CONSTRAINT fk_ual_user FOREIGN KEY (user_id) REFERENCES identity.users (id)
);

CREATE TABLE rbac.user_roles (
    user_id uuid NOT NULL,
    role_id uuid NOT NULL,
    assigned_at timestamp with time zone NOT NULL,
    assigned_by_user_id uuid,
    CONSTRAINT "PK_user_roles" PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_ur_role FOREIGN KEY (role_id) REFERENCES rbac.roles (id) ON DELETE CASCADE,
    CONSTRAINT fk_ur_user FOREIGN KEY (user_id) REFERENCES identity.users (id) ON DELETE CASCADE
);

CREATE INDEX ix_identity_refresh_tokens_expires_at ON identity.identity_refresh_tokens (expires_at);

CREATE INDEX ix_identity_users_activo ON identity.identity_users (activo);

CREATE UNIQUE INDEX ix_identity_users_documento ON identity.identity_users (documento_tipo, documento_numero);

CREATE UNIQUE INDEX ix_identity_users_email ON identity.identity_users (email);

CREATE UNIQUE INDEX ix_menu_items_code ON rbac.menu_items (code);

CREATE INDEX ix_menu_items_parent ON rbac.menu_items (parent_id);

CREATE INDEX ix_menu_items_sort ON rbac.menu_items (sort_order);

CREATE INDEX ix_notification_delivery_created_at ON notifications.notification_delivery (created_at);

CREATE INDEX ix_notification_delivery_procedure_id ON notifications.notification_delivery (procedure_id);

CREATE INDEX ix_notification_delivery_recipient_user_id ON notifications.notification_delivery (recipient_user_id);

CREATE INDEX ix_prt_token_hash ON identity.password_reset_tokens (token_hash);

CREATE INDEX ix_prt_user_id ON identity.password_reset_tokens (user_id);

CREATE UNIQUE INDEX ix_permissions_code ON rbac.permissions (code);

CREATE INDEX ix_permissions_module ON rbac.permissions (module);

CREATE INDEX "IX_role_menu_items_menu_item_id" ON rbac.role_menu_items (menu_item_id);

CREATE INDEX "IX_role_permissions_permission_id" ON rbac.role_permissions (permission_id);

CREATE UNIQUE INDEX ix_roles_code ON rbac.roles (code);

CREATE INDEX ix_sync_type_unresolved ON identity.sync_inconsistencies (type) WHERE resolved_at IS NULL;

CREATE INDEX ix_ual_event ON identity.user_audit_log (event);

CREATE INDEX ix_ual_occurred_at ON identity.user_audit_log (occurred_at);

CREATE INDEX ix_ual_user_id ON identity.user_audit_log (user_id);

CREATE INDEX ix_user_roles_role_id ON rbac.user_roles (role_id);

CREATE INDEX "IX_users_created_by_user_id" ON identity.users (created_by_user_id);

CREATE UNIQUE INDEX ix_users_cognito_sub ON identity.users (cognito_sub) WHERE cognito_sub IS NOT NULL;

CREATE INDEX ix_users_document ON identity.users (document_type, document_number) WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX ix_users_email_active ON identity.users (email) WHERE deleted_at IS NULL;

CREATE INDEX ix_users_status ON identity.users (status) WHERE deleted_at IS NULL;

INSERT INTO rbac.roles (id, code, name, description, is_system, is_active, created_at, updated_at) VALUES
    ('01900000-0001-7001-8001-000000000001', 'ADMIN', 'Administrador', 'Acceso total al shell FLIT', TRUE, TRUE, '2026-06-03T00:00:00Z'::timestamptz, '2026-06-03T00:00:00Z'::timestamptz);

INSERT INTO rbac.permissions (id, code, name, description, module, is_system, created_at) VALUES
    ('01900000-0002-7001-8001-000000000001',          'USERS.LIST',            'Listar usuarios',        NULL, 'USERS', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0002-7001-8001-000000000002',        'USERS.CREATE',          'Crear usuarios',         NULL, 'USERS', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0002-7001-8001-000000000003',          'USERS.EDIT',            'Editar usuarios',        NULL, 'USERS', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0002-7001-8001-000000000004',  'USERS.CHANGE_STATUS',   'Cambiar estado usuario', NULL, 'USERS', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0002-7001-8001-000000000005',        'USERS.DELETE',          'Eliminar usuario',       NULL, 'USERS', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0002-7001-8001-000000000006', 'USERS.RESET_PASSWORD',  'Resetear contraseña',    NULL, 'USERS', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0002-7001-8001-000000000007',     'USERS.MANAGE_MFA',      'Gestionar MFA',          NULL, 'USERS', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0004-7001-8001-000000000001',    'RBAC.MANAGE_ROLES',       'Gestionar roles',       NULL, 'RBAC', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0004-7001-8001-000000000002',    'RBAC.MANAGE_PERMISSIONS', 'Gestionar permisos',    NULL, 'RBAC', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0004-7001-8001-000000000003',     'RBAC.ASSIGN_ROLE',        'Asignar rol a usuario', NULL, 'RBAC', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0005-7001-8001-000000000001',         'MENU.MANAGE',             'Gestionar menús',       NULL, 'MENU', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0009-7001-8001-000000000001',  'NOTIFICATIONS.SEND',      'Enviar notificaciones', NULL, 'NOTIFICATIONS', TRUE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-0009-7001-8001-000000000002',  'NOTIFICATIONS.VIEW_HISTORY', 'Ver historial de notificaciones', NULL, 'NOTIFICATIONS', TRUE, '2026-06-03T00:00:00Z'::timestamptz);

INSERT INTO rbac.menu_items (id, code, parent_id, label, icon, frontend_path, sort_order, is_active, is_visible, is_separator, created_at) VALUES
    ('01900000-000b-7001-8001-000000000001',           'HOME',           NULL, 'Inicio',          'home',         '/',                1, TRUE, TRUE, FALSE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-000b-7001-8001-000000000003', 'ADMINISTRATION', NULL, 'Administración',  'shield-check', NULL,               2, TRUE, TRUE, FALSE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-000b-7001-8001-000000000005',        'PROFILE',        NULL, 'Mi Perfil',       'user',         '/profile',        99, TRUE, TRUE, FALSE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-000b-7001-8001-000000000021',       'ADMIN_USERS',       '01900000-000b-7001-8001-000000000003', 'Usuarios', 'users',  '/admin/users',       1, TRUE, TRUE, FALSE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-000b-7001-8001-000000000022',       'ADMIN_ROLES',       '01900000-000b-7001-8001-000000000003', 'Roles',    'shield', '/admin/roles',       2, TRUE, TRUE, FALSE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-000b-7001-8001-000000000023', 'ADMIN_PERMISSIONS', '01900000-000b-7001-8001-000000000003', 'Permisos', 'key',    '/admin/permissions', 3, TRUE, TRUE, FALSE, '2026-06-03T00:00:00Z'::timestamptz),
    ('01900000-000b-7001-8001-000000000024',       'ADMIN_MENUS',       '01900000-000b-7001-8001-000000000003', 'Menús',    'menu',   '/admin/menus',       4, TRUE, TRUE, FALSE, '2026-06-03T00:00:00Z'::timestamptz);

INSERT INTO rbac.role_permissions (role_id, permission_id, assigned_at, assigned_by_user_id)
SELECT '01900000-0001-7001-8001-000000000001'::uuid, p.id, '2026-06-03T00:00:00Z'::timestamptz, NULL
FROM rbac.permissions p
WHERE p.is_system = TRUE;

INSERT INTO rbac.role_menu_items (role_id, menu_item_id)
SELECT '01900000-0001-7001-8001-000000000001'::uuid, mi.id
FROM rbac.menu_items mi;

INSERT INTO identity.users
    (id, cognito_sub, email, full_name, document_type, document_number, phone,
     status, mfa_enabled, created_at, updated_at)
VALUES
    ('01900000-100b-7001-8001-000000000001'::uuid,
     'stub-admin@flit.io',
     'admin@flit.io',
     'Administrador FLIT',
     'CC',
     '1000000000',
     '3000000000',
     'ACTIVE',
     FALSE,
     '2026-05-22T00:00:00Z'::timestamptz,
     '2026-05-22T00:00:00Z'::timestamptz)
ON CONFLICT (email) WHERE deleted_at IS NULL DO NOTHING;

INSERT INTO rbac.user_roles (user_id, role_id, assigned_at)
SELECT u.id, '01900000-0001-7001-8001-000000000001'::uuid, '2026-05-22T00:00:00Z'::timestamptz
FROM identity.users u
WHERE u.email = 'admin@flit.io'
ON CONFLICT (user_id, role_id) DO NOTHING;

INSERT INTO identity.users
    (id, cognito_sub, email, full_name, document_type, document_number, phone,
     status, mfa_enabled, created_at, updated_at)
VALUES
    ('01900000-100b-7001-8001-000000000002'::uuid,
     'stub-admin@flitsas.io',
     'admin@flitsas.io',
     'Administrador FLIT SAS',
     'CC',
     '1000000000',
     '3000000000',
     'ACTIVE',
     FALSE,
     '2026-05-22T00:00:00Z'::timestamptz,
     '2026-05-22T00:00:00Z'::timestamptz)
ON CONFLICT (email) WHERE deleted_at IS NULL DO NOTHING;

INSERT INTO rbac.user_roles (user_id, role_id, assigned_at)
SELECT u.id, '01900000-0001-7001-8001-000000000001'::uuid, '2026-05-22T00:00:00Z'::timestamptz
FROM identity.users u
WHERE u.email = 'admin@flitsas.io'
ON CONFLICT (user_id, role_id) DO NOTHING;

COMMIT;

