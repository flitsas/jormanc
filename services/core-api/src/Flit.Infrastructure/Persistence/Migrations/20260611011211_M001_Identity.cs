using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M001_Identity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PG 17+: uuidv7() via pg_uuidv7 when packaged; dev fallback uses gen_random_uuid() (convenciones §4).
            migrationBuilder.Sql("""
                DO $ef$
                BEGIN
                  CREATE EXTENSION IF NOT EXISTS "pg_uuidv7";
                EXCEPTION
                  WHEN OTHERS THEN
                    IF to_regprocedure('uuidv7()') IS NULL THEN
                      CREATE OR REPLACE FUNCTION public.uuidv7() RETURNS uuid
                      LANGUAGE sql VOLATILE PARALLEL SAFE
                      AS $fn$ SELECT gen_random_uuid() $fn$;
                    END IF;
                END
                $ef$;
                """);

            migrationBuilder.EnsureSchema(
                name: "procedures_config");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "companies");

            migrationBuilder.EnsureSchema(
                name: "integrations");

            migrationBuilder.EnsureSchema(
                name: "documents");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "ot");

            migrationBuilder.EnsureSchema(
                name: "procedures");

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_name = table.Column<string>(type: "text", nullable: false),
                    table_name = table.Column<string>(type: "text", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<char>(type: "character(1)", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nit = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "connector_configs",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_type = table.Column<string>(type: "text", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    credentials_ref = table.Column<string>(type: "jsonb", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    timeout_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 4000),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connector_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consolidated_packages",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    merged_file_ref = table.Column<string>(type: "text", nullable: false),
                    download_filename = table.Column<string>(type: "text", nullable: false),
                    doc_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consolidated_packages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_types",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    load_type = table.Column<string>(type: "text", nullable: false, defaultValue: "carga"),
                    allowed_formats = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[\"pdf\"]"),
                    max_size_mb = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    is_reusable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identity_validations",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    verdict = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    liveness_ref = table.Column<string>(type: "text", nullable: true),
                    document_photo_ref = table.Column<string>(type: "text", nullable: true),
                    validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_validations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_logs",
                schema: "integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    connector_type = table.Column<string>(type: "text", nullable: false),
                    operation = table.Column<string>(type: "text", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    request_payload = table.Column<string>(type: "jsonb", nullable: true),
                    response_payload = table.Column<string>(type: "jsonb", nullable: true),
                    http_status = table.Column<int>(type: "integer", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    logged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ot_organisms",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    mode = table.Column<string>(type: "text", nullable: false, defaultValue: "dashboard"),
                    quipux_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    quipux_config = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ot_organisms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    slug = table.Column<string>(type: "text", nullable: false),
                    module = table.Column<string>(type: "text", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "procedure_types",
                schema: "procedures_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    family = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false, defaultValue: "global"),
                    scope_ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehicle_query_key = table.Column<string>(type: "text", nullable: false, defaultValue: "placa"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "procedures",
                schema: "procedures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    procedure_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedure_type_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    composite_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "draft"),
                    current_step_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    step_data = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "company_configs",
                schema: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    only_own_vehicles = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    baul_firmas_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notification_target = table.Column<string>(type: "text", nullable: false, defaultValue: "radicador"),
                    smtp_mode = table.Column<string>(type: "text", nullable: false, defaultValue: "native"),
                    smtp_config_encrypted = table.Column<byte[]>(type: "bytea", nullable: true),
                    matricula_config = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    traspasos_config = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    contingency_config = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    recaudo_methods = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_configs", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_configs_companies",
                        column: x => x.company_id,
                        principalSchema: "companies",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_ot_enabled",
                schema: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ot_slug = table.Column<string>(type: "text", nullable: false),
                    procedure_family = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_ot_enabled", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_ot_enabled_companies",
                        column: x => x.company_id,
                        principalSchema: "companies",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_signature_matrix",
                schema: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_role = table.Column<string>(type: "text", nullable: false),
                    signature_type = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_signature_matrix", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_signature_matrix_companies",
                        column: x => x.company_id,
                        principalSchema: "companies",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_user_exceptions",
                schema: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_by = table.Column<Guid>(type: "uuid", nullable: true),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_user_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_user_exceptions_companies",
                        column: x => x.company_id,
                        principalSchema: "companies",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_templates",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    content_ref = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_templates_document_types",
                        column: x => x.document_type_id,
                        principalSchema: "documents",
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procedure_documents",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin = table.Column<string>(type: "text", nullable: false, defaultValue: "uploaded"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    file_ref = table.Column<string>(type: "text", nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: true),
                    generation_metadata = table.Column<string>(type: "jsonb", nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedure_documents_document_types",
                        column: x => x.document_type_id,
                        principalSchema: "documents",
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procedure_type_documents",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    order_index = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    actor_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allow_partial_consolidation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_type_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedure_type_documents_document_types",
                        column: x => x.document_type_id,
                        principalSchema: "documents",
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ot_document_labels",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    ot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ot_document_labels", x => x.id);
                    table.ForeignKey(
                        name: "fk_ot_doc_labels_organisms",
                        column: x => x.ot_id,
                        principalSchema: "ot",
                        principalTable: "ot_organisms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ot_document_orders",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    ot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedure_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordered_document_type_ids = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ot_document_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_ot_doc_orders_organisms",
                        column: x => x.ot_id,
                        principalSchema: "ot",
                        principalTable: "ot_organisms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ot_integration_logs",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    ot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    procedure_ref = table.Column<string>(type: "text", nullable: true),
                    request_payload = table.Column<string>(type: "jsonb", nullable: true),
                    response_payload = table.Column<string>(type: "jsonb", nullable: true),
                    http_status = table.Column<int>(type: "integer", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    logged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ot_integration_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_ot_integration_logs_organisms",
                        column: x => x.ot_id,
                        principalSchema: "ot",
                        principalTable: "ot_organisms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ot_rule_sets",
                schema: "ot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    ot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false),
                    actions = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ot_rule_sets", x => x.id);
                    table.ForeignKey(
                        name: "fk_ot_rule_sets_organisms",
                        column: x => x.ot_id,
                        principalSchema: "ot",
                        principalTable: "ot_organisms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "actor_definitions",
                schema: "procedures_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    allowed_nature = table.Column<string>(type: "text", nullable: false, defaultValue: "ambas"),
                    min_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    max_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    order_index = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    legal_rep_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actor_definitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_actor_definitions_legal_rep",
                        column: x => x.legal_rep_actor_id,
                        principalSchema: "procedures_config",
                        principalTable: "actor_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_actor_definitions_procedure_types",
                        column: x => x.procedure_type_id,
                        principalSchema: "procedures_config",
                        principalTable: "procedure_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_connectors",
                schema: "procedures_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    http_verb = table.Column<string>(type: "text", nullable: false, defaultValue: "GET"),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    param_bindings = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    response_mappings = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_connectors", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_connectors_procedure_types",
                        column: x => x.procedure_type_id,
                        principalSchema: "procedures_config",
                        principalTable: "procedure_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "procedure_steps",
                schema: "procedures_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    step_type = table.Column<string>(type: "text", nullable: false, defaultValue: "form"),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedure_steps_procedure_types",
                        column: x => x.procedure_type_id,
                        principalSchema: "procedures_config",
                        principalTable: "procedure_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "procedure_type_snapshots",
                schema: "procedures_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_type_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedure_type_snapshots_procedure_types",
                        column: x => x.procedure_type_id,
                        principalSchema: "procedures_config",
                        principalTable: "procedure_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rule_sets",
                schema: "procedures_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false),
                    actions = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_sets", x => x.id);
                    table.ForeignKey(
                        name: "fk_rule_sets_procedure_types",
                        column: x => x.procedure_type_id,
                        principalSchema: "procedures_config",
                        principalTable: "procedure_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "procedure_actors",
                schema: "procedures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nature = table.Column<string>(type: "text", nullable: false),
                    parent_actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_type = table.Column<string>(type: "text", nullable: true),
                    document_number = table.Column<string>(type: "text", nullable: true),
                    nit = table.Column<string>(type: "text", nullable: true),
                    full_name = table.Column<string>(type: "text", nullable: true),
                    cuota_pct = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    query_results = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    identity_validated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_actors", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedure_actors_parent",
                        column: x => x.parent_actor_id,
                        principalSchema: "procedures",
                        principalTable: "procedure_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_procedure_actors_procedures",
                        column: x => x.procedure_id,
                        principalSchema: "procedures",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "procedure_attachments",
                schema: "procedures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label_slug = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    file_ref = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedure_attachments_procedures",
                        column: x => x.procedure_id,
                        principalSchema: "procedures",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_queries",
                schema: "procedures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    query_key = table.Column<string>(type: "text", nullable: false, defaultValue: "placa"),
                    query_value = table.Column<string>(type: "text", nullable: false),
                    runt_payload = table.Column<string>(type: "jsonb", nullable: true),
                    simit_payload = table.Column<string>(type: "jsonb", nullable: true),
                    rues_payload = table.Column<string>(type: "jsonb", nullable: true),
                    warnings = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    queried_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicle_queries", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_queries_procedures",
                        column: x => x.procedure_id,
                        principalSchema: "procedures",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invitations",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    roles_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitations_tenants",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_roles_tenants",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    must_reset_pwd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_tenants",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "template_fields",
                schema: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marker = table.Column<string>(type: "text", nullable: false),
                    data_source = table.Column<string>(type: "text", nullable: false),
                    data_path = table.Column<string>(type: "text", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_template_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_template_fields_document_templates",
                        column: x => x.template_id,
                        principalSchema: "documents",
                        principalTable: "document_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "query_rules",
                schema: "procedures_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    actor_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<string>(type: "text", nullable: false),
                    entry_key = table.Column<string>(type: "text", nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    verifications = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_query_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_query_rules_actor_definitions",
                        column: x => x.actor_definition_id,
                        principalSchema: "procedures_config",
                        principalTable: "actor_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_sections",
                schema: "procedures_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_collapsible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_sections_procedure_steps",
                        column: x => x.step_id,
                        principalSchema: "procedures_config",
                        principalTable: "procedure_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "procedure_signatures",
                schema: "procedures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signature_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    file_ref = table.Column<string>(type: "text", nullable: true),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_signatures", x => x.id);
                    table.ForeignKey(
                        name: "fk_procedure_signatures_actors",
                        column: x => x.actor_id,
                        principalSchema: "procedures",
                        principalTable: "procedure_actors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_procedure_signatures_procedures",
                        column: x => x.procedure_id,
                        principalSchema: "procedures",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "identity",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions",
                        column: x => x.permission_id,
                        principalSchema: "identity",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_users",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jti = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessions_tenants",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sessions_users",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_fields",
                schema: "procedures_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    field_type = table.Column<string>(type: "text", nullable: false, defaultValue: "text"),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    config = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_fields_form_sections",
                        column: x => x.section_id,
                        principalSchema: "procedures_config",
                        principalTable: "form_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_actor_definitions_legal_rep_actor_id",
                schema: "procedures_config",
                table: "actor_definitions",
                column: "legal_rep_actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_actor_definitions_procedure_type_id",
                schema: "procedures_config",
                table: "actor_definitions",
                column: "procedure_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_actor_definitions_tenant_type",
                schema: "procedures_config",
                table: "actor_definitions",
                columns: new[] { "tenant_id", "procedure_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_api_connectors_procedure_type_id",
                schema: "procedures_config",
                table: "api_connectors",
                column: "procedure_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_connectors_tenant_type",
                schema: "procedures_config",
                table: "api_connectors",
                columns: new[] { "tenant_id", "procedure_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_changed_at",
                schema: "audit",
                table: "audit_log",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_tenant_table_record",
                schema: "audit",
                table: "audit_log",
                columns: new[] { "tenant_id", "table_name", "record_id" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_name",
                schema: "companies",
                table: "companies",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_companies_nit",
                schema: "companies",
                table: "companies",
                column: "nit");

            migrationBuilder.CreateIndex(
                name: "ix_companies_status",
                schema: "companies",
                table: "companies",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_companies_tenant_id",
                schema: "companies",
                table: "companies",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_company_configs_company_id",
                schema: "companies",
                table: "company_configs",
                column: "company_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_company_ot_family",
                schema: "companies",
                table: "company_ot_enabled",
                columns: new[] { "company_id", "ot_slug", "procedure_family" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_signature_matrix_role",
                schema: "companies",
                table: "company_signature_matrix",
                columns: new[] { "company_id", "actor_role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_connector_configs_tenant_type_primary",
                schema: "integrations",
                table: "connector_configs",
                columns: new[] { "tenant_id", "connector_type", "is_primary" },
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "ix_consolidated_packages_tenant_procedure",
                schema: "documents",
                table: "consolidated_packages",
                columns: new[] { "tenant_id", "procedure_id" });

            migrationBuilder.CreateIndex(
                name: "uq_consolidated_package_proc_version",
                schema: "documents",
                table: "consolidated_packages",
                columns: new[] { "procedure_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_templates_active_per_type",
                schema: "documents",
                table: "document_templates",
                columns: new[] { "document_type_id", "status" },
                filter: "status = 'active'");

            migrationBuilder.CreateIndex(
                name: "uq_template_doc_type_version",
                schema: "documents",
                table: "document_templates",
                columns: new[] { "document_type_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_types_tenant_name",
                schema: "documents",
                table: "document_types",
                columns: new[] { "tenant_id", "name" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_form_fields_section_id",
                schema: "procedures_config",
                table: "form_fields",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_fields_config_gin",
                schema: "procedures_config",
                table: "form_fields",
                column: "config")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_form_fields_tenant_section",
                schema: "procedures_config",
                table: "form_fields",
                columns: new[] { "tenant_id", "section_id" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_form_sections_step_id",
                schema: "procedures_config",
                table: "form_sections",
                column: "step_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_sections_tenant_step_order",
                schema: "procedures_config",
                table: "form_sections",
                columns: new[] { "tenant_id", "step_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_validations_created_at",
                schema: "integrations",
                table: "identity_validations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_identity_validations_tenant_procedure_actor",
                schema: "integrations",
                table: "identity_validations",
                columns: new[] { "tenant_id", "procedure_id", "actor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_integration_logs_logged_at",
                schema: "integrations",
                table: "integration_logs",
                column: "logged_at");

            migrationBuilder.CreateIndex(
                name: "ix_integration_logs_tenant_type",
                schema: "integrations",
                table: "integration_logs",
                columns: new[] { "tenant_id", "connector_type" });

            migrationBuilder.CreateIndex(
                name: "ix_invitations_tenant_id_status",
                schema: "identity",
                table: "invitations",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_invitations_token_hash",
                schema: "identity",
                table: "invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_ot_doc_label_ot_slug",
                schema: "ot",
                table: "ot_document_labels",
                columns: new[] { "ot_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ot_document_orders_ot_id",
                schema: "ot",
                table: "ot_document_orders",
                column: "ot_id");

            migrationBuilder.CreateIndex(
                name: "uq_ot_doc_orders_tenant_ot_type",
                schema: "ot",
                table: "ot_document_orders",
                columns: new[] { "tenant_id", "ot_id", "procedure_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ot_integration_logs_ot_id",
                schema: "ot",
                table: "ot_integration_logs",
                column: "ot_id");

            migrationBuilder.CreateIndex(
                name: "ix_ot_integration_logs_logged_at",
                schema: "ot",
                table: "ot_integration_logs",
                column: "logged_at");

            migrationBuilder.CreateIndex(
                name: "ix_ot_integration_logs_tenant_ot",
                schema: "ot",
                table: "ot_integration_logs",
                columns: new[] { "tenant_id", "ot_id" });

            migrationBuilder.CreateIndex(
                name: "uq_ot_organisms_tenant_slug",
                schema: "ot",
                table: "ot_organisms",
                columns: new[] { "tenant_id", "slug" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ot_rule_sets_ot_id",
                schema: "ot",
                table: "ot_rule_sets",
                column: "ot_id");

            migrationBuilder.CreateIndex(
                name: "ix_ot_rule_sets_tenant_ot_active",
                schema: "ot",
                table: "ot_rule_sets",
                columns: new[] { "tenant_id", "ot_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_user_id_active",
                schema: "identity",
                table: "password_reset_tokens",
                columns: new[] { "user_id", "expires_at" },
                filter: "used_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_password_reset_tokens_hash",
                schema: "identity",
                table: "password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_permissions_slug",
                schema: "identity",
                table: "permissions",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_procedure_actors_parent_actor_id",
                schema: "procedures",
                table: "procedure_actors",
                column: "parent_actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_procedure_actors_procedure_id",
                schema: "procedures",
                table: "procedure_actors",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_proc_actors_procedure_id",
                schema: "procedures",
                table: "procedure_actors",
                columns: new[] { "tenant_id", "procedure_id" });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_attachments_procedure_id",
                schema: "procedures",
                table: "procedure_attachments",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedure_attachments_tenant_procedure_label",
                schema: "procedures",
                table: "procedure_attachments",
                columns: new[] { "tenant_id", "procedure_id", "label_slug" });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_documents_document_type_id",
                schema: "documents",
                table: "procedure_documents",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedure_documents_tenant_procedure_status",
                schema: "documents",
                table: "procedure_documents",
                columns: new[] { "tenant_id", "procedure_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_signatures_actor_id",
                schema: "procedures",
                table: "procedure_signatures",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_procedure_signatures_procedure_id",
                schema: "procedures",
                table: "procedure_signatures",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedure_signatures_tenant_procedure_status",
                schema: "procedures",
                table: "procedure_signatures",
                columns: new[] { "tenant_id", "procedure_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_steps_procedure_type_id",
                schema: "procedures_config",
                table: "procedure_steps",
                column: "procedure_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedure_steps_tenant_type_order",
                schema: "procedures_config",
                table: "procedure_steps",
                columns: new[] { "tenant_id", "procedure_type_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_type_documents_document_type_id",
                schema: "documents",
                table: "procedure_type_documents",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedure_type_documents_tenant_type",
                schema: "documents",
                table: "procedure_type_documents",
                columns: new[] { "tenant_id", "procedure_type_id" });

            migrationBuilder.CreateIndex(
                name: "uq_proc_type_doc",
                schema: "documents",
                table: "procedure_type_documents",
                columns: new[] { "procedure_type_id", "document_type_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_snapshot_type_version",
                schema: "procedures_config",
                table: "procedure_type_snapshots",
                columns: new[] { "procedure_type_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_procedure_types_tenant_id_active",
                schema: "procedures_config",
                table: "procedure_types",
                columns: new[] { "tenant_id", "is_active" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_procedure_type_slug_tenant",
                schema: "procedures_config",
                table: "procedure_types",
                columns: new[] { "slug", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_procedures_submitted_at",
                schema: "procedures",
                table: "procedures",
                column: "submitted_at");

            migrationBuilder.CreateIndex(
                name: "ix_procedures_tenant_company_id",
                schema: "procedures",
                table: "procedures",
                columns: new[] { "tenant_id", "company_id" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_procedures_tenant_status",
                schema: "procedures",
                table: "procedures",
                columns: new[] { "tenant_id", "status" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_procedures_composite_id",
                schema: "procedures",
                table: "procedures",
                column: "composite_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_query_rules_actor_definition_id",
                schema: "procedures_config",
                table: "query_rules",
                column: "actor_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_query_rules_tenant_actor",
                schema: "procedures_config",
                table: "query_rules",
                columns: new[] { "tenant_id", "actor_definition_id" });

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                schema: "identity",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_tenant_id",
                schema: "identity",
                table: "roles",
                column: "tenant_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_roles_slug_tenant",
                schema: "identity",
                table: "roles",
                columns: new[] { "slug", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rule_sets_procedure_type_id",
                schema: "procedures_config",
                table: "rule_sets",
                column: "procedure_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_rule_sets_conditions_gin",
                schema: "procedures_config",
                table: "rule_sets",
                column: "conditions")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_rule_sets_tenant_type_active",
                schema: "procedures_config",
                table: "rule_sets",
                columns: new[] { "tenant_id", "procedure_type_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_sessions_tenant_id",
                schema: "identity",
                table: "sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_active_revoked",
                schema: "identity",
                table: "sessions",
                column: "user_id",
                filter: "is_revoked = false");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_user_id_is_revoked",
                schema: "identity",
                table: "sessions",
                columns: new[] { "user_id", "is_revoked" });

            migrationBuilder.CreateIndex(
                name: "uq_sessions_jti",
                schema: "identity",
                table: "sessions",
                column: "jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_template_field_marker",
                schema: "documents",
                table: "template_fields",
                columns: new[] { "template_id", "marker" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_user_exceptions_user_id",
                schema: "companies",
                table: "tenant_user_exceptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_user_exception",
                schema: "companies",
                table: "tenant_user_exceptions",
                columns: new[] { "company_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenants_is_active",
                schema: "identity",
                table: "tenants",
                column: "is_active",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_tenants_slug",
                schema: "identity",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                schema: "identity",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id_tenant_id",
                schema: "identity",
                table: "user_roles",
                columns: new[] { "user_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id",
                schema: "identity",
                table: "users",
                column: "tenant_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_users_email_tenant",
                schema: "identity",
                table: "users",
                columns: new[] { "email", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_queries_procedure_id",
                schema: "procedures",
                table: "vehicle_queries",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_queries_query_value",
                schema: "procedures",
                table: "vehicle_queries",
                column: "query_value");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_queries_tenant_procedure_id",
                schema: "procedures",
                table: "vehicle_queries",
                columns: new[] { "tenant_id", "procedure_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_connectors",
                schema: "procedures_config");

            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "company_configs",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "company_ot_enabled",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "company_signature_matrix",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "connector_configs",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "consolidated_packages",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "form_fields",
                schema: "procedures_config");

            migrationBuilder.DropTable(
                name: "identity_validations",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "integration_logs",
                schema: "integrations");

            migrationBuilder.DropTable(
                name: "invitations",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ot_document_labels",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "ot_document_orders",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "ot_integration_logs",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "ot_rule_sets",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "password_reset_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "procedure_attachments",
                schema: "procedures");

            migrationBuilder.DropTable(
                name: "procedure_documents",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "procedure_signatures",
                schema: "procedures");

            migrationBuilder.DropTable(
                name: "procedure_type_documents",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "procedure_type_snapshots",
                schema: "procedures_config");

            migrationBuilder.DropTable(
                name: "query_rules",
                schema: "procedures_config");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "rule_sets",
                schema: "procedures_config");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "template_fields",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "tenant_user_exceptions",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "vehicle_queries",
                schema: "procedures");

            migrationBuilder.DropTable(
                name: "form_sections",
                schema: "procedures_config");

            migrationBuilder.DropTable(
                name: "ot_organisms",
                schema: "ot");

            migrationBuilder.DropTable(
                name: "procedure_actors",
                schema: "procedures");

            migrationBuilder.DropTable(
                name: "actor_definitions",
                schema: "procedures_config");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "document_templates",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "procedure_steps",
                schema: "procedures_config");

            migrationBuilder.DropTable(
                name: "procedures",
                schema: "procedures");

            migrationBuilder.DropTable(
                name: "document_types",
                schema: "documents");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "procedure_types",
                schema: "procedures_config");
        }
    }
}
