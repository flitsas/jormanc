using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M006_AuditLogTenantFallback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    IF v_tenant_id IS NULL AND to_jsonb(OLD) ? 'tenant_id' THEN
                      v_tenant_id := (OLD.tenant_id)::uuid;
                    END IF;
                    INSERT INTO audit.audit_log
                      (tenant_id, schema_name, table_name, record_id, operation, changed_by, old_values, new_values)
                    VALUES
                      (v_tenant_id, TG_TABLE_SCHEMA, TG_TABLE_NAME, v_record_id, 'D', v_changed_by,
                       to_jsonb(OLD), NULL);
                    RETURN OLD;
                  ELSIF TG_OP = 'INSERT' THEN
                    v_record_id := NEW.id;
                    IF v_tenant_id IS NULL AND to_jsonb(NEW) ? 'tenant_id' THEN
                      v_tenant_id := (NEW.tenant_id)::uuid;
                    END IF;
                    IF v_changed_by IS NULL AND to_jsonb(NEW) ? 'created_by' THEN
                      v_changed_by := (NEW.created_by)::uuid;
                    END IF;
                    INSERT INTO audit.audit_log
                      (tenant_id, schema_name, table_name, record_id, operation, changed_by, old_values, new_values)
                    VALUES
                      (v_tenant_id, TG_TABLE_SCHEMA, TG_TABLE_NAME, v_record_id, 'I', v_changed_by,
                       NULL, to_jsonb(NEW));
                  ELSE
                    v_record_id := NEW.id;
                    IF v_tenant_id IS NULL AND to_jsonb(NEW) ? 'tenant_id' THEN
                      v_tenant_id := (NEW.tenant_id)::uuid;
                    END IF;
                    IF v_changed_by IS NULL AND to_jsonb(NEW) ? 'updated_by' THEN
                      v_changed_by := (NEW.updated_by)::uuid;
                    END IF;
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
