using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace Flit.Infrastructure.Persistence;

internal static class TenantSessionContext
{
    private const string SessionSetupMarker = "set_config('app.tenant_id'";

    public static bool IsSessionSetupCommand(DbCommand command) =>
        command.CommandText.Contains(SessionSetupMarker, StringComparison.Ordinal);

    public static async Task ApplyAsync(
        DbConnection? connection,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken ct = default)
    {
        if (connection is null)
            return;

        var (tenantId, userId) = ResolveSessionValues(httpContextAccessor);
        if (string.IsNullOrEmpty(tenantId))
            return;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT set_config('app.tenant_id', $1, false), set_config('app.user_id', $2, false)";
        cmd.Parameters.Add(new NpgsqlParameter { Value = tenantId });
        cmd.Parameters.Add(new NpgsqlParameter { Value = userId });
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public static void Apply(DbConnection? connection, IHttpContextAccessor httpContextAccessor)
    {
        if (connection is null)
            return;

        var (tenantId, userId) = ResolveSessionValues(httpContextAccessor);
        if (string.IsNullOrEmpty(tenantId))
            return;

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT set_config('app.tenant_id', $1, false), set_config('app.user_id', $2, false)";
        cmd.Parameters.Add(new NpgsqlParameter { Value = tenantId });
        cmd.Parameters.Add(new NpgsqlParameter { Value = userId });
        cmd.ExecuteNonQuery();
    }

    private static (string TenantId, string UserId) ResolveSessionValues(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var tenantId = user?.FindFirst("tid")?.Value ?? string.Empty;
        var userId = user?.FindFirst("sub")?.Value ?? string.Empty;
        return (tenantId, userId);
    }
}
