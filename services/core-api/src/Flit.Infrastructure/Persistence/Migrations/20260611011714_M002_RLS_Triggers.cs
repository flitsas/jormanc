using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M002_RLS_Triggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // Extensions
            // ============================================================
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pg_uuidv7\";");

            // ============================================================
            // Shared trigger functions
            // ============================================================
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION public.fn_increment_row_version()
                RETURNS TRIGGER LANGUAGE plpgsql AS $$
                BEGIN
                  NEW.row_version := OLD.row_version + 1;
                  RETURN NEW;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION public.fn_audit_log()
                RETURNS TRIGGER LANGUAGE plpgsql SECURITY DEFINER AS $$
                DECLARE
                  v_changed_by uuid;
                  v_tenant_id  uuid;
                  v_record_id  uuid;
                BEGIN
                  v_changed_by := NULLIF(current_setting('app.user_id', true), '')::uuid;
                  v_tenant_id  := NULLIF(current_setting('app.tenant_id', true), '')::uuid;

                  IF TG_OP = 'DELETE' THEN
                    v_record_id := OLD.id;
                    INSERT INTO audit.audit_log
                      (tenant_id, schema_name, table_name, record_id, operation, changed_by, old_values, new_values)
                    VALUES
                      (v_tenant_id, TG_TABLE_SCHEMA, TG_TABLE_NAME, v_record_id, 'D', v_changed_by,
                       to_jsonb(OLD), NULL);
                    RETURN OLD;
                  ELSIF TG_OP = 'INSERT' THEN
                    v_record_id := NEW.id;
                    INSERT INTO audit.audit_log
                      (tenant_id, schema_name, table_name, record_id, operation, changed_by, old_values, new_values)
                    VALUES
                      (v_tenant_id, TG_TABLE_SCHEMA, TG_TABLE_NAME, v_record_id, 'I', v_changed_by,
                       NULL, to_jsonb(NEW));
                  ELSE
                    v_record_id := NEW.id;
                    INSERT INTO audit.audit_log
                      (tenant_id, schema_name, table_name, record_id, operation, changed_by, old_values, new_values)
                    VALUES
                      (v_tenant_id, TG_TABLE_SCHEMA, TG_TABLE_NAME, v_record_id, 'U', v_changed_by,
                       to_jsonb(OLD), to_jsonb(NEW));
                  END IF;
                  RETURN NEW;
                END;
                $$;
                """);

            // ============================================================
            // Identity schema — RLS + triggers
            // ============================================================

            // identity.users
            migrationBuilder.Sql("""
                ALTER TABLE identity.users ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.users FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON identity.users
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_users
                  BEFORE UPDATE ON identity.users
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                CREATE TRIGGER trg_audit_users
                  AFTER INSERT OR UPDATE OR DELETE ON identity.users
                  FOR EACH ROW EXECUTE FUNCTION public.fn_audit_log();
                """);

            // identity.roles
            migrationBuilder.Sql("""
                ALTER TABLE identity.roles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.roles FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON identity.roles
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_roles
                  BEFORE UPDATE ON identity.roles
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            // identity.sessions
            migrationBuilder.Sql("""
                ALTER TABLE identity.sessions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.sessions FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON identity.sessions
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            // identity.invitations
            migrationBuilder.Sql("""
                ALTER TABLE identity.invitations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.invitations FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON identity.invitations
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            // ============================================================
            // Companies schema — RLS + triggers
            // ============================================================

            migrationBuilder.Sql("""
                ALTER TABLE companies.companies ENABLE ROW LEVEL SECURITY;
                ALTER TABLE companies.companies FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON companies.companies
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_companies
                  BEFORE UPDATE ON companies.companies
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                CREATE TRIGGER trg_audit_companies
                  AFTER INSERT OR UPDATE OR DELETE ON companies.companies
                  FOR EACH ROW EXECUTE FUNCTION public.fn_audit_log();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE companies.company_configs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE companies.company_configs FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON companies.company_configs
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_company_configs
                  BEFORE UPDATE ON companies.company_configs
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE companies.company_signature_matrix ENABLE ROW LEVEL SECURITY;
                ALTER TABLE companies.company_signature_matrix FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON companies.company_signature_matrix
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_company_sig_matrix
                  BEFORE UPDATE ON companies.company_signature_matrix
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE companies.company_ot_enabled ENABLE ROW LEVEL SECURITY;
                ALTER TABLE companies.company_ot_enabled FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON companies.company_ot_enabled
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            // ============================================================
            // ProceduresConfig schema — RLS + triggers
            // ============================================================

            migrationBuilder.Sql("""
                ALTER TABLE procedures_config.procedure_types ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures_config.procedure_types FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures_config.procedure_types
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_procedure_types
                  BEFORE UPDATE ON procedures_config.procedure_types
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                CREATE TRIGGER trg_audit_procedure_types
                  AFTER INSERT OR UPDATE OR DELETE ON procedures_config.procedure_types
                  FOR EACH ROW EXECUTE FUNCTION public.fn_audit_log();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures_config.procedure_steps ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures_config.procedure_steps FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures_config.procedure_steps
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_procedure_steps
                  BEFORE UPDATE ON procedures_config.procedure_steps
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures_config.form_sections ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures_config.form_sections FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures_config.form_sections
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_form_sections
                  BEFORE UPDATE ON procedures_config.form_sections
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures_config.form_fields ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures_config.form_fields FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures_config.form_fields
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_form_fields
                  BEFORE UPDATE ON procedures_config.form_fields
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures_config.api_connectors ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures_config.api_connectors FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures_config.api_connectors
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_api_connectors
                  BEFORE UPDATE ON procedures_config.api_connectors
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures_config.rule_sets ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures_config.rule_sets FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures_config.rule_sets
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_rule_sets
                  BEFORE UPDATE ON procedures_config.rule_sets
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures_config.actor_definitions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures_config.actor_definitions FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures_config.actor_definitions
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_actor_definitions
                  BEFORE UPDATE ON procedures_config.actor_definitions
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures_config.query_rules ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures_config.query_rules FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures_config.query_rules
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_query_rules
                  BEFORE UPDATE ON procedures_config.query_rules
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            // ============================================================
            // Procedures schema — RLS + triggers
            // ============================================================

            migrationBuilder.Sql("""
                ALTER TABLE procedures.procedures ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures.procedures FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures.procedures
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_procedures
                  BEFORE UPDATE ON procedures.procedures
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                CREATE TRIGGER trg_audit_procedures
                  AFTER INSERT OR UPDATE OR DELETE ON procedures.procedures
                  FOR EACH ROW EXECUTE FUNCTION public.fn_audit_log();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures.procedure_actors ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures.procedure_actors FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures.procedure_actors
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_procedure_actors
                  BEFORE UPDATE ON procedures.procedure_actors
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures.vehicle_queries ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures.vehicle_queries FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures.vehicle_queries
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures.procedure_signatures ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures.procedure_signatures FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures.procedure_signatures
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_procedure_signatures
                  BEFORE UPDATE ON procedures.procedure_signatures
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE procedures.procedure_attachments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE procedures.procedure_attachments FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON procedures.procedure_attachments
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            // ============================================================
            // Documents schema — RLS + triggers
            // ============================================================

            migrationBuilder.Sql("""
                ALTER TABLE documents.document_types ENABLE ROW LEVEL SECURITY;
                ALTER TABLE documents.document_types FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON documents.document_types
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_document_types
                  BEFORE UPDATE ON documents.document_types
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE documents.procedure_type_documents ENABLE ROW LEVEL SECURITY;
                ALTER TABLE documents.procedure_type_documents FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON documents.procedure_type_documents
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_procedure_type_documents
                  BEFORE UPDATE ON documents.procedure_type_documents
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE documents.document_templates ENABLE ROW LEVEL SECURITY;
                ALTER TABLE documents.document_templates FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON documents.document_templates
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_document_templates
                  BEFORE UPDATE ON documents.document_templates
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE documents.procedure_documents ENABLE ROW LEVEL SECURITY;
                ALTER TABLE documents.procedure_documents FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON documents.procedure_documents
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_procedure_documents
                  BEFORE UPDATE ON documents.procedure_documents
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE documents.consolidated_packages ENABLE ROW LEVEL SECURITY;
                ALTER TABLE documents.consolidated_packages FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON documents.consolidated_packages
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            // ============================================================
            // OT schema — RLS + triggers
            // ============================================================

            migrationBuilder.Sql("""
                ALTER TABLE ot.ot_organisms ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ot.ot_organisms FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON ot.ot_organisms
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_ot_organisms
                  BEFORE UPDATE ON ot.ot_organisms
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE ot.ot_document_orders ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ot.ot_document_orders FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON ot.ot_document_orders
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            migrationBuilder.Sql("""
                ALTER TABLE ot.ot_document_labels ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ot.ot_document_labels FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON ot.ot_document_labels
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_ot_document_labels
                  BEFORE UPDATE ON ot.ot_document_labels
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE ot.ot_rule_sets ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ot.ot_rule_sets FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON ot.ot_rule_sets
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_ot_rule_sets
                  BEFORE UPDATE ON ot.ot_rule_sets
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE ot.ot_integration_logs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE ot.ot_integration_logs FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON ot.ot_integration_logs
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            // ============================================================
            // Integrations schema — RLS
            // ============================================================

            migrationBuilder.Sql("""
                ALTER TABLE integrations.connector_configs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE integrations.connector_configs FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON integrations.connector_configs
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                CREATE TRIGGER trg_row_version_connector_configs
                  BEFORE UPDATE ON integrations.connector_configs
                  FOR EACH ROW EXECUTE FUNCTION public.fn_increment_row_version();
                """);

            migrationBuilder.Sql("""
                ALTER TABLE integrations.integration_logs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE integrations.integration_logs FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON integrations.integration_logs
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            migrationBuilder.Sql("""
                ALTER TABLE integrations.identity_validations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE integrations.identity_validations FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON integrations.identity_validations
                  USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
                """);

            // ============================================================
            // Analytics schema + Materialized views (vistas dashboard)
            // ============================================================
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS analytics;");

            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.v_procedures_summary AS
                SELECT
                  p.tenant_id,
                  p.company_id,
                  p.status,
                  COUNT(*) AS total,
                  DATE_TRUNC('day', p.created_at) AS day
                FROM procedures.procedures p
                WHERE p.deleted_at IS NULL
                GROUP BY p.tenant_id, p.company_id, p.status, DATE_TRUNC('day', p.created_at)
                WITH DATA;
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS idx_v_procedures_summary_pk
                  ON analytics.v_procedures_summary (tenant_id, company_id, status, day);
                """);

            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.v_top_radicadores AS
                SELECT
                  p.tenant_id,
                  p.company_id,
                  p.created_by AS radicador_id,
                  COUNT(*) AS total_radicados,
                  DATE_TRUNC('month', p.created_at) AS month
                FROM procedures.procedures p
                WHERE p.deleted_at IS NULL
                GROUP BY p.tenant_id, p.company_id, p.created_by, DATE_TRUNC('month', p.created_at)
                WITH DATA;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Analytics views
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS analytics.v_top_radicadores;");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS analytics.v_procedures_summary;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS analytics;");

            // Drop all audit triggers
            foreach (var (schema, table) in new[]
            {
                ("identity", "users"), ("companies", "companies"),
                ("procedures_config", "procedure_types"), ("procedures", "procedures")
            })
            {
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS trg_audit_{table} ON {schema}.{table};");
            }

            // Drop all row_version triggers
            foreach (var (schema, table) in new[]
            {
                ("identity", "users"), ("identity", "roles"),
                ("companies", "companies"), ("companies", "company_configs"),
                ("companies", "company_signature_matrix"),
                ("procedures_config", "procedure_types"), ("procedures_config", "procedure_steps"),
                ("procedures_config", "form_sections"), ("procedures_config", "form_fields"),
                ("procedures_config", "api_connectors"), ("procedures_config", "rule_sets"),
                ("procedures_config", "actor_definitions"), ("procedures_config", "query_rules"),
                ("procedures", "procedures"), ("procedures", "procedure_actors"),
                ("procedures", "procedure_signatures"),
                ("documents", "document_types"), ("documents", "procedure_type_documents"),
                ("documents", "document_templates"), ("documents", "procedure_documents"),
                ("ot", "ot_organisms"), ("ot", "ot_document_labels"), ("ot", "ot_rule_sets"),
                ("integrations", "connector_configs")
            })
            {
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS trg_row_version_{table} ON {schema}.{table};");
            }

            // Drop all RLS policies
            foreach (var (schema, table) in new[]
            {
                ("identity", "users"), ("identity", "roles"),
                ("identity", "sessions"), ("identity", "invitations"),
                ("companies", "companies"), ("companies", "company_configs"),
                ("companies", "company_signature_matrix"), ("companies", "company_ot_enabled"),
                ("procedures_config", "procedure_types"), ("procedures_config", "procedure_steps"),
                ("procedures_config", "form_sections"), ("procedures_config", "form_fields"),
                ("procedures_config", "api_connectors"), ("procedures_config", "rule_sets"),
                ("procedures_config", "actor_definitions"), ("procedures_config", "query_rules"),
                ("procedures", "procedures"), ("procedures", "procedure_actors"),
                ("procedures", "vehicle_queries"), ("procedures", "procedure_signatures"),
                ("procedures", "procedure_attachments"),
                ("documents", "document_types"), ("documents", "procedure_type_documents"),
                ("documents", "document_templates"), ("documents", "procedure_documents"),
                ("documents", "consolidated_packages"),
                ("ot", "ot_organisms"), ("ot", "ot_document_orders"),
                ("ot", "ot_document_labels"), ("ot", "ot_rule_sets"), ("ot", "ot_integration_logs"),
                ("integrations", "connector_configs"), ("integrations", "integration_logs"),
                ("integrations", "identity_validations")
            })
            {
                migrationBuilder.Sql($"ALTER TABLE {schema}.{table} DISABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"DROP POLICY IF EXISTS tenant_isolation ON {schema}.{table};");
            }

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.fn_audit_log();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.fn_increment_row_version();");
        }
    }
}
