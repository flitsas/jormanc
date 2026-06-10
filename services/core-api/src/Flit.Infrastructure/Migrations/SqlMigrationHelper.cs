using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Flit.Infrastructure.Migrations;

internal static class SqlMigrationHelper
{
    public static void ApplyEmbeddedSql(MigrationBuilder migrationBuilder, string resourceName)
    {
        var assembly = typeof(SqlMigrationHelper).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        var sql = reader.ReadToEnd();
        migrationBuilder.Sql(sql);
    }
}
