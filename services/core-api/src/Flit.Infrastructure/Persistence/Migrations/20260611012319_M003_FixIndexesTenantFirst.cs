using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M003_FixIndexesTenantFirst : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_procedures_submitted_at",
                schema: "procedures",
                table: "procedures");

            migrationBuilder.DropIndex(
                name: "ix_ot_integration_logs_logged_at",
                schema: "ot",
                table: "ot_integration_logs");

            migrationBuilder.DropIndex(
                name: "ix_integration_logs_logged_at",
                schema: "integrations",
                table: "integration_logs");

            migrationBuilder.DropIndex(
                name: "ix_identity_validations_created_at",
                schema: "integrations",
                table: "identity_validations");

            migrationBuilder.DropIndex(
                name: "ix_companies_name",
                schema: "companies",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "ix_companies_nit",
                schema: "companies",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "ix_companies_status",
                schema: "companies",
                table: "companies");

            migrationBuilder.CreateIndex(
                name: "ix_procedures_tenant_submitted_at",
                schema: "procedures",
                table: "procedures",
                columns: new[] { "tenant_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ot_integration_logs_tenant_logged_at",
                schema: "ot",
                table: "ot_integration_logs",
                columns: new[] { "tenant_id", "logged_at" });

            migrationBuilder.CreateIndex(
                name: "ix_integration_logs_tenant_logged_at",
                schema: "integrations",
                table: "integration_logs",
                columns: new[] { "tenant_id", "logged_at" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_validations_tenant_created_at",
                schema: "integrations",
                table: "identity_validations",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_tenant_name",
                schema: "companies",
                table: "companies",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_tenant_nit",
                schema: "companies",
                table: "companies",
                columns: new[] { "tenant_id", "nit" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_tenant_status",
                schema: "companies",
                table: "companies",
                columns: new[] { "tenant_id", "status" },
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_procedures_tenant_submitted_at",
                schema: "procedures",
                table: "procedures");

            migrationBuilder.DropIndex(
                name: "ix_ot_integration_logs_tenant_logged_at",
                schema: "ot",
                table: "ot_integration_logs");

            migrationBuilder.DropIndex(
                name: "ix_integration_logs_tenant_logged_at",
                schema: "integrations",
                table: "integration_logs");

            migrationBuilder.DropIndex(
                name: "ix_identity_validations_tenant_created_at",
                schema: "integrations",
                table: "identity_validations");

            migrationBuilder.DropIndex(
                name: "ix_companies_tenant_name",
                schema: "companies",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "ix_companies_tenant_nit",
                schema: "companies",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "ix_companies_tenant_status",
                schema: "companies",
                table: "companies");

            migrationBuilder.CreateIndex(
                name: "ix_procedures_submitted_at",
                schema: "procedures",
                table: "procedures",
                column: "submitted_at");

            migrationBuilder.CreateIndex(
                name: "ix_ot_integration_logs_logged_at",
                schema: "ot",
                table: "ot_integration_logs",
                column: "logged_at");

            migrationBuilder.CreateIndex(
                name: "ix_integration_logs_logged_at",
                schema: "integrations",
                table: "integration_logs",
                column: "logged_at");

            migrationBuilder.CreateIndex(
                name: "ix_identity_validations_created_at",
                schema: "integrations",
                table: "identity_validations",
                column: "created_at");

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
        }
    }
}
