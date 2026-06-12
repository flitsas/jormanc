using System.Data;
using System.Data.Common;
using Flit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Flit.Api.Configuration;

/// <summary>
/// Mantiene una conexión PG abierta con <c>app.tenant_id</c> fijado para operaciones de seed
/// (sin HttpContext). Evita fallos RLS por pooling de Npgsql.
/// </summary>
internal static class SeedDbSession
{
    public static async Task RunAsync(
        FlitDbContext db,
        Guid tenantId,
        Guid userId,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;

        if (!wasOpen)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT set_config('app.tenant_id', $1, false), set_config('app.user_id', $2, false)";
            cmd.Parameters.Add(new NpgsqlParameter { Value = tenantId.ToString() });
            cmd.Parameters.Add(new NpgsqlParameter { Value = userId.ToString() });
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            await action(cancellationToken);
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }
}
