using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class M003_TenantFKAndPII : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ================================================================
        // M003 — Cross-schema tenant_id FK constraints + PII column comments
        // Fixes db-schema-validator MISSING_2:
        //   A4: tenant_id FK to identity.tenants not set on business tables
        //   A15: PII COMMENT ON COLUMN annotations missing from migration SQL
        //
        // ADR-0010: Shared DB + tenant_id + RLS strategy.
        // ON DELETE RESTRICT chosen to prevent accidental cascade deletion
        // of tenant data; Tenant deactivation is soft-delete, not hard-delete.
        // ================================================================

        // --- identity schema --- user_roles tenant FK (sessions/invitations ya en M001)
        migrationBuilder.Sql(@"
            ALTER TABLE identity.user_roles
                ADD CONSTRAINT fk_user_roles_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;
        ");

        // --- companies schema ---
        // company_configs, company_ot_enabled, company_signature_matrix, tenant_user_exceptions
        // no tienen tenant_id propio — aislamiento heredado vía FK a companies.companies (RLS).
        // Justificación: tabla hija en relación 1:N/1:1 con companies; el FK a tenants es redundante.
        migrationBuilder.Sql(@"
            ALTER TABLE companies.companies
                ADD CONSTRAINT fk_companies_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;
        ");

        // --- procedures_config schema ---
        migrationBuilder.Sql(@"
            ALTER TABLE procedures_config.procedure_types
                ADD CONSTRAINT fk_procedure_types_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures_config.procedure_steps
                ADD CONSTRAINT fk_procedure_steps_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures_config.form_sections
                ADD CONSTRAINT fk_form_sections_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures_config.form_fields
                ADD CONSTRAINT fk_form_fields_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures_config.api_connectors
                ADD CONSTRAINT fk_api_connectors_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures_config.rule_sets
                ADD CONSTRAINT fk_rule_sets_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures_config.actor_definitions
                ADD CONSTRAINT fk_actor_definitions_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures_config.query_rules
                ADD CONSTRAINT fk_query_rules_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;
        ");
        // procedure_type_snapshots: sin tenant_id (heredado por FK a procedure_types)

        // --- procedures schema ---
        migrationBuilder.Sql(@"
            ALTER TABLE procedures.procedures
                ADD CONSTRAINT fk_procedures_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures.procedure_actors
                ADD CONSTRAINT fk_procedure_actors_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures.vehicle_queries
                ADD CONSTRAINT fk_vehicle_queries_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures.procedure_signatures
                ADD CONSTRAINT fk_procedure_signatures_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE procedures.procedure_attachments
                ADD CONSTRAINT fk_procedure_attachments_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;
        ");

        // --- documents schema ---
        migrationBuilder.Sql(@"
            ALTER TABLE documents.document_types
                ADD CONSTRAINT fk_document_types_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE documents.procedure_type_documents
                ADD CONSTRAINT fk_procedure_type_documents_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE documents.document_templates
                ADD CONSTRAINT fk_document_templates_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE documents.procedure_documents
                ADD CONSTRAINT fk_procedure_documents_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE documents.consolidated_packages
                ADD CONSTRAINT fk_consolidated_packages_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;
        ");

        // --- ot schema ---
        migrationBuilder.Sql(@"
            ALTER TABLE ot.ot_organisms
                ADD CONSTRAINT fk_ot_organisms_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE ot.ot_document_orders
                ADD CONSTRAINT fk_ot_document_orders_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE ot.ot_document_labels
                ADD CONSTRAINT fk_ot_document_labels_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE ot.ot_rule_sets
                ADD CONSTRAINT fk_ot_rule_sets_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE ot.ot_integration_logs
                ADD CONSTRAINT fk_ot_integration_logs_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;
        ");

        // --- integrations schema ---
        migrationBuilder.Sql(@"
            ALTER TABLE integrations.connector_configs
                ADD CONSTRAINT fk_connector_configs_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE integrations.integration_logs
                ADD CONSTRAINT fk_integration_logs_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;

            ALTER TABLE integrations.identity_validations
                ADD CONSTRAINT fk_identity_validations_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;
        ");

        // --- audit schema ---
        migrationBuilder.Sql(@"
            ALTER TABLE audit.audit_log
                ADD CONSTRAINT fk_audit_log_tenants
                FOREIGN KEY (tenant_id) REFERENCES identity.tenants(id) ON DELETE RESTRICT;
        ");

        // ================================================================
        // PII COMMENT ON COLUMN — Habeas Data Colombia (Ley 1581/2012)
        // Obligatorio por convención §6.3 y ADR-0011 (payloads de integraciones).
        // Etiquetas: @pii:high | @pii:medium | @pii:low
        // ================================================================

        // procedures.vehicle_queries — RUNT/SIMIT/RUES payloads
        migrationBuilder.Sql(@"
            COMMENT ON TABLE procedures.vehicle_queries IS
                'Consultas de vehículo contra RUNT/SIMIT/RUES. Contiene datos PII. Habeas Data: Ley 1581/2012.';
            COMMENT ON COLUMN procedures.vehicle_queries.runt_payload IS
                '@pii:high — Respuesta completa RUNT (propietario, prenda, restricciones). Habeas Data: Ley 1581/2012.';
            COMMENT ON COLUMN procedures.vehicle_queries.simit_payload IS
                '@pii:medium — Respuesta completa SIMIT (multas, comparendos).';
            COMMENT ON COLUMN procedures.vehicle_queries.rues_payload IS
                '@pii:medium — Respuesta completa RUES (registro mercantil).';
        ");

        // integrations.identity_validations — biometrics
        migrationBuilder.Sql(@"
            COMMENT ON TABLE integrations.identity_validations IS
                'Validación biométrica de identidad (liveness Verifik). @pii:high — Habeas Data: Ley 1581/2012.';
            COMMENT ON COLUMN integrations.identity_validations.liveness_ref IS
                '@pii:high — Referencia MinIO al video de liveness. No exponer sin consentimiento del titular.';
            COMMENT ON COLUMN integrations.identity_validations.document_photo_ref IS
                '@pii:high — Referencia MinIO a la fotografía del documento de identidad.';
        ");

        // integrations.integration_logs — payloads externos
        migrationBuilder.Sql(@"
            COMMENT ON COLUMN integrations.integration_logs.request_payload IS
                '@pii:medium — Payload de solicitud a sistema externo. Puede contener datos personales según conector.';
            COMMENT ON COLUMN integrations.integration_logs.response_payload IS
                '@pii:medium — Payload de respuesta de sistema externo. Puede contener datos personales según conector.';
        ");

        // procedures.procedure_actors — datos personales del actor
        migrationBuilder.Sql(@"
            COMMENT ON COLUMN procedures.procedure_actors.full_name IS
                '@pii:medium — Nombre del actor en el trámite. Habeas Data: Ley 1581/2012.';
            COMMENT ON COLUMN procedures.procedure_actors.document_number IS
                '@pii:high — Número de documento del actor.';
            COMMENT ON COLUMN procedures.procedure_actors.query_results IS
                '@pii:medium — Resultados de consultas externas (JSONB). Puede contener datos personales.';
        ");

        // identity.users — datos del usuario
        migrationBuilder.Sql(@"
            COMMENT ON COLUMN identity.users.email IS
                '@pii:medium — Correo electrónico del usuario. Dato personal según Ley 1581/2012.';
            COMMENT ON COLUMN identity.users.full_name IS
                '@pii:medium — Nombre completo del usuario.';
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove PII comments
        migrationBuilder.Sql(@"
            COMMENT ON TABLE procedures.vehicle_queries IS NULL;
            COMMENT ON COLUMN procedures.vehicle_queries.runt_payload IS NULL;
            COMMENT ON COLUMN procedures.vehicle_queries.simit_payload IS NULL;
            COMMENT ON COLUMN procedures.vehicle_queries.rues_payload IS NULL;

            COMMENT ON TABLE integrations.identity_validations IS NULL;
            COMMENT ON COLUMN integrations.identity_validations.liveness_ref IS NULL;
            COMMENT ON COLUMN integrations.identity_validations.document_photo_ref IS NULL;

            COMMENT ON COLUMN integrations.integration_logs.request_payload IS NULL;
            COMMENT ON COLUMN integrations.integration_logs.response_payload IS NULL;

            COMMENT ON COLUMN procedures.procedure_actors.full_name IS NULL;
            COMMENT ON COLUMN procedures.procedure_actors.document_number IS NULL;
            COMMENT ON COLUMN procedures.procedure_actors.query_results IS NULL;

            COMMENT ON COLUMN identity.users.email IS NULL;
            COMMENT ON COLUMN identity.users.full_name IS NULL;
        ");

        // Remove cross-schema FK constraints
        migrationBuilder.Sql(@"
            ALTER TABLE audit.audit_log DROP CONSTRAINT IF EXISTS fk_audit_log_tenants;

            ALTER TABLE integrations.identity_validations DROP CONSTRAINT IF EXISTS fk_identity_validations_tenants;
            ALTER TABLE integrations.integration_logs DROP CONSTRAINT IF EXISTS fk_integration_logs_tenants;
            ALTER TABLE integrations.connector_configs DROP CONSTRAINT IF EXISTS fk_connector_configs_tenants;

            ALTER TABLE ot.ot_rule_sets DROP CONSTRAINT IF EXISTS fk_ot_rule_sets_tenants;
            ALTER TABLE ot.ot_integration_logs DROP CONSTRAINT IF EXISTS fk_ot_integration_logs_tenants;
            ALTER TABLE ot.ot_document_labels DROP CONSTRAINT IF EXISTS fk_ot_document_labels_tenants;
            ALTER TABLE ot.ot_document_orders DROP CONSTRAINT IF EXISTS fk_ot_document_orders_tenants;
            ALTER TABLE ot.ot_organisms DROP CONSTRAINT IF EXISTS fk_ot_organisms_tenants;

            ALTER TABLE documents.consolidated_packages DROP CONSTRAINT IF EXISTS fk_consolidated_packages_tenants;
            ALTER TABLE documents.procedure_documents DROP CONSTRAINT IF EXISTS fk_procedure_documents_tenants;
            ALTER TABLE documents.document_templates DROP CONSTRAINT IF EXISTS fk_document_templates_tenants;
            ALTER TABLE documents.procedure_type_documents DROP CONSTRAINT IF EXISTS fk_procedure_type_documents_tenants;
            ALTER TABLE documents.document_types DROP CONSTRAINT IF EXISTS fk_document_types_tenants;

            ALTER TABLE procedures.procedure_attachments DROP CONSTRAINT IF EXISTS fk_procedure_attachments_tenants;
            ALTER TABLE procedures.procedure_signatures DROP CONSTRAINT IF EXISTS fk_procedure_signatures_tenants;
            ALTER TABLE procedures.vehicle_queries DROP CONSTRAINT IF EXISTS fk_vehicle_queries_tenants;
            ALTER TABLE procedures.procedure_actors DROP CONSTRAINT IF EXISTS fk_procedure_actors_tenants;
            ALTER TABLE procedures.procedures DROP CONSTRAINT IF EXISTS fk_procedures_tenants;

            ALTER TABLE procedures_config.query_rules DROP CONSTRAINT IF EXISTS fk_query_rules_tenants;
            ALTER TABLE procedures_config.actor_definitions DROP CONSTRAINT IF EXISTS fk_actor_definitions_tenants;
            ALTER TABLE procedures_config.rule_sets DROP CONSTRAINT IF EXISTS fk_rule_sets_tenants;
            ALTER TABLE procedures_config.api_connectors DROP CONSTRAINT IF EXISTS fk_api_connectors_tenants;
            ALTER TABLE procedures_config.form_fields DROP CONSTRAINT IF EXISTS fk_form_fields_tenants;
            ALTER TABLE procedures_config.form_sections DROP CONSTRAINT IF EXISTS fk_form_sections_tenants;
            ALTER TABLE procedures_config.procedure_steps DROP CONSTRAINT IF EXISTS fk_procedure_steps_tenants;
            ALTER TABLE procedures_config.procedure_types DROP CONSTRAINT IF EXISTS fk_procedure_types_tenants;

            ALTER TABLE companies.companies DROP CONSTRAINT IF EXISTS fk_companies_tenants;

            ALTER TABLE identity.user_roles DROP CONSTRAINT IF EXISTS fk_user_roles_tenants;
        ");
    }
}
