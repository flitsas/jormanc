using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M005_AlignModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sessions_active_revoked",
                schema: "identity",
                table: "sessions");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_active_revoked",
                schema: "identity",
                table: "sessions",
                column: "user_id",
                filter: "is_revoked = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sessions_active_revoked",
                schema: "identity",
                table: "sessions");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_active_revoked",
                schema: "identity",
                table: "sessions",
                column: "user_id",
                filter: "is_revoked = false AND expires_at > now()");
        }
    }
}
